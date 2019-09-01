using Enflow;
using System;
using System.Linq.Expressions;
using WorQLess.Net.Attributes;

namespace WorQLess.Net.Rules
{

    [Expose]
    public class GreaterThanRule<T> : IProjection<T, bool>
        , IWorQLessDynamic
        , IWorQLessRuleBooster
    {
        public virtual IFieldExpression FieldExpression { get; set; }
        public virtual object Value { get; set; }

        public virtual Expression<Func<T, bool>> Predicate
        {
            get
            {
                return FieldExpression.GetLambdaExpression<T>(Expression.GreaterThan, Value);
            }
        }
    }
}
