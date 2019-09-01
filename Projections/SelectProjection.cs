using System;
using System.Linq.Expressions;
using WorQLess.Net.Attributes;
using Enflow;
using Newtonsoft.Json.Linq;

namespace WorQLess.Net.Workflows
{
	[Expose]
	public class SelectProjection<T, U> : IProjection<T, U>
		, IWorQLessDynamic
		, IRawArguments
	{
		public virtual IFieldExpression FieldExpression { get; set; }
		public virtual JArray Arguments { get; set; }

		public virtual Expression<Func<T, U>> Predicate => FieldExpression.GetLambdaExpression<T, U>();
	}
}