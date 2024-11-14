using System.Reflection;

namespace DbController
{
    /// <summary>
    /// Represents a cache for all property mappings for a given class. 
    /// <para>
    /// The <see cref="CompareFieldAttribute"/> is understood as an assignment.
    /// </para>
    /// <para>
    /// The cache is currently only used for type conversion by Dapper.
    /// </para>
    /// </summary>
    public class TypeAttributeCache
    {
        private Type Type { get; set; }
        public TypeAttributeCache(Type type)
        {
            Type = type;
        }

        private Dictionary<string, PropertyInfo> InternalCache { get; set; } = [];

        public void Cache<TAttribute>(Func<TAttribute, string[]> compareFunction) where TAttribute : Attribute
        {
            InternalCache.Clear();
            foreach (PropertyInfo p in Type.GetProperties())
            {
                TAttribute? attribute = p.GetCustomAttribute<TAttribute>();
                if (attribute is not null)
                {
                    string[] names = compareFunction(attribute);
                    for (int i = 0; i < names.Length; i++)
                    {
                        InternalCache.TryAdd(names[i], p);
                    }

                }
            }
        }

        public PropertyInfo? Get(string name)
        {
            if (InternalCache.TryGetValue(name, out PropertyInfo? value))
            {
                return value;
            }
            else
            {
                return Type.GetProperty(name);
            }
        }
    }
    /// <summary>
    /// Provides caching functiojnaltiy for remapped properties to be bound within dapper queries.
    /// </summary>
    public static class SingletonTypeAttributeCache
    {
        public static Dictionary<Type, TypeAttributeCache> InternalCache { get; } = [];
        /// <summary>
        /// Caches a specific type
        /// </summary>
        /// <typeparam name="TAttribute"></typeparam>
        /// <param name="type"></param>
        /// <param name="compareFunction"></param>
        public static void Cache<TAttribute>(Type type, Func<TAttribute, string[]> compareFunction) where TAttribute : Attribute
        {
            if (InternalCache.TryGetValue(type, out TypeAttributeCache? value))
            {
                value.Cache(compareFunction);
            }
            else
            {
                TypeAttributeCache cache = new(type);
                cache.Cache(compareFunction);
                InternalCache.Add(type, cache);
            }
        }
        /// <summary>
        /// Caches all properties of the current assembly for dapper mapping operations.
        /// </summary>
        /// <typeparam name="TAttribute"></typeparam>
        /// <param name="compareFunction"></param>
        /// <returns></returns>
        public static List<Type> CacheAll<TAttribute>(Func<TAttribute, string[]> compareFunction) where TAttribute : Attribute
        {
            List<Type> cachedTypes = [];
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var types = Array.Empty<Type>();

                try
                {
                    types = assembly.GetTypes();
                }
                catch (Exception)
                {
                    // This can fail when loading dynamic types of certain DLL. In .NET 8 SqlClient will fail.
                }

                foreach (Type type in types)
                {
                    Cache(type, compareFunction);
                    cachedTypes.Add(type);
                }
            }
            return cachedTypes;
        }
        /// <summary>
        /// Gets the property info for a given name
        /// </summary>
        /// <param name="type"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static PropertyInfo? Get(Type type, string name)
        {
            if (InternalCache.TryGetValue(type, out TypeAttributeCache? value))
            {
                return value.Get(name);
            }
            else
            {
                return null;
            }
        }
    }
}
