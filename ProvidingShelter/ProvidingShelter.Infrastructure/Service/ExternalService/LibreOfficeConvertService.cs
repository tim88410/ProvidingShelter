using Microsoft.Extensions.Logging;
using ProvidingShelter.Infrastructure.Abstractions;
using System.Diagnostics;

namespace ProvidingShelter.Infrastructure.Service.ExternalService
{
    public sealed class LibreOfficeConvertService : ILibreOfficeConvertService
    {
        private readonly ILibreOfficeOptions _opt;
        private readonly ILogger<LibreOfficeConvertService> _log;

        public LibreOfficeConvertService(ILibreOfficeOptions opt, ILogger<LibreOfficeConvertService> log)
        {
            _opt = opt;
            _log = log;
        }

        public async Task<string> ConvertOdsToXlsxAsync(string odsFullPath, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(odsFullPath) || !File.Exists(odsFullPath))
                throw new FileNotFoundException($"ODS 檔不存在：{odsFullPath}");

            var soffice = ResolveSofficePath(_opt.SofficePath);
            var work = Path.Combine(Path.GetTempPath(), "lo-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(work);
            Directory.CreateDirectory(Path.Combine(work, "profile"));

            var tmpIn = Path.Combine(work, "in.ods");
            File.Copy(odsFullPath, tmpIn, overwrite: true);

            var tmpOut = Path.Combine(work, "in.xlsx");
            if (File.Exists(tmpOut)) File.Delete(tmpOut);

            string? lastOut = null, lastErr = null;
            var ui = "file:///" + Path.Combine(work, "profile").TrimEnd('\\').Replace("\\", "/");

            // 依序嘗試：無濾鏡 → Calc MS Excel 2007 XML → Calc Office Open XML
            var argSets = new[]
            {
                $"--headless --nologo --norestore --nolockcheck --nodefault -env:UserInstallation=\"{ui}\" --convert-to xlsx --outdir \"{work}\" \"{tmpIn}\"",
                $"--headless --nologo --norestore --nolockcheck --nodefault -env:UserInstallation=\"{ui}\" --convert-to \"xlsx:Calc MS Excel 2007 XML\" --outdir \"{work}\" \"{tmpIn}\"",
                $"--headless --nologo --norestore --nolockcheck --nodefault -env:UserInstallation=\"{ui}\" --convert-to \"xlsx:Calc Office Open XML\" --outdir \"{work}\" \"{tmpIn}\""
            };

            try
            {
                foreach (var args in argSets)
                {
                    using var p = Process.Start(new ProcessStartInfo(soffice, args)
                    {
                        UseShellExecute = false,
                        RedirectStandardError = true,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true
                    }) ?? throw new InvalidOperationException("無法啟動 soffice。");

                    if (!p.WaitForExit(90_000)) { try { p.Kill(true); } catch { } }

                    lastOut = await p.StandardOutput.ReadToEndAsync();
                    lastErr = await p.StandardError.ReadToEndAsync();

                    // 成功條件：檔案存在
                    if (File.Exists(tmpOut)) break;
                    await Task.Delay(150, ct); // 等 flush
                }

                if (!File.Exists(tmpOut))
                    throw new InvalidOperationException($"LibreOffice 轉檔失敗。STDOUT:\n{lastOut}\nSTDERR:\n{lastErr}");

                var finalOut = Path.ChangeExtension(odsFullPath, ".xlsx");
                if (File.Exists(finalOut))
                {
                    var fi = new FileInfo(finalOut);
                    if (fi.IsReadOnly) fi.IsReadOnly = false;
                    File.Delete(finalOut);
                }
                File.Move(tmpOut, finalOut, overwrite: true);
                return finalOut;
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "LibreOffice 轉檔失敗：{Path}", odsFullPath);
                throw;
            }
            finally
            {
                // 清理暫存（失敗時保留資料夾便於排錯也可）
                try { Directory.Delete(work, recursive: true); } catch { }
            }
        }

        private static string ResolveSofficePath(string? hint)
        {
            if (!string.IsNullOrWhiteSpace(hint) && File.Exists(hint)) return hint;

            var env = Environment.GetEnvironmentVariable("LIBREOFFICE_PATH");
            if (!string.IsNullOrWhiteSpace(env) && File.Exists(env)) return env;

            var candidates = new[]
            {
                @"C:\Program Files\LibreOffice\program\soffice.exe",
                @"C:\Program Files (x86)\LibreOffice\program\soffice.exe"
            };
            foreach (var c in candidates) if (File.Exists(c)) return c;

            try
            {
                var psi = new ProcessStartInfo("where", "soffice") { UseShellExecute = false, RedirectStandardOutput = true, CreateNoWindow = true };
                using var p = Process.Start(psi)!; p.WaitForExit(3000);
                var line = p.StandardOutput.ReadLine();
                if (!string.IsNullOrWhiteSpace(line) && File.Exists(line)) return line!;
            }
            catch { }

            throw new FileNotFoundException("找不到 soffice.exe，請設定 ILibreOfficeOptions.SofficePath 或環境變數 LIBREOFFICE_PATH。");
        }
    }
}
