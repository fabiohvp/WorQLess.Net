using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace WorQLess.Boosters
{
    public class OrderByDescBooster : IBooster
    {
        private static MethodInfo OrderByDescendingMethod;

        static OrderByDescBooster()
        {
            var enumerableMethods = typeof(Enumerable).GetMethods();

            OrderByDescendingMethod = enumerableMethods
                .First(o =>
                    o.Name == nameof(Enumerable.OrderByDescending)
                    && o.GetParameters().Length == 2
                );
        }

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
            var jArray = (JArray)property.Value;
            var lastField = fields.Last();
            var type = lastField.Value.Type.GetGenericArguments().LastOrDefault();
            var projection = typeCreator.BuildExpression(type, jArray, false);

            var method = OrderByDescendingMethod
                .MakeGenericMethod(type, projection.Type);

            var _expression = Expression.Call
            (
                method,
                lastField.Value.Expression,
                projection.GetLambdaExpression()
            );

            fields.Remove(lastField.Key);
            var fieldValue = new FieldExpression(_expression, initialParameter);
            fields.Add(property.Name, fieldValue);
        }
    }
}