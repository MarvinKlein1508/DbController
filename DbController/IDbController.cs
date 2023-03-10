using System.Data;

namespace DbController
{
    /// <summary>
    /// Generalization of a database interface
    /// </summary>
    public interface IDbController : IDisposable
    {
        string CommandText { get; }
        string ConnectionString { get; }
        /// <summary>
        /// Executes SQL and returns the first specified object found.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="selectCommand"></param>
        /// <param name="param"></param>
        /// <returns>When no object is found, this method will return null.</returns>
        Task<T?> GetFirstAsync<T>(string selectCommand, object? param = null);
        /// <summary>
        /// Executes SQL and returns all found objects within a list.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="selectCommand"></param>
        /// <param name="param"></param>
        /// <returns>When no objects are found, an empty list will be returned.</returns>
        Task<List<T>> SelectDataAsync<T>(string selectCommand, object? param = null);
        /// <summary>
        /// Executes SQL and does not return anything.
        /// </summary>
        /// <param name="sqlCommand"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        Task QueryAsync(string sqlCommand, object? param = null);
        /// <summary>
        /// Starts a new transaction for this IDbController instance.
        /// </summary>
        /// <returns></returns>
        Task StartTransactionAsync();
        /// <summary>
        /// Commits and write all changes of the current transaction to the database.
        /// </summary>
        /// <returns></returns>
        Task CommitChangesAsync();
        /// <summary>
        /// Rollsback all changes of the current transaction
        /// </summary>
        /// <returns></returns>
        Task RollbackChangesAsync();
        /// <summary>
        /// Gets the syntax to return the last inserted ID.
        /// </summary>
        /// <returns></returns>
        string GetLastIdSql();
        /// <summary>
        /// Gets the syntax to fetch only a specific amount of rows.
        /// </summary>
        /// <param name="pageNumber"></param>
        /// <param name="limit"></param>
        /// <returns></returns>
        string GetPaginationSyntax(int pageNumber, int limit);
    }
    public interface IDbController<TConnection, TCommand, TTransaction> : IDbController where TConnection : IDbConnection where TCommand : IDbCommand where TTransaction : IDbTransaction
    {
        TConnection Connection { get; }
        TTransaction? Transaction { get; }
        TCommand Command { get; }
    }
}