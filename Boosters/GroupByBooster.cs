using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using WorQLess.Extensions;
using WorQLess.Models;

namespace WorQLess.Boosters
{
    public class GroupByBooster : Booster
    {
        private static readonly MethodInfo AsQueryableMethod;
        private static readonly MethodInfo GroupByMethod;
        private static readonly MethodInfo SelectMethod;

        static GroupByBooster()
        {
            var enumerableMethods = typeof(Queryable).GetMethods();

            AsQueryableMethod = enumerableMethods
                .First(o =>
                    o.Name == nameof(Queryable.AsQueryable)
                    && o.GetGenericArguments().Length == 1
                );

            //IQueryable<IGrouping<TKey, TSource>> GroupBy<TSource, TKey>(this IQueryable<TSource> source, Expression<Func<TSource, TKey>> keySelector);
            GroupByMethod = enumerableMethods
                .First(o =>
                    o.Name == nameof(Queryable.GroupBy)
                    && o.GetGenericArguments().Length == 2
                    && o.GetParameters().Length == 2
                );

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

            var fieldValue = CallMethod
            (
                typeCreator,
                parameter,
                (JArray)property.Value,
                GroupByMethod,
                createAnonymousProjection: true
            );

            fields.Add(property.Name, fieldValue);
        }
    }
}