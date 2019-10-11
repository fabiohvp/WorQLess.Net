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

            if (parameter.Type.FullName.Contains("IGrouping"))
            {
                var lastField = fields.LastOrDefault();

                if (lastField.Value == null)
                {

                }
                else
                {
                    var entityType = parameter.Type.GetGenericArguments().Last();
                    var entityParameter = Expression.Parameter(entityType);

                    var selectProjection = typeCreator.BuildProjection(parameter, (JArray)property.Value, false);
                    var selectLamda = selectProjection.GetLambdaExpression();

                    var groupByCall = lastField.Value.Expression;
                    var selectCall = Expression.Call(typeof(Queryable), "Select", new Type[] { parameter.Type, selectLamda.Body.Type }, groupByCall, Expression.Quote(selectLamda));

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
                    SelectMethod,
                    createAnonymousProjection: false
                );

                fields.Add(property.Name, fieldValue);
            }
        }
    }
}