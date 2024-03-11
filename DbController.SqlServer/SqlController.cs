using Dapper;
using System.Reflection;
using Microsoft.Data.SqlClient;
using System.Data;

namespace DbController.SqlServer;

/// <summary>
/// Database wrapper for SqlServer connections.
/// </summary>
public sealed class SqlController : IDisposable, IDbController<SqlConnection, SqlTransaction>
{
    private bool _disposedValue;
    /// <inheritdoc />
    public required SqlConnection Connection { get; init; }
    /// <inheritdoc />
    public SqlTransaction? Transaction { get; private set; }

    #region Constructors
    /// <summary>
    /// Creates a new <see cref="SqlController"/> with the given ConnectionString and opens the connection.
    /// </summary>
    /// <param name="connectionString"></param>
    [Obsolete("This method should no longer be used. It will be removed in a future version. Please use SqlController.CreateAsync instead.")]
    public SqlController(string connectionString)
    {

        Connection = new SqlConnection(connectionString);
        Connection.Open();
    }

    private SqlController()
    {

    }

    /// <summary>
    /// Creates a new <see cref="SqlController"/> with the given ConnectionString and opens the connection asynchronously.
    /// </summary>
    /// <param name="connectionString"></param>
    public static async Task<SqlController> CreateAsync(string connectionString)
    {
        var controller = new SqlController()
        {
            Connection = new SqlConnection(connectionString)
        };

        await controller.Connection.OpenAsync();
        return controller;
    }

    /// <summary>
    /// Static constructor to initialize the TypeAttributeCache
    /// </summary>
    static SqlController()
    {
        // INIT Dapper for CompareField
        foreach (Type type in SingletonTypeAttributeCache.CacheAll<CompareFieldAttribute>((att) => att.FieldNames))
        {
            SqlMapper.SetTypeMap(type, new CustomPropertyTypeMap(
                type,
                (type, columnName) =>
                {
                    PropertyInfo? prop = SingletonTypeAttributeCache.Get(type, columnName);

                    return prop is null ? type.GetProperty(columnName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance)! : prop;

                }
            ));
        }
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
    #endregion
    #region Transaction
    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">Is triggered if a transaction is already running.</exception>
    public async Task StartTransactionAsync()
    {
        if (Transaction is not null)
        {
            throw new InvalidOperationException($"Es konnte keine Transaction gestartet werden, da bereits eine Transaction läuft");
        }

        Transaction = (SqlTransaction)await Connection.BeginTransactionAsync();
    }
    /// <inheritdoc />
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
    /// <inheritdoc />
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
                // Dispose schließt die Verbindung automatisch
                Connection.Dispose();
                Transaction?.Dispose();
                Transaction = null;
            }

            _disposedValue = true;
        }
    }
    /// <inheritdoc />
    ~SqlController()
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
        return "SELECT SCOPE_IDENTITY();";
    }
    /// <inheritdoc />
    public string GetPaginationSyntax(int pageNumber, int limit)
    {
        return $" OFFSET {(pageNumber - 1) * limit} ROWS FETCH NEXT {limit} ROWS ONLY";
    }
    
}