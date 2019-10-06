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
    public class OrderByDescBooster : Booster
    {
        private static readonly MethodInfo OrderByDescendingMethod;

        static OrderByDescBooster()
        {
            var enumerableMethods = typeof(Queryable).GetMethods();

            OrderByDescendingMethod = enumerableMethods
                .First(o =>
                    o.Name == nameof(Queryable.OrderByDescending)
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
            Boost3(typeCreator, sourceType, propertyType, fields, property, expression, parameter, OrderByDescendingMethod);
        }
    }
}