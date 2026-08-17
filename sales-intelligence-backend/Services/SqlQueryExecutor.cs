using System.Data;
using Microsoft.EntityFrameworkCore;
using SalesIntelligence.Api.Data;
using SalesIntelligence.Api.Models;

namespace SalesIntelligence.Api.Services
{
    public interface ISqlQueryExecutor
    {
        Task<SqlQueryResult> ExecuteAsync(string sql, CancellationToken cancellationToken = default);
    }

    public class SqlQueryExecutor : ISqlQueryExecutor
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<SqlQueryExecutor> _logger;

        public SqlQueryExecutor(ApplicationDbContext db, ILogger<SqlQueryExecutor> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<SqlQueryResult> ExecuteAsync(string sql, CancellationToken cancellationToken = default)
        {
            var result = new SqlQueryResult();
            var connection = _db.Database.GetDbConnection();

            if (connection.State != ConnectionState.Open)
            {
                await connection.OpenAsync(cancellationToken);
            }

            await using var command = connection.CreateCommand();
            command.CommandText = sql;
            command.CommandTimeout = 30;

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            for (var i = 0; i < reader.FieldCount; i++)
            {
                result.Columns.Add(reader.GetName(i));
            }

            while (await reader.ReadAsync(cancellationToken))
            {
                var row = new Dictionary<string, object?>();
                for (var i = 0; i < reader.FieldCount; i++)
                {
                    row[result.Columns[i]] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                }
                result.Rows.Add(row);
            }

            _logger.LogInformation("SQL agent executed query returning {RowCount} rows", result.RowCount);
            return result;
        }
    }
}
