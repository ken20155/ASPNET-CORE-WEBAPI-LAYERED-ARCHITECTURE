using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;
using WebApiInterviewStatus.Dbconfig;

namespace WebApiInterviewStatus.Models
{
    public enum DbSetType { Db1, Db2, Db3 }

    public class MainModel
    {
        private readonly Db1Context _db1;
        private readonly Db2Context _db2;
        private readonly Db3Context _db3;

        public MainModel(Db1Context db1, Db2Context db2, Db3Context db3)
        {
            _db1 = db1;
            _db2 = db2;
            _db3 = db3;
        }

        private SqlConnection GetSqlConnection(DbSetType dbSet)
        {
            var conn = dbSet switch
            {
                DbSetType.Db1 => _db1.Database.GetDbConnection(),
                DbSetType.Db2 => _db2.Database.GetDbConnection(),
                DbSetType.Db3 => _db3.Database.GetDbConnection(),
                _ => throw new ArgumentException("Invalid DbSetType")
            };

            if (conn is SqlConnection sqlConn)
                return sqlConn;

            throw new InvalidOperationException("Database connection is not a SqlConnection");
        }

        public async Task<dynamic?> GetRowAsync(string sql, object? param = null, string? column = null, DbSetType dbSet = DbSetType.Db2)
        {
            var conn = GetSqlConnection(dbSet);

            if (conn.State != ConnectionState.Open)
                await conn.OpenAsync();

            var result = await conn.QueryAsync(sql, param);
            var row = result.FirstOrDefault();
            if (row == null) return null;

            if (!string.IsNullOrEmpty(column) && row is IDictionary<string, object> dict)
                return dict.ContainsKey(column) ? dict[column] : null;

            return row;
        }
        public async Task<IEnumerable<dynamic>> GetAllAsync(string sql, object? param = null, DbSetType dbSet = DbSetType.Db2)
        {
            await using var conn = GetSqlConnection(dbSet);
            if (conn.State != ConnectionState.Open)
                await conn.OpenAsync();

            return await conn.QueryAsync(sql, param);
        }
        public async Task<dynamic> InsertAsync(string table,object data,DbSetType dbSet = DbSetType.Db2)
        {
            await using var conn = GetSqlConnection(dbSet);
            if (conn.State != ConnectionState.Open)
                await conn.OpenAsync();

            // Convert object to dictionary
            var props = data.GetType().GetProperties();
            var columns = string.Join(", ", props.Select(p => $"[{p.Name}]"));
            var values = string.Join(", ", props.Select(p => $"@{p.Name}"));

            var sql = $@"
                        INSERT INTO [{table}] ({columns})
                        VALUES ({values});
                        SELECT CAST(SCOPE_IDENTITY() as int);";

            var id = await conn.ExecuteScalarAsync<int>(sql, data);

            return new
            {
                results = id > 0,
                id
            };
        }

    }
}
