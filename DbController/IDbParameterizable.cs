﻿namespace DbController
{
    /// <summary>
    /// Provides methods to generate parameters for Dapper queries.
    /// </summary>
    public interface IDbParameterizable
    {
        /// <summary>
        /// Gets a dictionary of parameters for the object which can be used in Dapper-Queries.
        /// </summary>
        /// <returns></returns>
        Dictionary<string, object?> GetParameters();
    }
}
