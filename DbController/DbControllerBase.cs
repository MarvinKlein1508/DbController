using Dapper;
using Microsoft.Extensions.Configuration;
using System.Reflection;

namespace DbController;
/// <summary>
/// Provides a base class for database controllers with common initialization functionality.
/// </summary>
/// <remarks>
/// The <see cref="DbControllerBase"/> class includes methods to initialize the database connection string 
/// and set up a type attribute cache for custom property mapping.
/// </remarks>
public abstract class DbControllerBase
{
    /// <summary>
    /// Stores the connection string used to connect to the database.
    /// </summary>
    /// <remarks>
    /// This field is initialized to an empty string and can be set using the <see cref="Initialize(IConfiguration)"/> or
    /// <see cref="Initialize(string)"/> methods.
    /// </remarks>
    protected static string _connectionString = string.Empty;
    /// <summary>
    /// Initializes the connection string for the application.
    /// </summary>
    /// <param name="configuration">An instance of <see cref="IConfiguration"/> used to retrieve the connection string.</param>
    /// <remarks>
    /// This method retrieves the connection string named "Default" from the configuration. 
    /// If no connection string is found, it assigns an empty string.
    /// </remarks>
    public static void Initialize(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("Default") ?? string.Empty;
    }

    /// <summary>
    /// Initializes the connection string for the application with a specified value.
    /// </summary>
    /// <param name="connectionString">The connection string to initialize the application with.</param>
    public static void Initialize(string connectionString)
    {
        _connectionString = connectionString;
    }
    /// <summary>
    /// Initializes the type attribute cache for classes decorated with the <see cref="CompareFieldAttribute"/>.
    /// </summary>
    /// <remarks>
    /// This method iterates through all types with the <see cref="CompareFieldAttribute"/> attribute and sets a custom type map for each.
    /// The type map uses <see cref="SingletonTypeAttributeCache"/> to retrieve properties by column name, supporting case-insensitive matching.
    /// </remarks>
    public static void InitializeTypeAttributeCache()
    {
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
}
