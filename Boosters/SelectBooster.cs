using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using WorQLess.Extensions;
using WorQLess.Models;
using WorQLess.Requests;

namespace WorQLess.Boosters
{
    public class SelectBooster : Booster
    {
        private static readonly MethodInfo AsQueryableMethod;
        private static readonly MethodInfo SelectMethod;

        static SelectBooster()
        {
            var enumerableMethods = typeof(Queryable).GetMethods();

            AsQueryableMethod = enumerableMethods
                .First(o =>
                    o.Name == nameof(Queryable.AsQueryable)
                    && o.GetGenericArguments().Length == 1
                );

            SelectMethod = enumerableMethods
                .First(o =>
                    o.Name == nameof(Queryable.Select)
                    && o.GetParameters()[1].ParameterType.GetGenericArguments()[0].GetGenericArguments().Length == 2
                );
        }

        public override IFieldExpression Boost2(TypeCreator typeCreator, Type propertyType, JArray jArray, Expression expression, ParameterExpression parameter)
        {
            var projection = typeCreator.BuildExpression(propertyType, jArray);
            var _expression = expression;

            if (_expression.Type.GetInterfaces().Any(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IEnumerable<>)))
            {
                var asQueryableMethod = AsQueryableMethod
                    .MakeGenericMethod(propertyType);

                _expression = Expression.Call
                (
                    asQueryableMethod,
                    _expression
                );
            }

            var method = SelectMethod
                .MakeGenericMethod(propertyType, projection.ReturnType);

            var selectExpression = Expression.Call
            (
                method,
                _expression,
                projection.GetLambdaExpression()
            );

            return new FieldExpression(selectExpression, projection.Parameter);

            //ToList() only when using aspnet core
            //var toListMethod = ToListMethod
            //    .MakeGenericMethod(projection.Type);

            //var toListExpression = Expression.Call
            //(
            //    toListMethod,
            //    selectExpression
            //);

            //return new FieldExpression(toListExpression, projection.Parameter);
        }

        private IFieldExpression Boost3
        (
            TypeCreator typeCreator,
            Type queryType,
            Type propertyType,
            ParameterExpression parameter,
            IProjectionRequest projectionRequest
        )
        {
            var projection = typeCreator.BuildExpression(propertyType, projectionRequest);
            Expression expression = parameter;

            if (expression.Type.GetInterfaces().Any(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IEnumerable<>)))
            {
                var asQueryableMethod = AsQueryableMethod
                    .MakeGenericMethod(propertyType);

                expression = Expression.Call
                (
                    asQueryableMethod,
                    expression
                );
            }

            var method = SelectMethod
                .MakeGenericMethod(propertyType, projection.ReturnType);

            var selectExpression = Expression.Call
            (
                method,
                expression,
                projection.GetLambdaExpression()
            );

            var fieldValue = new FieldExpression(selectExpression, parameter);
            return fieldValue;
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
            if (property.Value is JArray)
            {
                var projectionRequest = new ProjectionRequest
                {
                    Name = nameof(Queryable.Select),
                    Args = property.Value
                };

                if (fields.Any())
                {
                    var lastKey = fields.Select(o => o.Key).First();
                    var lastField = fields[lastKey];
                    var queryType = lastField.ReturnType;//.GetGenericArguments().LastOrDefault();
                    var _parameter = Expression.Parameter(queryType);
                    propertyType = queryType.GetGenericArguments().LastOrDefault();

                    var fieldValue = Boost3
                    (
                        typeCreator,
                        queryType,
                        propertyType,
                        _parameter,
                        projectionRequest
                    );

                    fieldValue = fieldValue.Combine(lastField, _parameter);
                    fieldValue.Parameter = lastField.Parameter;

                    fields.Remove(lastKey);
                    fields.Add(property.Name, fieldValue);
                }
                else
                {
                    var queryType = typeof(IQueryable<>).MakeGenericType(sourceType);
                    var _parameter = Expression.Parameter(queryType);

                    var fieldValue = Boost3
                    (
                        typeCreator,
                        queryType,
                        propertyType,
                        _parameter,
                        projectionRequest
                    );

                    fields.Add(property.Name, fieldValue);
                }
            }
            else
            {
                var jObject = (JObject)property.Value;
                var properties = jObject.Properties();
                var _property = properties.First();
                var propertyInfo = propertyType.GetProperty(_property.Name);
                var _propertyType = propertyInfo.PropertyType.GetGenericArguments().LastOrDefault();
                var _expression = Expression.Property(expression, propertyInfo);
                var fieldValue = Boost2(typeCreator, _propertyType, (JArray)_property.Value, _expression, parameter);

                var _fields = new Dictionary<string, IFieldExpression>();
                _fields.Add(_property.Name, fieldValue);

                foreach (var __property in properties.Skip(1))
                {
                    if (WQL.Boosters.ContainsKey(property.Name))
                    {
                        WQL.Boosters[property.Name].Boost(typeCreator, sourceType, fieldValue.ReturnType, _fields, __property, fieldValue.Expression, fieldValue.Parameter);
                    }
                    else
                    {
                        throw new InvalidOperationException("$select - first property is your query and additional properties must be boosters");
                    }
                }


                fields.Add(_fields.Last());
            }
        }
    }
}