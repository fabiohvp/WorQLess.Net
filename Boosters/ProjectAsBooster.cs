using Enflow;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using WorQLess.Extensions;
using WorQLess.Models;

namespace WorQLess.Boosters
{
    public class ProjectAsBooster : IBooster
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
            var lastKey = fields
                .Keys
                .Last();
            var lastExpression = fields[lastKey];

            var projection = Reflection
                .CreateProjection(sourceType, property.Value.ToString(), new Type[] { lastExpression.ReturnType }, null);

            var predicate = projection
                .GetType()
                .GetProperty(nameof(IProjection<object, object>.Predicate))
                .GetValue(projection);

            var _expression = System
                .Linq
                .Expressions
                .Expression
                .Invoke((Expression)predicate, lastExpression.GetLambdaExpression());

            fields.Remove(lastKey);
            fields.Add
            (
                lastKey,
                new FieldExpression(_expression, parameter)
                {
                    Interfaces = lastExpression.Interfaces
                }
            );
        }
    }
}