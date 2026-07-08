namespace MyTestVueApp.Server.Database
{
    public interface IPostgresDataAccess
    {
        Task<List<T>> QueryAsync<T>(
            string sql,
            Action<SqlCommand>? configure,
            Func<SqlDataReader, T> map);

        Task<T?> ExecuteScalarAsync<T>(
            string sql,
            Action<SqlCommand>? configure = null);

        Task<int> ExecuteAsync(
            string sql,
            Action<SqlCommand>? configure = null);

        Task<T> ExecuteInTransactionAsync<T>(
            Func<SqlConnection, SqlTransaction, Task<T>> operation);
    }
}
