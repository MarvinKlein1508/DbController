using Dapper;
using MySqlConnector;
using System.Data;
using System.Data.Common;

namespace DbController.MySql;

/// <summary>
/// Database wrapper for MySql connections.
/// </summary>
public sealed class MySqlController : DbControllerBase, IDisposable, IDbController<MySqlConnection, MySqlTransaction>
{
    private bool _disposedValue;
    /// <inheritdoc />
    public MySqlConnection Connection { get; }
    /// <inheritdoc />
    public MySqlTransaction? Transaction { get; private set; }

    #region Constructors
    /// <summary>
    /// Creates a new <see cref="MySqlController"/> with the given ConnectionString and opens the connection.
    /// </summary>
    /// <param name="connectionString"></param>
    public MySqlController(string? connectionString = null)
    {
        connectionString ??= _connectionString;

        Connection = new MySqlConnection(connectionString);
        Connection.Open();
    }

    #endregion
    #region SQL-Methods
    /// <inheritdoc />
    public async Task QueryAsync(string sql, object? param = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        CommandDefinition definition = new CommandDefinition(sql, param, Transaction, cancellationToken: cancellationToken);
        await Connection.QueryAsync(definition);
    }
    /// <inheritdoc />
    public Task<T?> GetFirstAsync<T>(string sql, object? param = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        CommandDefinition definition = new CommandDefinition(sql, param, Transaction, cancellationToken: cancellationToken);
        Task<T?> result = Connection.QueryFirstOrDefaultAsync<T?>(definition);
        return result;
    }
    /// <inheritdoc />
    public async Task<List<T>> SelectDataAsync<T>(string sql, object? param = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        CommandDefinition definition = new CommandDefinition(sql, param, Transaction, cancellationToken: cancellationToken);
        IEnumerable<T> enumerable = await Connection.QueryAsync<T>(definition);
        return enumerable.ToList();
    }
    /// <inheritdoc />
    public async Task<DynamicParameters?> ExecuteProcedureAsync(string procedureName, DynamicParameters? param = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        CommandDefinition definition = new CommandDefinition(procedureName, param, Transaction, cancellationToken: cancellationToken, commandType: CommandType.StoredProcedure);
        await Connection.ExecuteAsync(definition);
        return param;
    }

    /// <inheritdoc />
    public Task<DbDataReader> ExecuteReaderAsync(string sql, object? param = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        CommandDefinition definition = new CommandDefinition(sql, param, Transaction, cancellationToken: cancellationToken);
        return Connection.ExecuteReaderAsync(definition);
    }
    #endregion
    #region Transaction
    /// <summary>
    /// <inheritdoc />
    /// </summary>
    /// <exception cref="InvalidOperationException">Is triggered if a transaction is already running.</exception>
    public async Task StartTransactionAsync()
    {
        if (Transaction is not null)
        {
            throw new InvalidOperationException($"Es konnte keine Transaction gestartet werden, da bereits eine Transaction läuft");
        }

        Transaction = await Connection.BeginTransactionAsync();
    }
    /// <summary>
    /// <inheritdoc />
    /// <para>
    /// When the method is called, the <see cref="Transaction"/> is completed and can no longer be used.
    /// </para>
    /// </summary>
    public async Task CommitChangesAsync()
    {
        try
        {
            await Transaction.CommitAsync();
        }
        catch (Exception)
        {

            throw;
        }
        finally
        {
            Transaction?.Dispose();
            Transaction = null;
        }
    }
    public async Task RollbackChangesAsync()
    {
        try
        {
            await Transaction.RollbackAsync();
        }
        catch (Exception)
        {

            throw;
        }
        finally
        {
            Transaction?.Dispose();
            Transaction = null;
        }
    }
    #endregion
    #region IDisposable
    private void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                // Dispose closes the connection automatically
                Connection.Dispose();
                Transaction?.Dispose();
            }

            _disposedValue = true;
        }
    }
    /// <inheritdoc />
    ~MySqlController()
    {
        Dispose(disposing: false);
    }
    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
    #endregion
    /// <inheritdoc />
    public string GetLastIdSql()
    {
        return "SELECT LAST_INSERT_ID();";
    }
    /// <inheritdoc />
    public string GetPaginationSyntax(int pageNumber, int limit)
    {
        return $"LIMIT {(pageNumber - 1) * limit}, {limit}";
    }

}