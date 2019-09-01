using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace WorQLess.Net.Boosters
{
    public interface IBooster
    {
        void Boost
        (
            //current instance
            TypeCreator typeCreator,
            //query type (IQueryable<sourceType>)
            Type sourceType,
            //current property type (o => o.Name) // propertyType = "string"
            Type propertyType,
            //current object fields (o => new { Name, Age }) //fields = { { "Name", IFieldExpression }, { "Age", IFieldExpression } }
            IDictionary<string, IFieldExpression> fields,
            //property from json query ($as: 'person') //property.Name = "$as", property.Value = "person"
            //in case property being an Array or JObject, property.Value must be casted and treated according
            JProperty property,
            //current expression (o => o.Name) //expression = o.Name
            Expression expression,
            //initial lambda parameter (o => o.Name) //initialParameter = o
            //this is used when you want to convert you current expression to lambda expression (IFieldExpression.GetLambdaExpression())
            ParameterExpression initialParameter
        );
    }
}