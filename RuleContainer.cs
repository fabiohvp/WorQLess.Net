using System.Collections.Generic;
using System.Linq;
using Enflow;
using WorQLess.Net.Extensions;
using WorQLess.Net.Requests;
using System;

namespace WorQLess.Net
{
	public interface IRuleContainer
	{
		RuleOperand Operand { get; set; }
		bool Negate { get; set; }
		IRuleRequest Request { get; set; }
		IEnumerable<IRuleContainer> Rules { get; set; }

		IStateRule<T> GetStateRule<T>();
		IStateRule<T> ApplyOperands<T>(IStateRule<T> stateRuleA, IStateRule<T> stateRuleB);
	}

	public class RuleContainer : IRuleContainer
	{
		public virtual IRuleRequest Request { get; set; }
		public virtual RuleOperand Operand { get; set; }
		public virtual bool Negate { get; set; }
		public virtual IEnumerable<IRuleContainer> Rules { get; set; }

		public RuleContainer(IRuleRequest request)
		{
			var rules = new List<IRuleContainer>();

			foreach (var _rule in request.Rules)
			{
				rules.Add(new RuleContainer(_rule));
			}

			Operand = request.Operand;
			Negate = request.Negate;
			Request = request;
			Rules = rules;
		}

		public IStateRule<T> GetStateRule<T>()
		{
			var stateRule = (IStateRule<T>)Reflection
				.CreateRule(typeof(T), Request.Name, new Type[] { typeof(T) }, Request.Args);

			if (Rules.Any())
			{
				var innerRules = Rules.GetStateRule<T>();
				stateRule = ApplyOperands(stateRule, innerRules);
			}

			return stateRule;
		}

		public IStateRule<T> ApplyOperands<T>(IStateRule<T> stateRuleA, IStateRule<T> stateRuleB)
		{
			if (Operand == RuleOperand.Or)
			{
				stateRuleA = stateRuleA.Or(stateRuleB);
			}
			else
			{
				stateRuleA = stateRuleA.And(stateRuleB);
			}

			if (Negate)
			{
				stateRuleA = stateRuleA.Not();
			}

			return stateRuleA;
		}
	}

	public static class Rulextensions
	{
		public static IStateRule<T> GetStateRule<T>(this IEnumerable<IRuleContainer> rules)
		{
			var firstRule = rules.FirstOrDefault();
			var rulesExceptFirst = rules.Skip(1);
			var stateRule = firstRule.GetStateRule<T>();

			foreach (var rule in rulesExceptFirst)
			{
				var _stateRule = rule.GetStateRule<T>();
				stateRule = rule.ApplyOperands(stateRule, _stateRule);
			}

			return stateRule;
		}
	}
}