using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using WorQLess.Extensions;
using WorQLess.Models;

namespace WorQLess.Boosters
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
            ParameterExpression parameter
        )
        {
            if (fields.Any())
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
            else
            {
                // fields.Add
                //(
                //    property.Value.ToString(),
                //    new FieldExpression(expression, parameter)
                //);
            }
        }
    }
}