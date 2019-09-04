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
            var enumerableMethods = typeof(Queryable).GetMethods();

            SelectMethod = enumerableMethods
                .First(o =>
                    o.Name == nameof(Queryable.Select)
                    && o.GetParameters()[1].ParameterType.GetGenericArguments()[0].GetGenericArguments().Length == 2
                );

            //ToListMethod = enumerableMethods
            //    .First(o => o.Name == nameof(Enumerable.ToList));
        }

        public FieldExpression Select(TypeCreator typeCreator, Type propertyType, JArray jArray, Expression expression, ParameterExpression initialParameter)
        {
            var projection = typeCreator.BuildExpression(propertyType, jArray);

            var method = SelectMethod
                .MakeGenericMethod(propertyType, projection.Type);

            var selectExpression = Expression.Call
            (
                method,
                expression,
                projection.GetLambdaExpression()
            );

            return new FieldExpression(selectExpression, projection.InitialParameter);

            //ToList() only when using aspnet core
            //var toListMethod = ToListMethod
            //    .MakeGenericMethod(projection.Type);

            //var toListExpression = Expression.Call
            //(
            //    toListMethod,
            //    selectExpression
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
            var _propertyType = propertyInfo.PropertyType.GetGenericArguments().LastOrDefault();
            var _expression = Expression.Property(expression, propertyInfo);
            var fieldValue = Select(typeCreator, _propertyType, (JArray)_property.Value, _expression, initialParameter);

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