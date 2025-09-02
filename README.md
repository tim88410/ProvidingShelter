dotnet tool update --global dotnet-ef

dotnet add .\ProvidingShelter.Infrastructure\ProvidingShelter.Infrastructure.csproj package Microsoft.EntityFrameworkCore.Design

dotnet add .\ProvidingShelter.Infrastructure\ProvidingShelter.Infrastructure.csproj package Microsoft.EntityFrameworkCore.SqlServer

$env:ConnectionStrings__DefaultConnection="Server=DESKTOP-FSB7MK7\MSSQLSERVER2022;Database=ProvidingShelter;User Id=ProvidingShelter_Migrator;Password=Strong!Migrator#Pwd;Encrypt=True;TrustServerCertificate=True;Application Name=PS-Migrator;MultipleActiveResultSets=False"

dotnet ef migrations list --project .\ProvidingShelter.Infrastructure\ProvidingShelter.Infrastructure.csproj --startup-project .\ProvidingShelter.Importer\ProvidingShelter.Importer.csproj --verbose

dotnet ef migrations add InitialCreate --project .\ProvidingShelter.Infrastructure\ProvidingShelter.Infrastructure.csproj --startup-project .\ProvidingShelter.Importer\ProvidingShelter.Importer.csproj --context ShelterDbContext --output-dir Migrations --verbose

dotnet ef database update --project .\ProvidingShelter.Infrastructure\ProvidingShelter.Infrastructure.csproj --startup-project .\ProvidingShelter.Importer\ProvidingShelter.Importer.csproj --verbose

Remove-Item Env:ConnectionStrings__DefaultConnection -ErrorAction SilentlyContinue
