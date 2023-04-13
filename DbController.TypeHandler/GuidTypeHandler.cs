using Dapper;
using System.Data;

namespace DbController.TypeHandler
{
    /// <summary>
    /// Converts <see cref="Guid"/> type from database using Dapper.
    /// </summary>
    public class GuidTypeHandler : SqlMapper.TypeHandler<Guid>
    {
        public override void SetValue(IDbDataParameter parameter, Guid guid)
        {
            parameter.Value = guid.ToString();
        }

        public override Guid Parse(object value)
        {
            return new Guid((string)value);
        }
    }
}