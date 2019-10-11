using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using WorQLess.Extensions;
using WorQLess.Models;

namespace WorQLess.Boosters
{
    public class TakeBooster : Booster
    {
        private static readonly MethodInfo TakeMethod;

        static TakeBooster()
        {
            var enumerableMethods = typeof(Queryable).GetMethods();

            TakeMethod = typeof(Queryable)
                .GetMethod(nameof(Queryable.Take));
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
            var count = property.Value.ToObject<int>();

            var fieldValue = CallMethod
            (
                typeCreator,
                parameter,
                Expression.Constant(count),
                TakeMethod
            );

            fields.Add(property.Name, fieldValue);
        }
    }
}