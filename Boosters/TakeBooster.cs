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
    public class TakeBooster : IBooster
    {
        private static MethodInfo TakeMethod;

        static TakeBooster()
        {
            var enumerableMethods = typeof(Queryable).GetMethods();

            TakeMethod = typeof(Queryable)
                .GetMethod(nameof(Queryable.Take));
        }

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
            var count = property.Value.ToObject<int>();

            if (fields.Any())
            {
                var lastField = fields.Last();
                var type = lastField.Value.ReturnType.GetGenericArguments().LastOrDefault();

                var method = TakeMethod
                    .MakeGenericMethod(type);

                var _expression = Expression.Call
                (
                    method,
                    lastField.Value.Expression,
                    Expression.Constant(count)
                );

                fields.Remove(lastField.Key);
                var fieldValue = new FieldExpression(_expression, parameter);
                fields.Add(property.Name, fieldValue);
            }
            else
            {
                var type = expression.Type.GetGenericArguments().LastOrDefault();

                var method = TakeMethod
                    .MakeGenericMethod(type);

                var _expression = Expression.Call
                (
                    method,
                    expression,
                    Expression.Constant(count)
                );

                var fieldValue = new FieldExpression(_expression, parameter);
                fields.Add(property.Name, fieldValue);
            }
        }
    }
}