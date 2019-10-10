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
    public abstract class Booster : IBooster
    {
        private static readonly MethodInfo AsQueryableMethod;

        static Booster()
        {
            var enumerableMethods = typeof(Queryable).GetMethods();

            AsQueryableMethod = enumerableMethods
                .First(o =>
                    o.Name == nameof(Queryable.AsQueryable)
                    && o.GetGenericArguments().Length == 1
                );
        }

        public abstract void Boost(TypeCreator typeCreator, Type sourceType, Type propertyType, IDictionary<string, IFieldExpression> fields, JProperty property, Expression expression, ParameterExpression parameter);

        public virtual IFieldExpression Boost2(TypeCreator typeCreator, Type propertyType, JArray jArray, Expression expression, ParameterExpression parameter)
        {
            return null;
        }

        protected virtual IFieldExpression Boost4
        (
            Type type,
            MethodInfo methodInfo,
            Expression expression,
            IFieldExpression projection,
            ParameterExpression parameter
        )
        {
            var method = default(MethodInfo);
            var lambda = projection.GetLambdaExpression();

            if (methodInfo.GetGenericArguments().Length == 1)
            {
                method = methodInfo
                    .MakeGenericMethod(type);
            }
            else
            {
                method = methodInfo
                    .MakeGenericMethod(type, projection.ReturnType);
            }

            var _expression = Expression.Call
            (
                method,
                expression,
                Expression.Quote(lambda)
            );

            var fieldValue = new FieldExpression(_expression, parameter);
            return fieldValue;
        }

        protected virtual IFieldExpression Boost5
        (
            Type type,
            MethodInfo methodInfo,
            Expression expression,
            IFieldExpression projection,
            ParameterExpression parameter
        )
        {
            var queryType = typeof(IQueryable<>)
                .MakeGenericType(type);

            parameter = Expression.Parameter(queryType);
            expression = parameter;

            var fieldValue = Boost4
            (
                type,
                methodInfo,
                expression,
                projection,
                parameter
            );

            return fieldValue;
        }


        protected virtual void Boost3
        (
            TypeCreator typeCreator,
            Type sourceType,
            Type propertyType,
            IDictionary<string, IFieldExpression> fields,
            JProperty property,
            Expression expression,
            ParameterExpression parameter,
            MethodInfo methodInfo
        )
        {
            var jArray = (JArray)property.Value;

            if (fields.Any())
            {
                var lastField = fields.Last();
                var type = lastField.Value.ReturnType.GetGenericArguments().LastOrDefault();
                var projection = typeCreator.BuildExpression(type, jArray, false);

                var fieldValue = Boost4
                (
                    type,
                    methodInfo,
                    lastField.Value.Expression,
                    projection,
                    parameter
                );

                fieldValue = fieldValue.Combine(lastField.Value, parameter);
                fieldValue.Parameter = lastField.Value.Parameter;

                fields.Remove(lastField.Key);
                fields.Add(property.Name, fieldValue);
            }
            else
            {
                var type = expression.Type.GetGenericArguments().LastOrDefault();

                //if (type != default(Type))
                //{
                //    var projection = typeCreator
                //        .BuildExpression(type, jArray, false);

                //    var fieldValue = Boost4
                //    (
                //        type,
                //        methodInfo,
                //        expression,
                //        projection,
                //        parameter
                //    );

                //    fields.Add(property.Name, fieldValue);
                //}
                //else
                {
                    //type = expression.Type;
                    var projection = typeCreator
                        .BuildExpression(type, jArray, false);

                    //var lambda = Expression.Lambda
                    //(
                    //    typeof(Func<,>).MakeGenericType(type, projection.ReturnType),
                    //    projection.Expression,
                    //    projection.Parameter
                    //);

                    //var queryType = typeof(IQueryable<>)
                    //    .MakeGenericType(type);

                    //parameter = Expression.Parameter(queryType);
                    //expression = parameter;

                    //var selectExpression = Expression.Call
                    //(
                    //    methodInfo.MakeGenericMethod(type),
                    //    expression,
                    //    lambda
                    //);

                    //var fieldValue = new FieldExpression(selectExpression, parameter);

                    var fieldValue = Boost5
                    (
                        type,
                        methodInfo,
                        expression,
                        projection,
                        parameter
                    );

                    fields.Add(property.Name, fieldValue);
                }
            }
        }

        protected virtual IFieldExpression AsQueryable
        (
            TypeCreator typeCreator,
            Type queryType,
            Type propertyType,
            ParameterExpression parameter,
            IProjectionRequest projectionRequest,
            MethodInfo methodInfo
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

            var method = default(MethodInfo);

            if (methodInfo.GetGenericArguments().Length == 1)
            {
                method = methodInfo
                    .MakeGenericMethod(propertyType);
            }
            else
            {
                method = methodInfo
                    .MakeGenericMethod(propertyType, projection.ReturnType);
            }

            //var method = methodInfo
            //    .MakeGenericMethod(propertyType, projection.ReturnType);

            var selectExpression = Expression.Call
            (
                method,
                expression,
                Expression.Quote(projection.GetLambdaExpression())
            );

            var fieldValue = new FieldExpression(selectExpression, parameter);
            return fieldValue;
        }
    }
}
