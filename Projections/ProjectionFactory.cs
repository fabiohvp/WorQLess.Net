using Enflow;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using WorQLess.Boosters;
using WorQLess.Extensions;
using WorQLess.Requests;

namespace WorQLess.Net.Projections
{
    public static class ProjectionFactory
    {
        private static Type GetQueryType<T>()
        {
            return typeof(IQueryable<T>);
        }

        public static IWorQLessDynamic Create(Type sourceType, ProjectionRequest projectionRequest)
        {
            var projection = default(IWorQLessDynamic);
            var returnType = sourceType;
            var fieldExpression = default(IFieldExpression);

            if (!string.IsNullOrEmpty(projectionRequest?.Name))
            {
                if (typeof(IWorQLessDynamic2).IsAssignableFrom(projectionRequest.Type))
                {
                    var fields = new Dictionary<string, IFieldExpression>();
                    var booster = new SelectBooster();

                    var queryType = (Type)typeof(ProjectionFactory)
                        .GetMethod(nameof(GetQueryType), BindingFlags.NonPublic | BindingFlags.Static)
                        .MakeGenericMethod(sourceType)
                        .Invoke(null, null);

                    var initialParameter = Expression.Parameter(queryType);
                    Expression body = initialParameter;
                    var jArray = (JArray)projectionRequest.Args;
                    var jArray2 = new JArray(jArray.First());

                    var x = booster.Select(WQL.TypeCreator, sourceType, jArray2, body, initialParameter);
                    var z = new JArray(jArray.Skip(1));
                    var fieldExpression1 = WQL.TypeCreator.BuildExpression(x.Expression.Type, z, false);

                    returnType = fieldExpression1.ReturnType;
                    fieldExpression = Chain(x, fieldExpression1, initialParameter);
                    returnType = fieldExpression.ReturnType;
                }
                else if (typeof(IWorQLessDynamic).IsAssignableFrom(projectionRequest.Type))
                {
                    fieldExpression = WQL.TypeCreator.BuildExpression(sourceType, (JArray)projectionRequest.Args);
                    returnType = fieldExpression.ReturnType;
                }
                else
                {
                    var projectionInterface = projectionRequest.Type
                        .GetInterfaces()
                        .FirstOrDefault(o =>
                            o.IsGenericType
                            && o.GetGenericTypeDefinition() == typeof(IProjection<,>)
                        );

                    if (projectionInterface != null)
                    {
                        returnType = projectionInterface.GenericTypeArguments.LastOrDefault();
                    }
                }

                projection = (IWorQLessDynamic)Reflection
                    .CreateProjection(sourceType, projectionRequest.Name, new Type[] { sourceType, returnType }, projectionRequest.Args);
                projection.FieldExpression = fieldExpression;
            }

            return projection;
        }

        private class SwapVisitor : ExpressionVisitor
        {
            private readonly Expression _source, _replacement;

            public SwapVisitor(Expression source, Expression replacement)
            {
                _source = source;
                _replacement = replacement;
            }

            public override Expression Visit(Expression node)
            {
                return node == _source ? _replacement : base.Visit(node);
            }
        }

        private static IFieldExpression Chain(IFieldExpression _inner, IFieldExpression _outer, ParameterExpression initialParameter)
        {
            var inner = _inner.GetLambdaExpression();
            var outer = _outer.GetLambdaExpression();
            var visitor = new SwapVisitor(outer.Parameters[0], inner.Body);
            var expression = visitor.Visit(outer.Body);
            return new FieldExpression(expression, initialParameter);
        }
    }
}
