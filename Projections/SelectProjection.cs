using Enflow;
using Newtonsoft.Json.Linq;
using System;
using System.Linq.Expressions;
using WorQLess.Attributes;
using WorQLess.Models;

namespace WorQLess.Workflows
{
    [Expose]
    public class SelectProjection<T, U> : IProjection<T, U>
        , IWorQLessDynamicProjection
        , IRawArguments
    {
        public virtual IFieldExpression FieldExpression { get; set; }
        public virtual JArray Arguments { get; set; }

        public virtual Expression<Func<T, U>> Predicate => FieldExpression.GetLambdaExpression<T, U>();
    }
}