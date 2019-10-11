using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using WorQLess.Extensions;
using WorQLess.Models;

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

        public abstract void Boost
        (
            TypeCreator typeCreator,
            Expression body,
            JProperty property,
            IDictionary<string, IFieldExpression> fields
        );

        protected Expression GetParameter(IDictionary<string, IFieldExpression> fields, Expression currentParameter)
        {
            var lastField = fields.Values.LastOrDefault();

            if (lastField == null)
            {
                return currentParameter;
            }

            return Expression.Parameter(lastField.ReturnType.GetGenericArguments().Single()); //next method must use the last returned type
        }


        protected IFieldExpression CallMethod
        (
            TypeCreator typeCreator,
            Expression parameter,
            JArray jArray,
            MethodInfo methodInfo,
            bool createAnonymousProjection
        )
        {
            var projection = typeCreator.BuildProjection(parameter, jArray, createAnonymousProjection);
            var lambda = projection.GetLambdaExpression();

            return CallMethod
            (
                typeCreator,
                parameter,
                lambda,
                methodInfo
            );
        }

        protected IFieldExpression CallMethodOnGroup
        (
            TypeCreator typeCreator,
            Expression parameter,
            LambdaExpression lambda,
            MethodInfo methodInfo
        )
        {
            var queryType = typeof(IQueryable<>).MakeGenericType(parameter.Type);
            var queryParameter = Expression.Parameter(queryType);
            Expression expression = queryParameter;

            var method = methodInfo
                .MakeGenericMethod(parameter.Type);

            expression = Expression.Call
            (
                method,
                expression,
                Expression.Quote(lambda)
            );

            var fieldValue = new FieldExpression(expression, parameter);
            return fieldValue;
        }


        protected IFieldExpression CallMethod
        (
            TypeCreator typeCreator,
            Expression parameter,
            LambdaExpression lambda,
            MethodInfo methodInfo
        )
        {
            var queryType = typeof(IQueryable<>).MakeGenericType(parameter.Type);
            var queryParameter = Expression.Parameter(queryType);
            Expression expression = queryParameter;

            //if (expression.Type.GetInterfaces().Any(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IEnumerable<>)))
            //{
            //    var asQueryableMethod = AsQueryableMethod
            //        .MakeGenericMethod(parameter.Type);

            //    expression = Expression.Call
            //    (
            //        asQueryableMethod,
            //        expression
            //    );
            //}

            var method = default(MethodInfo);

            if (methodInfo.GetGenericArguments().Count() == 1)
            {
                method = methodInfo
                    .MakeGenericMethod(parameter.Type);
            }
            else
            {
                method = methodInfo
                    .MakeGenericMethod(parameter.Type, lambda.Body.Type);
            }

            expression = Expression.Call
            (
                method,
                expression,
                Expression.Quote(lambda)
            );

            var fieldValue = new FieldExpression(expression, queryParameter);
            return fieldValue;
        }

        protected IFieldExpression CallMethod
        (
            TypeCreator typeCreator,
            Expression parameter,
            ConstantExpression value,
            MethodInfo methodInfo
        )
        {
            var queryType = typeof(IQueryable<>).MakeGenericType(parameter.Type);
            var queryParameter = Expression.Parameter(queryType);
            Expression expression = queryParameter;

            if (expression.Type.GetInterfaces().Any(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IEnumerable<>)))
            {
                var asQueryableMethod = AsQueryableMethod
                    .MakeGenericMethod(parameter.Type);

                expression = Expression.Call
                (
                    asQueryableMethod,
                    expression
                );
            }

            var method = methodInfo
                .MakeGenericMethod(parameter.Type);

            expression = Expression.Call
            (
                method,
                expression,
                value
            );

            var fieldValue = new FieldExpression(expression, queryParameter);
            return fieldValue;
        }

        protected Expression AsQueryable(Expression expression)
        {
            var asQueryableMethod = AsQueryableMethod
                .MakeGenericMethod(expression.Type.GetGenericArguments().LastOrDefault());

            expression = Expression.Call
            (
                asQueryableMethod,
                expression
            );

            return expression;
        }
    }
}
