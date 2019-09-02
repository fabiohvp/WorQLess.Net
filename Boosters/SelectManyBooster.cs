using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace WorQLess.Boosters
{
    public class SelectManyBooster : IBooster
    {
        private static MethodInfo SelectManyMethod;
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

        private FieldExpression Select(TypeCreator typeCreator, PropertyInfo propertyInfo, JProperty property, Expression expression, ParameterExpression initialParameter)
        {
            var _expression = Expression.Property(expression, propertyInfo);
            var propertyType = propertyInfo.PropertyType.GetGenericArguments().LastOrDefault();

            var jArray = (JArray)property.Value;
            var projection = typeCreator.BuildExpression(propertyType, jArray, false);
            var type = projection.Type.GetGenericArguments().LastOrDefault();

            //db.ReceitaMunicipio.SelectMany(o => o.EsferaAdministrativa.ReceitasMunicipio.Select(p => new { p.Arrecadada }));
            var method = SelectManyMethod
                .MakeGenericMethod(projection.InitialParameter.Type, type);

            var selectExpression = Expression.Call
            (
                method,
                _expression,
                projection.GetLambdaExpression()
            );

            return new FieldExpression(selectExpression, initialParameter);

            ////ToList() only when using aspnet core
            //var toListMethod = ToListMethod
            //	.MakeGenericMethod(projection.Type);

            //var toListExpression = Expression.Call
            //(
            //	toListMethod,
            //	selectExpression
            //);

            //return new FieldExpression(toListExpression, projection.InitialParameter);
        }

        public virtual void Boost
        (
            TypeCreator typeCreator,
            Type sourceType,
            Type propertyType,
            IDictionary<string, IFieldExpression> fields,
            JProperty property,
            Expression expression,
            ParameterExpression initialParameter
        )
        {
            var jObject = (JObject)property.Value;
            var properties = jObject.Properties();
            var _property = properties.First();
            var propertyInfo = propertyType.GetProperty(_property.Name);
            var fieldValue = Select(typeCreator, propertyInfo, _property, expression, initialParameter);
            fields.Add(_property.Name, fieldValue);
        }
    }
}