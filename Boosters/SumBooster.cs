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
    public class SumBooster : Booster
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

            if (fields.Any())
            {
                var lastField = fields.Last();
                var type = lastField.Value.ReturnType.GetGenericArguments().LastOrDefault();
                var projection = typeCreator.BuildExpression(type, jArray, false);

                var method = SumMethod
                    .MakeGenericMethod(type);

                var _expression = Expression.Call
                (
                    method,
                    lastField.Value.Expression,
                    projection.GetLambdaExpression()
                );

                var fieldValue = (IFieldExpression)new FieldExpression(_expression, parameter);
                fieldValue = fieldValue.Combine(lastField.Value, parameter);
                fieldValue.Parameter = lastField.Value.Parameter;

                fields.Remove(lastField.Key);
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

                var fieldValue = new FieldExpression(_expression, parameter);
                fields.Add(property.Name, fieldValue);
            }
        }
    }
}