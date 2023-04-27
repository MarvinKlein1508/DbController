﻿namespace DbController
{
    /// <summary>
    /// Defines a databse model which can be indexed by an unique <see cref="Id"/>
    /// </summary>
    public interface IDbModel : IDbParameterizable
    {
        /// <summary>
        /// Gets the unique database identifier for the object.
        /// </summary>
        int Id { get; }
        
    }

    /// <summary>
    /// Extends the <see cref="IDbModel"/> interface with a property for a name.
    /// <para>
    /// This interface should only be used when you don't plan on using localization for your database objects.
    /// </para>
    /// </summary>
    public interface IDbModelWithName : IDbModel
    {
        /// <summary>
        /// Gets or sets the Name of the object.
        /// </summary>
        string Name { get; set; }
    }

    /// <summary>
    /// Defines a database model which supports one or more localizable properties.
    /// </summary>
    public interface ILocalizedDbModel : IDbModel
    {
        /// <summary>
        /// Returns an <see cref="Dictionary{TKey, TValue}"/> of parameters for each available localization.
        /// </summary>
        /// <returns></returns>
        IEnumerable<Dictionary<string, object?>> GetLocalizedParameters();
    }
}
