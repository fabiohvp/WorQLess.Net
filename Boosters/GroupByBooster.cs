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

                var fieldValue = Boost4
                (
                    type,
                    GroupByMethod,
                    lastField.Value.Expression,
                    projection,
                    parameter
                );

                fieldValue.Parameter = lastField.Value.Parameter;
                fields.Remove(lastField.Key);
                fields.Add(property.Name, fieldValue);
            }
            else
            {
                //var type = expression.Type.GetGenericArguments().LastOrDefault();
                //var key = GetKey(typeCreator, type, expression, (JArray)jArray[0]);

                //var method = GroupByMethod
                //    .MakeGenericMethod(type, key.ReturnType);

                //var _expression = Expression.Call
                //(
                //    method,
                //    expression,
                //    key.GetLambdaExpression()
                //);

                //var fieldValue = new FieldExpression(_expression, parameter);
                //var otherArgs = new JArray(jArray.Skip(1));
                //var pr = new ProjectionRequest
                //{
                //    Name = "Select",
                //    Args = otherArgs
                //};
                //var otherProjections = WQL.TypeCreator.BuildExpression(fieldValue.ReturnType, pr);

                //var x = Expression.Parameter(otherProjections.ReturnType);

                //var z = fieldValue.Combine(otherProjections, fieldValue.Parameter);

                ////var fieldValue = new FieldExpression(_expression, parameter);
                //fields.Add(property.Name, fieldValue);
            }
        }

        private IFieldExpression GetKey(TypeCreator typeCreator, Type sourceType, Expression expression, JArray jArray)
        {
            var key = typeCreator.BuildExpression(sourceType, jArray);
            return key;
        }
    }
}