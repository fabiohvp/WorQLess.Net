using System.Collections.Generic;
using System.Data.Entity;
using System.Reflection;

namespace WorQLess.Net.Extensions
{
    public static class DbContextExtensions
    {
        public static List<PropertyInfo> GetDbSetProperties(this DbContext context)
        {
            var dbSetProperties = new List<PropertyInfo>();
            var properties = context.GetType().GetProperties();

            foreach (var property in properties)
            {
                var setType = property.PropertyType;

                var isDbSet = setType.IsGenericType && (typeof(IDbSet<>).IsAssignableFrom(setType.GetGenericTypeDefinition()) || setType.GetInterface(typeof(IDbSet<>).FullName) != null);

                if (isDbSet)
                {
                    dbSetProperties.Add(property);
                }
            }

            return dbSetProperties;
        }
    }
}
