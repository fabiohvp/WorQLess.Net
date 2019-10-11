using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using WorQLess.Extensions;
using WorQLess.Models;

namespace WorQLess.Boosters
{
    public class OrderByAscBooster : Booster
    {
        private static readonly MethodInfo OrderByAscendingMethod;

        static OrderByAscBooster()
        {
            var enumerableMethods = typeof(Queryable).GetMethods();

            OrderByAscendingMethod = enumerableMethods
                .First(o =>
                    o.Name == nameof(Queryable.OrderBy)
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

            var fieldValue = CallMethod
            (
                typeCreator,
                parameter,
                (JArray)property.Value,
                OrderByAscendingMethod,
                createAnonymousProjection: false
            );

            fields.Add(property.Name, fieldValue);
        }
    }
}