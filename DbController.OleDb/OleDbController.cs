using Dapper;
using System.Data.OleDb;
using System.Reflection;
using System.Runtime.Versioning;

namespace DbController.OleDb
{
    [SupportedOSPlatform("windows")]
    public class OleDbController : IDbController<OleDbConnection, OleDbCommand, OleDbTransaction>
    {
        private bool _disposedValue;
        public OleDbConnection Connection => Command.Connection!;
        public OleDbTransaction? Transaction => Command.Transaction;
        public OleDbCommand Command { get; }
        public string ConnectionString { get; }
        public string CommandText => Command.CommandText ?? string.Empty;
        #region Constructors
        /// <summary>
        /// Creates a new <see cref="OleDbController"/> with the given ConnectionString and opens the connection.
        /// </summary>
        /// <param name="connectionString"></param>
        public OleDbController(string connectionString)
        {
            ConnectionString = connectionString;

            Command = new OleDbCommand
            {
                Connection = new OleDbConnection(ConnectionString)
            };

            Command.Connection.Open();
        }
        /// <summary>
        /// Static constructor to initialize the TypeAttributeCache
        /// </summary>
        static OleDbController()
        {
            if (!TypeAttributeCache.CacheIsInitialized)
            {
                // INIT Dapper for CompareField
                foreach (Type type in SingletonTypeAttributeCache.CacheAll<CompareFieldAttribute>((att) => att.FieldNames))
                {
                    SqlMapper.SetTypeMap(type, new CustomPropertyTypeMap(
                        type,
                        (type, columnName) =>
                        {
                            PropertyInfo? prop = SingletonTypeAttributeCache.Get(type, columnName);

                            return prop is null ? type.GetProperty(columnName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance) : prop;

                        }
                    ));
                }
                
                TypeAttributeCache.CacheIsInitialized = true;
            }
        }
        #endregion
        #region SQL-Methods
        public async Task QueryAsync(string sql, object? param = null, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            CommandDefinition definition = new CommandDefinition(sql, param, Transaction, cancellationToken: cancellationToken);
            await Command.Connection.QueryAsync(definition);
        }
        public Task<T?> GetFirstAsync<T>(string sql, object? param = null, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            CommandDefinition definition = new CommandDefinition(sql, param, Transaction, cancellationToken: cancellationToken);
            Task<T?> result = Command.Connection.QueryFirstOrDefaultAsync<T?>(definition);
            return result;
        }
        public async Task<List<T>> SelectDataAsync<T>(string sql, object? param = null, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            CommandDefinition definition = new CommandDefinition(sql, param, Transaction, cancellationToken: cancellationToken);
            IEnumerable<T> enumerable = await Command.Connection.QueryAsync<T>(definition);
            return enumerable.ToList();
        }
        #endregion
        #region Transaction
        /// <summary>
        /// Not Implemented
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public async Task CommitChangesAsync()
        {
            try
            {
                await Command.Transaction.CommitAsync();
            }
            catch (Exception)
            {

                throw;
            }
            finally
            {
                Command.Transaction?.Dispose();
            }
        }
        /// <summary>
        /// Not Implemented
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public async Task RollbackChangesAsync()
        {
            try
            {
                await Command.Transaction.RollbackAsync();
            }
            catch (Exception)
            {

                throw;
            }
        }
        /// <summary>
        /// Not Implemented
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public async Task StartTransactionAsync()
        {
            if (Command.Transaction is not null)
            {
                throw new InvalidOperationException($"Es konnte keine Transaction gestartet werden, da bereits eine Transaction läuft");
            }

            Command.Transaction = (OleDbTransaction)await Connection.BeginTransactionAsync();
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
                    Command.Dispose();
                }

                _disposedValue = true;
            }
        }

        ~OleDbController()
        {
            Dispose(disposing: false);
        }

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
}