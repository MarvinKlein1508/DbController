using Dapper;
using System.Data;
using System.Text.Json;

namespace DbController.TypeHandler;

/// <summary>
/// TypeHandler to serialize and deserialize JSON objects
/// </summary>
public class JsonTypeHandler : SqlMapper.ITypeHandler
{
    public void SetValue(IDbDataParameter parameter, object value)
    {
        if (value is null)
        {
            parameter.Value = null;
        }
        else
        {
            parameter.Value = JsonSerializer.Serialize(value);
        }
    }

    public object Parse(Type destinationType, object value)
    {
        if (value is null)
        {
            return null;
        }
        else
        {
            return JsonSerializer.Deserialize(value as string, destinationType);
        }
    }
}
