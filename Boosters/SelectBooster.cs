using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace WorQLess.Boosters
{
    public class SelectBooster : IBooster
    {
        private static MethodInfo SelectMethod;
        private static readonly MethodInfo ToListMethod;

        static SelectBooster()
        {
            var enumerableMethods = typeof(Enumerable).GetMethods();

            SelectMethod = enumerableMethods
                .First(o =>
                    o.Name == nameof(Enumerable.Select)
                    && o.GetParameters().Length == 2
                );

            ToListMethod = enumerableMethods
                .First(o => o.Name == nameof(Enumerable.ToList));
        }

        private FieldExpression Select(TypeCreator typeCreator, PropertyInfo propertyInfo, JProperty property, Expression expression, ParameterExpression initialParameter)
        {
            var propertyType = propertyInfo.PropertyType.GetGenericArguments().LastOrDefault();
            var _expression = Expression.Property(expression, propertyInfo);

            var jArray = (JArray)property.Value;
            var projection = typeCreator.BuildExpression(propertyType, jArray);

            var method = SelectMethod
                .MakeGenericMethod(propertyType, projection.Type);

            var selectExpression = Expression.Call
            (
                method,
                _expression,
                projection.GetLambdaExpression()
            );

            //return new FieldExpression(selectExpression, projection.InitialParameter);

            //ToList() only when using aspnet core
            var toListMethod = ToListMethod
                .MakeGenericMethod(projection.Type);

            var toListExpression = Expression.Call
            (
                toListMethod,
                selectExpression
            );

            return new FieldExpression(toListExpression, projection.InitialParameter);
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

            var _fields = new Dictionary<string, IFieldExpression>();
            _fields.Add(_property.Name, fieldValue);

            foreach (var __property in properties.Skip(1))
            {
                var booster = typeCreator.GetBooster(__property.Name);

                if (booster == default(IBooster))
                {
                    throw new InvalidOperationException("$select first property is you query and additional properties must be boosters");
                }
                else
                {
                    booster.Boost(typeCreator, sourceType, fieldValue.Type, _fields, __property, fieldValue.Expression, fieldValue.InitialParameter);
                }
            }


            fields.Add(_fields.Last());
        }
    }
}