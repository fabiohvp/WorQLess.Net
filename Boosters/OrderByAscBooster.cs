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
            Type sourceType,
            Type propertyType,
            IDictionary<string, IFieldExpression> fields,
            JProperty property,
            Expression expression,
            ParameterExpression parameter
        )
        {
            Boost3(typeCreator, sourceType, propertyType, fields, property, expression, parameter, OrderByAscendingMethod);
        }
    }
}