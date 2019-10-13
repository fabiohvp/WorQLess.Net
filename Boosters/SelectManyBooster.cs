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
    public class SelectManyBooster : Booster
    {
        private static readonly MethodInfo SelectManyMethod;

        static SelectManyBooster()
        {
            var enumerableMethods = typeof(Queryable).GetMethods();

            SelectManyMethod = enumerableMethods
                .First(o =>
                    o.Name == nameof(Queryable.SelectMany)
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

            //if (lastField.Value != null && (parameter.Type.FullName.Contains("IQueryable") || parameter.Type.FullName.Contains("IEnumerable") || parameter.Type.FullName.Contains("ICollection")))
            //{
            //    var fieldValue = CallMethod
            //    (
            //        typeCreator,
            //        parameter,
            //        (JArray)property.Value,
            //        SelectManyMethod,
            //        createAnonymousProjection: false
            //    );

            //    fields.Add(property.Name, fieldValue);
            //}
            //else
            //{
            if (lastField.Value == null)
            {
                var queryType = typeof(IQueryable<>).MakeGenericType(parameter.Type);
                var queryTypeParameter = Expression.Parameter(queryType);

                var selectProjection = typeCreator.BuildProjection(parameter, (JArray)property.Value, false);
                var selectLambda = selectProjection.GetLambdaExpression();
                var returnType = selectProjection.Expression.Type.GetGenericArguments().Single();

                var selectCall = Expression.Call(typeof(Queryable), nameof(Queryable.SelectMany), new Type[] { parameter.Type, returnType }, queryTypeParameter, Expression.Quote(selectLambda));

                var fieldValue = new FieldExpression(selectCall, queryTypeParameter);
                fields.Add(property.Name, fieldValue);
            }
            else
            {
                var selectProjection = typeCreator.BuildProjection(parameter, (JArray)property.Value, false);
                var selectLambda = selectProjection.GetLambdaExpression();
                var returnType = selectProjection.Expression.Type.GetGenericArguments().Single();

                var groupByCall = lastField.Value.Expression;
                var selectCall = Expression.Call(typeof(Queryable), nameof(Queryable.SelectMany), new Type[] { parameter.Type, returnType }, groupByCall, Expression.Quote(selectLambda));

                var fieldValue = new FieldExpression(selectCall, parameter);

                fieldValue.Parameter = lastField.Value.Parameter;
                fields.Remove(lastField.Key);
                fields.Add(property.Name, fieldValue);
            }
            //}
        }
    }
}