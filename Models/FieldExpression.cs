using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace WorQLess.Models
{
    public interface IFieldExpression
    {
        ParameterExpression Parameter { get; set; }
        Expression Expression { get; set; }
        IList<Type> Interfaces { get; set; }
        Type ReturnType { get; set; }

        LambdaExpression GetLambdaExpression();
        Expression<Func<T, U>> GetLambdaExpression<T, U>();
        Expression<Func<T, bool>> GetLambdaExpression<T>(Func<Expression, Expression, BinaryExpression> operandMethod, object value);
        //IFieldExpression Combine(IFieldExpression other, ParameterExpression parameter);
    }

    public class FieldExpression : IFieldExpression
    {
        public ParameterExpression Parameter { get; set; }
        public Expression Expression { get; set; }
        public IList<Type> Interfaces { get; set; }
        public Type ReturnType { get; set; }

        public FieldExpression(Expression expression, Expression parameter)
            : this(expression, parameter, expression.Type)
        {
        }

        public FieldExpression(Expression expression, Expression parameter, Type type)
        {
            Expression = expression;
            Parameter = parameter as ParameterExpression;
            ReturnType = type;
            Interfaces = new List<Type>();
        }

        public LambdaExpression GetLambdaExpression()
        {
            var lambda = Expression.Lambda(Expression, Parameter);
            return lambda;
        }

        public Expression<Func<T, U>> GetLambdaExpression<T, U>()
        {
            var lambda = Expression.Lambda<Func<T, U>>(Expression.Quote(Expression), Parameter);
            return lambda;
        }

        public Expression<Func<T, bool>> GetLambdaExpression<T>(Func<Expression, Expression, BinaryExpression> operandMethod, object value)
        {
            var type = Nullable.GetUnderlyingType(Expression.Type) ?? Expression.Type;
            var constant = Expression.Constant(Convert.ChangeType(value, type), Expression.Type);
            var operand = operandMethod(Expression, constant);
            var lambda = Expression.Lambda<Func<T, bool>>(operand, Parameter);
            return lambda;
        }


        //private class SwapVisitor : ExpressionVisitor
        //{
        //    private readonly Expression _source, _replacement;

        //    public SwapVisitor(Expression source, Expression replacement)
        //    {
        //        _source = source;
        //        _replacement = replacement;
        //    }

        //    public override Expression Visit(Expression node)
        //    {
        //        return node == _source ? _replacement : base.Visit(node);
        //    }
        //}

        //public IFieldExpression Combine(IFieldExpression other, ParameterExpression parameter)
        //{
        //    var outer = GetLambdaExpression();
        //    var inner = other.GetLambdaExpression();
        //    //var inner = GetLambdaExpression();
        //    //var outer = other.GetLambdaExpression();
        //    var visitor = new SwapVisitor(outer.Parameters[0], inner.Body);
        //    var expression = visitor.Visit(outer.Body);
        //    return new FieldExpression(expression, parameter);
        //}
    }
}