using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace WorQLess.Net.Boosters
{
    public class TakeBooster : IBooster
    {
        private static MethodInfo TakeMethod;

        static TakeBooster()
        {
            var enumerableMethods = typeof(Enumerable).GetMethods();

            TakeMethod = typeof(Enumerable)
                .GetMethod(nameof(Enumerable.Take));
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
            var count = property.Value.ToObject<int>();
            var lastField = fields.Last();
            var type = lastField.Value.Type.GetGenericArguments().LastOrDefault();

            var method = TakeMethod
                .MakeGenericMethod(type);

            var _expression = Expression.Call
            (
                method,
                lastField.Value.Expression,
                Expression.Constant(count)
            );

            fields.Remove(lastField.Key);
            var fieldValue = new FieldExpression(_expression, initialParameter);
            fields.Add(property.Name, fieldValue);
        }
    }
}