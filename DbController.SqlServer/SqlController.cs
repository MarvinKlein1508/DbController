using Dapper;
using System.Data.SqlClient;
using System.Reflection;

namespace DbController.SqlServer
{
    public sealed class SqlController : IDisposable, IDbController<SqlConnection, SqlCommand, SqlTransaction>
    {
        private bool _disposedValue;
        public SqlConnection Connection => Command.Connection;
        public string CommandText => Command.CommandText ?? String.Empty;
        public SqlTransaction? Transaction => Command.Transaction;
        public SqlCommand Command { get; }
        public string ConnectionString { get; }

        #region Constructors
        public SqlController(string connectionString)
        {
            ConnectionString = connectionString;
            Command = new SqlCommand
            {
                Connection = new SqlConnection(ConnectionString)
            };

            Command.Connection.Open();
        }
        /// <summary>
        /// Static constructor to initialize the TypeAttributeCache
        /// </summary>
        static SqlController()
        {
            // INIT Dapper for CompareField
            foreach (Type type in SingletonTypeAttributeCache.CacheAll<CompareFieldAttribute>((att) => att.FieldName))
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

            Command.Transaction = (SqlTransaction)await Connection.BeginTransactionAsync();
        }
        /// <summary>
        /// <inheritdoc />
        /// <para>
        /// When the method is called, the transaction is completed and can no longer be used.
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
                    // Dispose schließt die Verbindung automatisch
                    Connection.Dispose();
                    Command.Dispose();
                }

                _disposedValue = true;
            }
        }

        ~SqlController()
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
            return "SELECT SCOPE_IDENTITY();";
        }

        public string GetPaginationSyntax(int pageNumber, int limit)
        {
            return $" OFFSET {(pageNumber - 1) * limit} ROWS FETCH NEXT {limit} ROWS ONLY";
        }
    }
}