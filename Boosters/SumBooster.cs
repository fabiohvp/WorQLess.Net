using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace WorQLess.Boosters
{
    public class SumBooster : IBooster
    {
        private static MethodInfo SumMethod;

        static SumBooster()
        {
            var enumerableMethods = typeof(Queryable).GetMethods();

            SumMethod = enumerableMethods
                .First(o =>
                    o.Name == nameof(Queryable.Sum)
                    && o.ReturnType == typeof(decimal?)
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

            if (fields.Any())
            {
                var lastField = fields.Last();
                var type = lastField.Value.Type.GetGenericArguments().LastOrDefault();
                var projection = typeCreator.BuildExpression(type, jArray, false);

                var method = SumMethod
                    .MakeGenericMethod(type);

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
            else
            {
                var type = expression.Type.GetGenericArguments().LastOrDefault();
                var projection = typeCreator.BuildExpression(type, jArray, false);

                var method = SumMethod
                    .MakeGenericMethod(type);

                var _expression = Expression.Call
                (
                    method,
                    expression,
                    projection.GetLambdaExpression()
                );

                var fieldValue = new FieldExpression(_expression, initialParameter);
                fields.Add(property.Name, fieldValue);
            }
        }
    }
}