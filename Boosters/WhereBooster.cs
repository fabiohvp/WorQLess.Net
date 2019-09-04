using Enflow;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using WorQLess.Extensions;

namespace WorQLess.Boosters
{
    public class WhereBooster : IBooster
    {
        private static readonly MethodInfo WhereMethod;
        private static readonly MethodInfo ApplyOperandMethod;

        static WhereBooster()
        {
            var enumerableMethods = typeof(Queryable).GetMethods();

            WhereMethod = enumerableMethods
                .First(o =>
                    o.Name == nameof(Queryable.Where)
                    && o.GetParameters()[1].ParameterType.GetGenericArguments()[0].GetGenericArguments().Length == 2
                );

            ApplyOperandMethod = typeof(WhereBooster)
                .GetMethod(nameof(ApplyOperand), BindingFlags.NonPublic | BindingFlags.Static);
        }

        public IFieldExpression BuildExpression(TypeCreator typeCreator, JArray jArray, Type returnType, ParameterExpression initialParameter)
        {
            var expression = GetRuleContainer(typeCreator, (JObject)jArray.First(), returnType);

            foreach (JObject jObject in jArray.Skip(1))
            {
                var props = jObject.Properties()
                    .ToDictionary(o => o.Name, o => o.Value);
                var __expression = GetRuleContainer(typeCreator, props, returnType);

                expression = (Expression)ApplyOperandMethod
                    .MakeGenericMethod(returnType)
                    .Invoke(null, new object[] { props, expression, __expression });
            }

            return new FieldExpression(expression, initialParameter);
        }

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
            if (fields.Any())
            {
                var lastField = fields.Last();
                var returnType = lastField.Value.Type.GetGenericArguments().LastOrDefault();
                var _expression = BuildExpression(typeCreator, (JArray)property.Value, returnType, initialParameter)
                    .Expression;
                var method = WhereMethod
                    .MakeGenericMethod(returnType);
                var whereExpression = Expression.Call
                (
                    method,
                    lastField.Value.Expression,
                    _expression
                );

                fields.Remove(lastField.Key);
                var fieldValue = new FieldExpression(whereExpression, initialParameter);
                fields.Add(property.Name, fieldValue);
            }
            else
            {
                var returnType = expression.Type.GetGenericArguments().LastOrDefault();
                var _expression = BuildExpression(typeCreator, (JArray)property.Value, returnType, initialParameter)
                    .Expression;
                var method = WhereMethod
                    .MakeGenericMethod(returnType);
                var whereExpression = Expression.Call
                (
                    method,
                    expression,
                    _expression
                );

                var fieldValue = new FieldExpression(whereExpression, initialParameter);
                fields.Add(property.Name, fieldValue);
            }
        }

        private static Expression GetRuleContainer(TypeCreator typeCreator, JObject jObject, Type returnType)
        {
            var props = jObject.Properties()
                .ToDictionary(o => o.Name, o => o.Value);
            return GetRuleContainer(typeCreator, props, returnType);
        }

        private static Expression GetRuleContainer(TypeCreator typeCreator, Dictionary<string, JToken> props, Type returnType)
        {
            var _fields = new Dictionary<string, IFieldExpression>();
            var fieldExpression = typeCreator.BuildExpression(returnType, (JArray)props["args"], false);
            var rules = (JArray)props["rules"];
            var _expression = GetRule(typeCreator, (JObject)rules.First(), returnType, fieldExpression);

            foreach (JObject rule in rules.Skip(1))
            {
                var _props = rule.Properties()
                    .ToDictionary(o => o.Name, o => o.Value);
                var __expression = GetRule(typeCreator, _props, returnType, fieldExpression);

                _expression = (Expression)ApplyOperandMethod
                    .MakeGenericMethod(returnType)
                    .Invoke(null, new object[] { props, _expression, __expression });
            }


            return _expression;
        }

        private static Expression GetRule(TypeCreator typeCreator, JObject jObject, Type returnType, IFieldExpression fieldExpression)
        {
            var props = jObject.Properties()
                .ToDictionary(o => o.Name, o => o.Value);
            return GetRule(typeCreator, props, returnType, fieldExpression);
        }

        private static Expression GetRule(TypeCreator typeCreator, Dictionary<string, JToken> props, Type returnType, IFieldExpression fieldExpression)
        {
            var rule = (IWorQLessRuleBooster)Reflection.CreateRule(returnType, props["name"].ToObject<string>(), new Type[] { returnType }, null);
            rule.FieldExpression = fieldExpression;
            rule.Value = props["value"].ToObject<object>();

            return (Expression)rule
                .GetType()
                .GetProperty(nameof(IProjection<object, object>.Predicate))
                .GetValue(rule);
        }

        private static Expression<Func<T, bool>> ApplyOperand<T>(Dictionary<string, JToken> props, Expression<Func<T, bool>> expression1, Expression<Func<T, bool>> expression2)
        {
            var operand = 1;

            if (props.ContainsKey("operand"))
            {
                operand = (int)props["operand"];
            }

            if (operand == 2)
            {
                return LinqKit.PredicateBuilder.Or(expression1, expression2);
            }
            else if (operand == 3)
            {
                return LinqKit.PredicateBuilder.And(expression1, expression2);
            }
            else
            {
                return LinqKit.PredicateBuilder.And(expression1, expression2);
            }
        }
    }
}