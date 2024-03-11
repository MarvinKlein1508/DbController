using Dapper;
using System.Data;
using System.Data.OleDb;
using System.Reflection;
using System.Runtime.Versioning;

namespace DbController.OleDb;

/// <summary>
/// Database wrapper for OleDb connections.
/// </summary>
[SupportedOSPlatform("windows")]
public class OleDbController : IDbController<OleDbConnection, OleDbTransaction>
{
    private bool _disposedValue;
    /// <inheritdoc />
    public required OleDbConnection Connection { get; init; }
    /// <inheritdoc />
    public OleDbTransaction? Transaction { get; private set; }

    #region Constructors
    /// <summary>
    /// Creates a new <see cref="OleDbController"/> with the given ConnectionString and opens the connection.
    /// </summary>
    /// <param name="connectionString"></param>
    [Obsolete("This method should no longer be used. It will be removed in a future version. Please use OleDbController.CreateAsync instead.")]
    public OleDbController(string connectionString)
    {
        Connection = new OleDbConnection(connectionString);
        Connection.Open();
    }

    private OleDbController() 
    { 

    }

    /// <summary>
    /// Creates a new <see cref="OleDbController"/> with the given ConnectionString and opens the connection asynchronously.
    /// </summary>
    /// <param name="connectionString"></param>
    public static async Task<OleDbController> CreateAsync(string connectionString)
    {
        var controller = new OleDbController()
        {
            Connection = new OleDbConnection(connectionString)
        };

        await controller.Connection.OpenAsync();
        return controller;
    }


    /// <summary>
    /// Static constructor to initialize the TypeAttributeCache
    /// </summary>
    static OleDbController()
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
    /// <inheritdoc />
    public async Task StartTransactionAsync()
    {
        if (Transaction is not null)
        {
            throw new InvalidOperationException($"Es konnte keine Transaction gestartet werden, da bereits eine Transaction läuft");
        }

        Transaction = (OleDbTransaction)await Connection.BeginTransactionAsync();
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
                Transaction = null;
            }

            _disposedValue = true;
        }
    }
    /// <inheritdoc />
    ~OleDbController()
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



    /// <summary>
    /// Not Implemented
    /// </summary>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public string GetLastIdSql()
    {
        throw new NotImplementedException();
    }
    /// <summary>
    /// Not Implemented
    /// </summary>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public string GetPaginationSyntax(int pageNumber, int limit)
    {
        throw new NotImplementedException();
    }


}