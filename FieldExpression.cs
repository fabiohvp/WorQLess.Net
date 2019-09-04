using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace WorQLess
{
    public interface IFieldExpression
    {
        ParameterExpression InitialParameter { get; set; }
        Expression Expression { get; set; }
        IList<Type> Interfaces { get; set; }
        Type ReturnType { get; set; }

        LambdaExpression GetLambdaExpression();
        Expression<Func<T, U>> GetLambdaExpression<T, U>();
        Expression<Func<T, bool>> GetLambdaExpression<T>(Func<Expression, Expression, BinaryExpression> operandMethod, object value);
    }

    public class FieldExpression : IFieldExpression
    {
        public ParameterExpression InitialParameter { get; set; }
        public Expression Expression { get; set; }
        public IList<Type> Interfaces { get; set; }
        public Type ReturnType { get; set; }

        public FieldExpression(Expression expression, ParameterExpression initialParameter)
            : this(expression, initialParameter, expression.Type)
        {
        }

        public FieldExpression(Expression expression, ParameterExpression initialParameter, Type type)
        {
            Expression = expression;
            InitialParameter = initialParameter;
            ReturnType = type;
            Interfaces = new List<Type>();
        }

        public LambdaExpression GetLambdaExpression()
        {
            var lambda = Expression.Lambda(Expression, InitialParameter);
            return lambda;
        }
        
        public Expression<Func<T, U>> GetLambdaExpression<T, U>()
        {
            var lambda = Expression.Lambda<Func<T, U>>(Expression, InitialParameter);
            return lambda;
        }

        public Expression<Func<T, bool>> GetLambdaExpression<T>(Func<Expression, Expression, BinaryExpression> operandMethod, object value)
        {
            var type = Nullable.GetUnderlyingType(Expression.Type) ?? Expression.Type;
            var constant = Expression.Constant(Convert.ChangeType(value, type), Expression.Type);
            var operand = operandMethod(Expression, constant);
            var lambda = Expression.Lambda<Func<T, bool>>(operand, InitialParameter);
            return lambda;
        }
    }
}