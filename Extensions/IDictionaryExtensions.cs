using System.Linq;
using System.Linq.Expressions;
using WorQLess.Models;

namespace System.Collections.Generic
{
    public static class IDictionaryExtensions
    {
        public static void AddRange<TKey, TValue>(this IDictionary<TKey, TValue> dic1, IDictionary<TKey, TValue> dic2)
        {
            foreach (var item in dic2)
            {
                dic1.Add(item.Key, item.Value);
            }
        }

        private class ReplaceVisitor : ExpressionVisitor
        {
            private readonly Expression from, to;
            public ReplaceVisitor(Expression from, Expression to)
            {
                this.from = from;
                this.to = to;
            }

            public override Expression Visit(Expression ex)
            {
                if (ex == from)
                {
                    return to;
                }

                return base.Visit(ex);
            }
        }

        public static LambdaExpression Compose(this LambdaExpression first, LambdaExpression second)
        {
            var firstCall = second.Body.Replace(second.Parameters[0], first.Body);
            return Expression.Lambda(firstCall, first.Parameters[0]);
        }


        public static Expression Replace(this Expression ex, Expression from, Expression to)
        {
            return new ReplaceVisitor(from, to).Visit(ex);
        }

        public static IFieldExpression Compose(this IDictionary<string, IFieldExpression> projections)
        {
            var initialParameter = projections.First().Value.Parameter;
            var expressions = projections
                .Select(o => o.Value.GetLambdaExpression());

            var lambda = expressions.Aggregate((a, b) => a.Compose(b));
            return new FieldExpression(lambda, initialParameter, lambda.ReturnType);
        }
    }
}
