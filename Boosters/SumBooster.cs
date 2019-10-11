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
    public class SumBooster : Booster
    {
        private static readonly MethodInfo SumMethod1;
        private static readonly MethodInfo SumMethod2;

        static SumBooster()
        {
            var enumerableMethods1 = typeof(Enumerable).GetMethods();

            SumMethod1 = enumerableMethods1
                .First(o =>
                    o.Name == nameof(Enumerable.Sum)
                    && o.ReturnType == typeof(decimal?)
                    && o.GetParameters().Length == 2
                );

            var enumerableMethods2 = typeof(Queryable).GetMethods();

            SumMethod2 = enumerableMethods2
                .First(o =>
                    o.Name == nameof(Queryable.Sum)
                    && o.ReturnType == typeof(decimal?)
                    && o.GetParameters().Length == 2
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

                var sumProjection = typeCreator.BuildProjection(entityParameter, (JArray)property.Value, false);
                var sumLamda = sumProjection.GetLambdaExpression();

                var method = SumMethod1.MakeGenericMethod(entityType);

                if (lastField.Value == null)
                {
                    var sumCall = Expression.Call(method, parameter, sumLamda);

                    var fieldValue = new FieldExpression(sumCall, parameter);
                    fields.Add(property.Name, fieldValue);
                }
                else
                {
                    var groupByCall = lastField.Value.Expression;
                    var sumCall = Expression.Call(method, parameter, sumLamda);

                    var selectLambda = Expression.Lambda(sumCall, parameter as ParameterExpression);
                    var selectCall = Expression.Call(typeof(Queryable), "Select", new Type[] { parameter.Type, selectLambda.Body.Type }, groupByCall, Expression.Quote(selectLambda));

                    var fieldValue = new FieldExpression(selectCall, lastField.Value.Parameter);
                    fields.Remove(lastField.Key);
                    fields.Add(property.Name, fieldValue);
                }
            }
            else
            {
                var fieldValue = CallMethod
                (
                    typeCreator,
                    parameter,
                    (JArray)property.Value,
                    SumMethod2,
                    createAnonymousProjection: false
                );

                fields.Add(property.Name, fieldValue);
            }
        }

        //sample code from https://stackoverflow.com/a/39731153/3191072
        //var query = db.Documents.AsQueryable();
        //// query.GroupBy(a => 1)
        //var a = Expression.Parameter(typeof(Document), "a");
        //var groupKeySelector = Expression.Lambda(Expression.Constant(1), a);
        //var groupByCall = Expression.Call(typeof(Queryable), "GroupBy",
        //    new Type[] { a.Type, groupKeySelector.Body.Type },
        //    query.Expression, Expression.Quote(groupKeySelector));
        //// c => c.Amount
        //var c = Expression.Parameter(typeof(Document), "c");
        //var sumSelector = Expression.Lambda(Expression.PropertyOrField(c, "Amount"), c);
        //// b => b.Sum(c => c.Amount)
        //var b = Expression.Parameter(groupByCall.Type.GetGenericArguments().Single(), "b");
        //var sumCall = Expression.Call(typeof(Enumerable), "Sum",
        //    new Type[] { c.Type },
        //    b, sumSelector);
        //// query.GroupBy(a => 1).Select(b => b.Sum(c => c.Amount))
        //var selector = Expression.Lambda(sumCall, b);
        //var selectCall = Expression.Call(typeof(Queryable), "Select",
        //    new Type[] { b.Type, selector.Body.Type },
        //    groupByCall, Expression.Quote(selector));
        //// selectCall is our expression, let test it
        //var result = query.Provider.CreateQuery(selectCall);
    }
}