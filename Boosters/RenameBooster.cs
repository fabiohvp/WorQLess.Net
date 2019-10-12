using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using WorQLess.Extensions;
using WorQLess.Models;

namespace WorQLess.Boosters
{
    public class RenameBooster : Booster
    {
        public override void Boost
        (
            TypeCreator typeCreator,
            Expression expression,
            JProperty property,
            IDictionary<string, IFieldExpression> fields
        )
        {
            var configuration = (JArray)property.Value;
            var jArray = new JArray(configuration.First());
            var projection = typeCreator.BuildProjection(expression, jArray, false);
            var name = configuration.Last();

            fields.Add
            (
                name.ToString(),
                projection
            );
        }
    }
}