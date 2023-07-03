namespace DbController
{
    /// <summary>
    /// This property is being used to map columns to properties. 
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class CompareFieldAttribute : Attribute
    {
        
        /// <summary>
        /// Gets the corresponding field name of the database column.
        /// </summary>
        public string[] FieldNames { get; }

        /// <summary>
        /// Create a new CompareFieldAttribut
        /// </summary>
        /// <param name="fieldName">The name of the field as it is read from the database.</param>
        public CompareFieldAttribute(params string[] fieldName)
        {
            this.FieldNames = fieldName;
        }
    }
}