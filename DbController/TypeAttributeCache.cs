using System.Reflection;
using System.Xml.Linq;

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

        private Dictionary<string, PropertyInfo> InternalCache { get; set; } = new Dictionary<string, PropertyInfo>();

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
                        if (!InternalCache.ContainsKey(names[i]))
                        {
                            InternalCache.Add(names[i], p);
                        }
                    }
                    
                }
            }
        }

        public PropertyInfo? Get(string name)
        {
            if (InternalCache.ContainsKey(name))
            {
                return InternalCache[name];
            }
            else
            {
                return Type.GetProperty(name);
            }
        }
    }

    public static class SingletonTypeAttributeCache
    {
        public static Dictionary<Type, TypeAttributeCache> InternalCache { get; } = new Dictionary<Type, TypeAttributeCache>();
        public static void Cache<TAttribute>(Type type, Func<TAttribute, string[]> compareFunction) where TAttribute : Attribute
        {
            if (InternalCache.ContainsKey(type))
            {
                InternalCache[type].Cache(compareFunction);
            }
            else
            {
                TypeAttributeCache cache = new TypeAttributeCache(type);
                cache.Cache(compareFunction);
                InternalCache.Add(type, cache);
            }
        }

        public static List<Type> CacheAll<TAttribute>(Func<TAttribute, string[]> compareFunction) where TAttribute : Attribute
        {
            List<Type> cachedTypes = new List<Type>();
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (Type type in assembly.GetTypes())
                {
                    Cache(type, compareFunction);
                    cachedTypes.Add(type);
                }
            }
            return cachedTypes;
        }

        public static PropertyInfo? Get(Type type, string name)
        {
            if (InternalCache.ContainsKey(type))
            {
                return InternalCache[type].Get(name);
            }
            else
            {
                return null;
            }
        }
    }
}
