using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace WorQLess.Net.Boosters
{
    public class AsBooster : IBooster
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
            var lastKey = fields
                .Keys
                .Last();
            var lastExpression = fields[lastKey];

            fields.Remove(lastKey);
            fields.Add
            (
                property.Value.ToString(),
                lastExpression
            );
        }
    }
}