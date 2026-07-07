using Microsoft.Extensions.Options;
using MyTestVueApp.Server.Configuration;

namespace MyTestVueApp.Server.Database
{
    public class PostgresDataAccess : IPostgresDataAccess
    {
        private readonly IOptions<ApplicationConfiguration> _appConfig;

        public PostgresDataAccess(IOptions<ApplicationConfiguration> appConfig)
        {
            _appConfig = appConfig;
        }

        public async Task<List<T>> QueryAsync<T>(
            string sql,
            Action<SqlCommand>? configure,
            Func<SqlDataReader, T> map)
        {
            var results = new List<T>();
            using var connection = await OpenConnectionAsync();
            using var command = new SqlCommand(sql, connection);
            configure?.Invoke(command);

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                results.Add(map(reader));
            }

            return results;
        }

        public async Task<T?> ExecuteScalarAsync<T>(
            string sql,
            Action<SqlCommand>? configure = null)
        {
            using var connection = await OpenConnectionAsync();
            using var command = new SqlCommand(sql, connection);
            configure?.Invoke(command);

            var result = await command.ExecuteScalarAsync();
            if (result == null || result == DBNull.Value)
            {
                return default;
            }

            return (T)Convert.ChangeType(result, typeof(T));
        }

        public async Task<int> ExecuteAsync(
            string sql,
            Action<SqlCommand>? configure = null)
        {
            using var connection = await OpenConnectionAsync();
            using var command = new SqlCommand(sql, connection);
            configure?.Invoke(command);

            return await command.ExecuteNonQueryAsync();
        }

        public async Task<T> ExecuteInTransactionAsync<T>(
            Func<SqlConnection, SqlTransaction, Task<T>> operation)
        {
            using var connection = await OpenConnectionAsync();
            using var transaction = await connection.BeginTransactionAsync();

            try
            {
                var result = await operation(connection, transaction);
                await transaction.CommitAsync();
                return result;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        private async Task<SqlConnection> OpenConnectionAsync()
        {
            var connection = new SqlConnection(_appConfig.Value.ConnectionString);
            await connection.OpenAsync();
            return connection;
        }
    }
}
