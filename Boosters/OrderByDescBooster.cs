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
    public class OrderByDescBooster : Booster
    {
        private static MethodInfo OrderByDescendingMethod;

        static OrderByDescBooster()
        {
            var enumerableMethods = typeof(Queryable).GetMethods();

            OrderByDescendingMethod = enumerableMethods
                .First(o =>
                    o.Name == nameof(Queryable.OrderByDescending)
                    && o.GetParameters().Length == 2
                );
        }

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
            var jArray = (JArray)property.Value;
            var lastField = fields.Last();
            var type = lastField.Value.ReturnType.GetGenericArguments().LastOrDefault();
            var projection = typeCreator.BuildExpression(type, jArray, false);

            var method = OrderByDescendingMethod
                .MakeGenericMethod(type, projection.ReturnType);

            var _expression = Expression.Call
            (
                method,
                lastField.Value.Expression,
                projection.GetLambdaExpression()
            );

            fields.Remove(lastField.Key);
            var fieldValue = new FieldExpression(_expression, parameter);
            fields.Add(property.Name, fieldValue);
        }
    }
}