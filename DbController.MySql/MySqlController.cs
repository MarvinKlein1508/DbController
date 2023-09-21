using Dapper;
using MySql.Data.MySqlClient;
using System.Reflection;
using System.Threading;

namespace DbController.MySql
{
    public sealed class MySqlController : IDisposable, IDbController<MySqlConnection, MySqlCommand, MySqlTransaction>
    {
        private bool _disposedValue;
        public MySqlConnection Connection => Command.Connection;
        public MySqlTransaction? Transaction => Command.Transaction;
        public MySqlCommand Command { get; }
        public string CommandText => Command.CommandText ?? String.Empty;
        public string ConnectionString { get; }

        #region Constructors
        /// <summary>
        /// Creates a new <see cref="MySqlController"/> with the given ConnectionString and opens the connection.
        /// </summary>
        /// <param name="connectionString"></param>
        public MySqlController(string connectionString)
        {
            ConnectionString = connectionString;
            Command = new MySqlCommand
            {
                Connection = new MySqlConnection(ConnectionString)
            };

            Command.Connection.Open();
        }
        /// <summary>
        /// Static constructor to initialize the TypeAttributeCache
        /// </summary>
        static MySqlController()
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
        /// <inheritdoc />
        /// </summary>
        /// <exception cref="InvalidOperationException">Is triggered if a transaction is already running.</exception>
        public async Task StartTransactionAsync()
        {
            if (Command.Transaction is not null)
            {
                throw new InvalidOperationException($"Es konnte keine Transaction gestartet werden, da bereits eine Transaction läuft");
            }

            Command.Transaction = await Connection.BeginTransactionAsync();
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

        ~MySqlController()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
        #endregion

        public string GetLastIdSql()
        {
            return "SELECT LAST_INSERT_ID();";
        }

        public string GetPaginationSyntax(int pageNumber, int limit)
        {
            return $"LIMIT {(pageNumber - 1) * limit}, {limit}";
        }
    }
}