# DbController

An easy to use database wrapper which uses Dapper.

## What does this package do?
This package has been created to minify the effort to write database queries in C#. 

For example take a look on this block of code:
```csharp
DataTable result = new DataTable();
MySqlConnection connection = new MySqlConnection("YOUR_CONNECTION_STRING");
connection.Open();
MySqlCommand command = connection.CreateCommand();
command.CommandText = "SELECT * FROM customers";
MySqlDataAdapter adapter = new MySqlDataAdapter(command);
adapter.Fill(result);
connection.Close();
command.Dispose();
connection.Dispose();
```

With this package this code can be reduced to:
```csharp
using MySqlController dbController = new MySqlController("YOUR_CONNECTION_STRING");
List<Customer> customer = await dbController.SelectDataAsync<Customer>("SELECT * FROM customers");
```

It can even be more reduced when passing the `IDbController` around.
```
public Task<List<Customer>> GetCustomers(IDbController dbController)
{
	return dbController.SelectDataAsync<Customer>("SELECT * FROM customers");
}
```

Once initialized, the connection to the database will be opened automatically and it will kept open until the DbController gets disposed. In addition it keeps track of the active transaction with no addition work. 

## Installation

We offer different packages for this library. 

For MySql use:

`Install-Package DbController.MySql`

For SqlServer use:

`Install-Package DbController.SqlServer`

If you want to implement your own DbController then you should use:

`Install-Package DbController`

## Usage

Start by creating an instance of the DbController based on the package you want to use:

When using the `DbController.MySql` package a new controller can be created like this:
``` csharp
using MySqlController dbController = new MySqlController("YOUR_CONNECTION_STRING");
``` 

For the `DbController.SqlServer` package:
``` csharp
using SqlController dbController = new SqlController("YOUR_CONNECTION_STRING");
``` 

You can also use the interface to change the used DbController at runtime with dependency injection.
``` csharp
using IDbController dbController = new MySqlController("YOUR_CONNECTION_STRING");
``` 

`DbController` is using [Dapper](https://github.com/DapperLib/Dapper) under the hood for object mapping. 

## Extension to Dappers mapping
This package provides you with an attribute to map columns in the database to the corresponding property from your object. For example your customers table has a column with the name `customer_id`´. Your C# class uses a slightly different name to match the C# naming conventions. 

Now you can add an attribute to your property like this:
```csharp
public class Customer
{
	[CompareField("customer_id")]
	public int CustomerId { get; set; }
}
```

Both packages will create a `TypeAttributeCache` mapping all `CustomFieldAttributes` to the corresponding properties.

This will be done in the static constructor for the IDbController implementation. If you want to add this functionality to your own implementation of IDbController you'll should add a static constructor like this:
```csharp
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
```

## Methods
Every IDbController provides the following methods to work with:

|Method|Description|Returns
|------|-----------|-------
|QueryAsync|Executes SQL|---
|GetFirstAsync\<T\>|Executes SQL and returns maps the result to the corresponding object|Returns an instance of the object, null when no row is found
|SelectDataAsync\<T\>|Executes SQL and maps all results into a list|If no object is found it will return an empty list
|StartTransactionAsync|Starts a new transaction|Nothing will be returned
|CommitChangesAsync|Commits and writes all data from the current transaction|---
|RollbackChangesAsync|Rolls back all changes from the current transaction|---
|GetLastIdSql|Generates a valid SQL string to fetch the last inserted id|string
|GetPaginationSyntax|Generates a valid SQL string for pagination within SQL statements|string


## Best practices
It's generally recommended to pass an instance of IDbController to each function which needs to execute some SQL. For example:
```csharp
public static async Task Main(string[] args)
{
    using IDbController dbController = new MySqlController("YOUR_CONNECTIONSTRING");

    List<Customer> customers = await GetCustomers(dbController);
    List<Employee> employees = await GetEmployees(dbController);
}

public Task<List<Customer>> GetCustomers(IDbController dbController)
{
	return dbController.SelectDataAsync<Customer>("SELECT * FROM customers");
}

public Task<List<Employee>> GetEmployees(IDbController dbController)
{
	return dbController.SelectDataAsync<Employee>("SELECT * FROM employees");
}
```

The main benefit is that every function will be able to read from the current transaction state.


