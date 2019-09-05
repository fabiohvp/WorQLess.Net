using Enflow;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Linq.Expressions;
using WorQLess.Boosters;
using WorQLess.Extensions;
using WorQLess.Models;
using WorQLess.Requests;

namespace WorQLess.Projections
{
    public static class ProjectionFactory
    {
        public static IWorQLessProjection Create(Type sourceType, ProjectionRequest projectionRequest)
        {
            var fieldExpression = default(IFieldExpression);
            var projection = default(IWorQLessProjection);
            var returnType = sourceType;

            if (!string.IsNullOrEmpty(projectionRequest?.Name))
            {
                var projectionType = projectionRequest.Type;

                if (typeof(IWorQLessDynamicProjection).IsAssignableFrom(projectionType))
                {
                    var queryType = typeof(IQueryable<>).MakeGenericType(sourceType);
                    var parameter = Expression.Parameter(queryType);
                    Expression body = parameter;
                    var args = (JArray)projectionRequest.Args;
                    var firstArg = new JArray(args.First());

                    var booster = new SelectBooster();
                    var selectProjection = booster.Select(WQL.TypeCreator, sourceType, firstArg, body, parameter);
                    var otherArgs = new JArray(args.Skip(1));
                    var otherProjections = WQL.TypeCreator.BuildExpression(selectProjection.Expression.Type, otherArgs, false);

                    returnType = otherProjections.ReturnType;
                    fieldExpression = selectProjection.Combine(otherProjections, parameter);
                    returnType = fieldExpression.ReturnType;
                }
                else if (typeof(IWorQLessProjection).IsAssignableFrom(projectionType))
                {
                    fieldExpression = WQL.TypeCreator.BuildExpression(sourceType, (JArray)projectionRequest.Args);
                    returnType = fieldExpression.ReturnType;
                }
                else
                {
                    var projectionInterface = projectionType
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

                projection = (IWorQLessProjection)Reflection
                    .CreateProjection(sourceType, projectionRequest.Name, new Type[] { sourceType, returnType }, projectionRequest.Args);
                projection.FieldExpression = fieldExpression;
            }

            return projection;
        }
    }
}

