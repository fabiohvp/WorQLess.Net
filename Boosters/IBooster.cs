using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq.Expressions;
using WorQLess.Extensions;
using WorQLess.Models;

namespace WorQLess.Boosters
{
    public interface IBooster
    {
        void Boost
        (
            //current instance
            TypeCreator typeCreator,
            //initial lambda parameter (o => o.Name) //parameter = o
            //this is used when you want to convert you current expression to lambda expression (IFieldExpression.GetLambdaExpression())
            Expression expression,
            //property from json query ($as: 'person') //property.Name = "$as", property.Value = "person"
            //in case property being an Array or JObject, property.Value must be casted and treated according
            JProperty property,
            IDictionary<string, IFieldExpression> fields
        );

        //IFieldExpression Boost2(TypeCreator typeCreator, ParameterExpression parameter, JArray jArray);
    }
}