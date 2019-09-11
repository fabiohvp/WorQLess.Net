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
        private static readonly MethodInfo ToListMethod;

        static SelectManyBooster()
        {
            var enumerableMethods = typeof(Enumerable).GetMethods();

            SelectManyMethod = enumerableMethods
                .First(o =>
                    o.Name == nameof(Enumerable.SelectMany)
                    && o.GetParameters().Length == 2
                );

            ToListMethod = enumerableMethods
                .First(o => o.Name == nameof(Enumerable.ToList));
        }

        public override IFieldExpression Boost2(TypeCreator typeCreator, Type propertyType, JArray jArray, Expression expression, ParameterExpression parameter)
        {
            var projection = typeCreator.BuildExpression(propertyType, jArray, false);
            var type = projection.ReturnType.GetGenericArguments().LastOrDefault();

            //db.ReceitaMunicipio.SelectMany(o => o.EsferaAdministrativa.ReceitasMunicipio.Select(p => new { p.Arrecadada }));
            var method = SelectManyMethod
                .MakeGenericMethod(projection.Parameter.Type, type);

            var selectExpression = Expression.Call
            (
                method,
                expression,
                projection.GetLambdaExpression()
            );

            return new FieldExpression(selectExpression, parameter);

            ////ToList() only when using aspnet core
            //var toListMethod = ToListMethod
            //	.MakeGenericMethod(projection.Type);

            //var toListExpression = Expression.Call
            //(
            //	toListMethod,
            //	selectExpression
            //);

            //return new FieldExpression(toListExpression, projection.Parameter);
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
            var jObject = (JObject)property.Value;
            var properties = jObject.Properties();
            var _property = properties.First();
            var propertyInfo = propertyType.GetProperty(_property.Name);
            var _expression = Expression.Property(expression, propertyInfo);
            var _propertyType = propertyInfo.PropertyType.GetGenericArguments().LastOrDefault();
            var fieldValue = Boost2(typeCreator, _propertyType, (JArray)_property.Value, _expression, parameter);
            fields.Add(_property.Name, fieldValue);
        }
    }
}