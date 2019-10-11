using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using WorQLess.Extensions;
using WorQLess.Models;

namespace WorQLess.Boosters
{
    public class CountBooster : Booster
    {
        private static MethodInfo CountMethod;

        static CountBooster()
        {
            var enumerableMethods = typeof(Queryable).GetMethods();

            CountMethod = enumerableMethods
                .First(o =>
                    o.Name == nameof(Queryable.Count)
                    && o.GetParameters().Length == 1
                );
        }

        public override void Boost
        (
            TypeCreator typeCreator,
            Expression expression,
            JProperty property,
            IDictionary<string, IFieldExpression> fields
        )
        {
            var lastField = fields.LastOrDefault();
            var parameter = GetParameter(fields, expression);
            var entityType = parameter.Type;
            var method = CountMethod
               .MakeGenericMethod(entityType);

            if (lastField.Value == null)
            {
                var queryType = typeof(IQueryable<>).MakeGenericType(entityType);
                var queryParameter = Expression.Parameter(queryType);

                var countCall = Expression.Call
                (
                    method,
                    queryParameter
                );

                var fieldValue = new FieldExpression(countCall, queryParameter);
                fields.Add(property.Name, fieldValue);
            }
            else
            {
                var countCall = Expression.Call
                (
                    method,
                    lastField.Value.Expression
                );

                var fieldValue = new FieldExpression(countCall, lastField.Value.Parameter);
                fields.Remove(lastField.Key);
                fields.Add(property.Name, fieldValue);
            }
        }
    }
}