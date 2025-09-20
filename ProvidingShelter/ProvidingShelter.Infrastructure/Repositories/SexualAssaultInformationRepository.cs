using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using ProvidingShelter.Domain.Entities;
using ProvidingShelter.Domain.Repositories;
using ProvidingShelter.Infrastructure.Persistence;
using System.Data;

namespace ProvidingShelter.Infrastructure.Repositories
{
    public class SexualAssaultInformationRepository : ISexualAssaultInformationRepository
    {
        private readonly ShelterDbContext _db;

        public SexualAssaultInformationRepository(ShelterDbContext db)
        {
            _db = db;
        }

        public async Task AddRangeAsync(IEnumerable<SexualAssaultInformation> items, CancellationToken ct = default)
        {
            var list = items.ToList();
            if (list.Count == 0) return;

            var dt = BuildDataTable(list);

            // 取出 EF 的連線（SQL Server）
            var conn = (SqlConnection)_db.Database.GetDbConnection();
            var shouldClose = false;
            if (conn.State != ConnectionState.Open)
            {
                await conn.OpenAsync(ct);
                shouldClose = true;
            }

            try
            {
                using var bulk = new SqlBulkCopy(conn)
                {
                    DestinationTableName = "[dbo].[SexualAssaultInformation]",
                    BulkCopyTimeout = 0
                };

                foreach (DataColumn col in dt.Columns)
                {
                    bulk.ColumnMappings.Add(col.ColumnName, col.ColumnName);
                }

                await bulk.WriteToServerAsync(dt, ct);
            }
            finally
            {
                if (shouldClose)
                    await conn.CloseAsync();
            }
        }

        private static DataTable BuildDataTable(List<SexualAssaultInformation> list)
        {
            var dt = new DataTable();

            dt.Columns.Add("OwnerCityCode", typeof(string));
            dt.Columns.Add("InfoerType", typeof(string));
            dt.Columns.Add("InfoUnit", typeof(string));
            dt.Columns.Add("OtherInfoerType", typeof(string));
            dt.Columns.Add("OtherInfoUnit", typeof(string));
            dt.Columns.Add("ClientId", typeof(string));
            dt.Columns.Add("GENDER", typeof(string));
            dt.Columns.Add("BDate", typeof(int));
            dt.Columns.Add("IdType", typeof(string));
            dt.Columns.Add("Occupation", typeof(string));
            dt.Columns.Add("OtherOccupation", typeof(string));
            dt.Columns.Add("Education", typeof(string));
            dt.Columns.Add("Maimed", typeof(string));
            dt.Columns.Add("OtherMaimed", typeof(string));
            dt.Columns.Add("OtherMaimed2", typeof(string));
            dt.Columns.Add("School", typeof(string));
            dt.Columns.Add("DId", typeof(string));
            dt.Columns.Add("DSexId", typeof(string));
            dt.Columns.Add("DBDate", typeof(int));
            dt.Columns.Add("NumOfSuspect", typeof(byte));
            dt.Columns.Add("Relation", typeof(string));
            dt.Columns.Add("OtherRelation", typeof(string));
            dt.Columns.Add("OccurCity", typeof(string));
            dt.Columns.Add("OccurPlace", typeof(string));
            dt.Columns.Add("OccurTown", typeof(string));
            dt.Columns.Add("OtherOccurPlace", typeof(string));
            dt.Columns.Add("LastOccurTime", typeof(DateTime));
            dt.Columns.Add("InfoTimeYear", typeof(short));
            dt.Columns.Add("InfoTimeMonth", typeof(byte));
            dt.Columns.Add("TownCode", typeof(string));
            dt.Columns.Add("ReceiveTime", typeof(DateTime));
            dt.Columns.Add("NotifyDate", typeof(int));

            foreach (var x in list)
            {
                dt.Rows.Add(
                    (object?)x.OwnerCityCode ?? DBNull.Value,
                    (object?)x.InfoerType ?? DBNull.Value,
                    (object?)x.InfoUnit ?? DBNull.Value,
                    (object?)x.OtherInfoerType ?? DBNull.Value,
                    (object?)x.OtherInfoUnit ?? DBNull.Value,
                    (object?)x.ClientId ?? DBNull.Value,
                    (object?)x.Gender ?? DBNull.Value,
                    (object?)x.BDate ?? DBNull.Value,
                    (object?)x.IdType ?? DBNull.Value,
                    (object?)x.Occupation ?? DBNull.Value,
                    (object?)x.OtherOccupation ?? DBNull.Value,
                    (object?)x.Education ?? DBNull.Value,
                    (object?)x.Maimed ?? DBNull.Value,
                    (object?)x.OtherMaimed ?? DBNull.Value,
                    (object?)x.OtherMaimed2 ?? DBNull.Value,
                    (object?)x.School ?? DBNull.Value,
                    (object?)x.DId ?? DBNull.Value,
                    (object?)x.DSexId ?? DBNull.Value,
                    (object?)x.DBDate ?? DBNull.Value,
                    (object?)x.NumOfSuspect ?? DBNull.Value,
                    (object?)x.Relation ?? DBNull.Value,
                    (object?)x.OtherRelation ?? DBNull.Value,
                    (object?)x.OccurCity ?? DBNull.Value,
                    (object?)x.OccurPlace ?? DBNull.Value,
                    (object?)x.OccurTown ?? DBNull.Value,
                    (object?)x.OtherOccurPlace ?? DBNull.Value,

                    //
                    (object?)x.LastOccurTime ?? DBNull.Value,

                    (object?)x.InfoTimeYear ?? DBNull.Value,
                    (object?)x.InfoTimeMonth ?? DBNull.Value,
                    (object?)x.TownCode ?? DBNull.Value,

                    //
                    (object?)x.ReceiveTime ?? DBNull.Value,
                    (object?)x.NotifyDate ?? DBNull.Value
                );
            }

            return dt;
        }
    }
}
