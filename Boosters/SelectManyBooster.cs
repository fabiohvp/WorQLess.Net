using domain.Models.DWControleSocial;
using Newtonsoft.Json.Linq;
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

            if (parameter.Type.FullName.Contains("IGrouping"))
            {
                var lastField = fields.LastOrDefault();
                var entityType = parameter.Type.GetGenericArguments().Last();
                var entityParameter = Expression.Parameter(entityType);

                var queryType = typeof(IQueryable<>).MakeGenericType(entityType);
                var queryParameter = Expression.Parameter(queryType);

                var selectProjection = typeCreator.BuildProjection(parameter, (JArray)property.Value, false);
                var selectLamda = selectProjection.GetLambdaExpression();

                var method = SelectManyMethod.MakeGenericMethod(queryType, selectLamda.Body.Type.GetGenericArguments().LastOrDefault());
                var selectCall = Expression.Call(method, queryParameter, Expression.Quote(selectLamda));

                var fieldValue = new FieldExpression(selectCall, lastField.Value.Parameter);
                fields.Remove(lastField.Key);
                fields.Add(property.Name, fieldValue);


                var x = new List<FT_ReceitaMunicipio>()
                    .Select(o => new { o.IdTempo, o.Arrecadada })
                    .GroupBy(o => new { o.IdTempo })
                    .SelectMany(o => o.Select(p => p.Arrecadada));
            }
            else
            {
                var fieldValue = CallMethod
                (
                    typeCreator,
                    parameter,
                    (JArray)property.Value,
                    SelectManyMethod,
                    createAnonymousProjection: false
                );

                fields.Add(property.Name, fieldValue);
            }
        }
    }
}