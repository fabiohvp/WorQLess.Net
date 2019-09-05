using Enflow;
using System;
using System.Linq.Expressions;
using WorQLess.Attributes;
using WorQLess.Models;

namespace WorQLess.Rules
{

    [Expose]
    public class LessThanRule<T> : IProjection<T, bool>
        , IWorQLessProjection
        , IWorQLessRuleBooster
    {
        public virtual IFieldExpression FieldExpression { get; set; }
        public virtual object Value { get; set; }

        public virtual Expression<Func<T, bool>> Predicate
        {
            get
            {
                return FieldExpression.GetLambdaExpression<T>(Expression.LessThan, Value);
            }
        }
    }
}
