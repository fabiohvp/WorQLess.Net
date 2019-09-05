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
    public class GroupByBooster : IBooster
    {
        private static MethodInfo GroupByMethod;

        static GroupByBooster()
        {
            var enumerableMethods = typeof(Queryable).GetMethods();

            //IQueryable<IGrouping<TKey, TSource>> GroupBy<TSource, TKey>(this IQueryable<TSource> source, Expression<Func<TSource, TKey>> keySelector);
            GroupByMethod = enumerableMethods
                .First(o =>
                    o.Name == nameof(Queryable.GroupBy)
                    && o.GetGenericArguments().Length == 2
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
            ParameterExpression parameter
        )
        {
            var jArray = (JArray)property.Value;

            if (fields.Any())
            {
                var lastField = fields.Last();
                var type = lastField.Value.ReturnType.GetGenericArguments().LastOrDefault();
                var projection = typeCreator.BuildExpression(type, jArray);

                var method = GroupByMethod
                    .MakeGenericMethod(type);

                var _expression = Expression.Call
                (
                    method,
                    lastField.Value.Expression,
                    projection.GetLambdaExpression()
                );

                fields.Remove(lastField.Key);
                var fieldValue = new FieldExpression(_expression, parameter);
                fields.Add(property.Name, fieldValue);
            }
            else
            {
                var type = expression.Type.GetGenericArguments().LastOrDefault();
                var projection = typeCreator.BuildExpression(type, jArray);

                var groupingType = typeof(IGrouping<,>).MakeGenericType(projection.ReturnType, type);
                var queryType = typeof(IQueryable<>).MakeGenericType(groupingType);

                var method = GroupByMethod
                    .MakeGenericMethod(type, projection.ReturnType);

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