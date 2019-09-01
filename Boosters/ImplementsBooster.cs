using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace WorQLess.Net.Boosters
{
    public class ImplementsBooster : IBooster
    {
        public virtual void Boost
        (
            TypeCreator typeCreator,
            Type sourceType,
            Type propertyType,
            IDictionary<string, IFieldExpression> fields,
            JProperty property,
            Expression expression,
            ParameterExpression initialParameter
        )
        {
            var _properties = ((JArray)property.Value)
                .Cast<JValue>()
                .Select(o => o.Value.ToString());

            var types = Assembly
                .GetExecutingAssembly()
                .GetTypes()
                .AsParallel()
                .Where(o => _properties.Contains(o.Name))
                .ToArray();

            fields
                .Values
                .Last()
                .Interfaces = types;
        }
    }
}