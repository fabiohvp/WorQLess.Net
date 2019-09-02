using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Enflow;
using WorQLess.Extensions;
using Newtonsoft.Json.Linq;

namespace WorQLess.Boosters
{
	public class ProjectAsBooster : IBooster
	{
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
			var lastKey = fields
				.Keys
				.Last();
			var lastExpression = fields[lastKey];

			var projection = Reflection
				.CreateProjection(sourceType, property.Value.ToString(), new Type[] { lastExpression.Type }, null);

			var predicate = projection
				.GetType()
				.GetProperty(nameof(IProjection<object, object>.Predicate))
				.GetValue(projection);

			var _expression = System
				.Linq
				.Expressions
				.Expression
				.Invoke((Expression)predicate, lastExpression.GetLambdaExpression());

			fields.Remove(lastKey);
			fields.Add
			(
				lastKey,
				new FieldExpression(_expression, initialParameter)
				{
					Interfaces = lastExpression.Interfaces
				}
			);
		}
	}
}