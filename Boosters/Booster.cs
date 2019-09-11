using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using WorQLess.Extensions;
using WorQLess.Models;

namespace WorQLess.Boosters
{
    public abstract class Booster : IBooster
    {
        public abstract void Boost(TypeCreator typeCreator, Type sourceType, Type propertyType, IDictionary<string, IFieldExpression> fields, JProperty property, Expression expression, ParameterExpression parameter);

        public virtual IFieldExpression Boost2(TypeCreator typeCreator, Type propertyType, JArray jArray, Expression expression, ParameterExpression parameter)
        {
            return null;
        }
    }
}
