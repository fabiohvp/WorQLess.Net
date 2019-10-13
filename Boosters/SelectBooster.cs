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
    public class SelectBooster : Booster
    {
        private static readonly MethodInfo SelectMethod;

        static SelectBooster()
        {
            var enumerableMethods = typeof(Queryable).GetMethods();

            SelectMethod = enumerableMethods
                .First(o =>
                    o.Name == nameof(Queryable.Select)
                    && o.GetParameters()[1].ParameterType.GetGenericArguments()[0].GetGenericArguments().Length == 2
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
            var parameter = GetParameter(fields, expression);
            var lastField = fields.LastOrDefault();

            if (lastField.Value == null)
            {
                var entityType = parameter.Type;
                var entityParameter = parameter;
                var asQueryableCall = parameter;

                if (parameter.Type.FullName.Contains("IQueryable") || parameter.Type.FullName.Contains("IEnumerable") || parameter.Type.FullName.Contains("ICollection"))
                {
                    entityType = parameter.Type.GetGenericArguments().Last();
                    entityParameter = Expression.Parameter(entityType);
                    asQueryableCall = AsQueryable(parameter);
                }
                else
                {
                    var queryType = typeof(IQueryable<>).MakeGenericType(parameter.Type);
                    parameter = Expression.Parameter(queryType);
                    asQueryableCall = parameter;
                }

                var selectProjection = typeCreator.BuildProjection(entityParameter, (JArray)property.Value, false);
                var selectLambda = selectProjection.GetLambdaExpression();

                var selectCall = Expression.Call(typeof(Queryable), nameof(Queryable.Select), new Type[] { entityType, selectLambda.Body.Type }, asQueryableCall, Expression.Quote(selectLambda));
                var asEnumerableCall = Expression.Call(typeof(Enumerable), nameof(Enumerable.AsEnumerable), new Type[] { selectLambda.Body.Type }, selectCall);

                var fieldValue = new FieldExpression(asEnumerableCall, parameter);
                fields.Add(property.Name, fieldValue);
            }
            else
            {
                var selectProjection = typeCreator.BuildProjection(parameter, (JArray)property.Value, false);
                var selectLambda = selectProjection.GetLambdaExpression();

                var groupByCall = lastField.Value.Expression;
                var selectCall = Expression.Call(typeof(Queryable), nameof(Queryable.Select), new Type[] { parameter.Type, selectLambda.Body.Type }, groupByCall, Expression.Quote(selectLambda));

                var fieldValue = new FieldExpression(selectCall, parameter);

                fieldValue.Parameter = lastField.Value.Parameter;
                fields.Remove(lastField.Key);
                fields.Add(property.Name, fieldValue);
            }
        }
    }
}