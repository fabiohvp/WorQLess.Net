using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace WorQLess.Net.Boosters
{
    public class CountBooster : IBooster
    {
        private static MethodInfo CountMethod;

        static CountBooster()
        {
            var enumerableMethods = typeof(Enumerable).GetMethods();

            CountMethod = enumerableMethods
                .First(o =>
                    o.Name == nameof(Enumerable.Count)
                    && o.GetParameters().Length == 1
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
            var lastField = fields.Last();
            var type = lastField.Value.Type.GetGenericArguments().LastOrDefault();

            var method = CountMethod
                .MakeGenericMethod(type);

            var _expression = Expression.Call
            (
                method,
                lastField.Value.Expression
            );

            fields.Remove(lastField.Key);
            var fieldValue = new FieldExpression(_expression, initialParameter);
            fields.Add(property.Name, fieldValue);
        }
    }
}