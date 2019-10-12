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

            if (parameter.Type.FullName.Contains("IGrouping"))
            {
                var lastField = fields.LastOrDefault();
                var entityType = parameter.Type.GetGenericArguments().Last();

                if (lastField.Value == null)
                {
                    throw new NotImplementedException("Nested SelectMany still not implemented");
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