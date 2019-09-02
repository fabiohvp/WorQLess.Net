using Enflow;
using System;
using System.Linq.Expressions;
using WorQLess.Attributes;

namespace WorQLess.Rules
{

    [Expose]
    public class LessThanOrEqualRule<T> : IProjection<T, bool>
        , IWorQLessDynamic
        , IWorQLessRuleBooster
    {
        public virtual IFieldExpression FieldExpression { get; set; }
        public virtual object Value { get; set; }

        public virtual Expression<Func<T, bool>> Predicate
        {
            get
            {
                return FieldExpression.GetLambdaExpression<T>(Expression.LessThanOrEqual, Value);
            }
        }
    }
}
