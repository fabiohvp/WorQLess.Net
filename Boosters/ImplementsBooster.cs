using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using WorQLess.Extensions;
using WorQLess.Models;

namespace WorQLess.Boosters
{
    public class ImplementsBooster : Booster
    {
        public override void Boost
        (
            TypeCreator typeCreator,
            Type sourceType,
            Type propertyType,
            IDictionary<string, IFieldExpression> fields,
            JProperty property,
            Expression expression,
            ParameterExpression parameter
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