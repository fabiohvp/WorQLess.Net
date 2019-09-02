using System.Collections.Generic;

namespace WorQLess.Requests
{
	public interface IRuleRequest : IRequest
	{
		bool Negate { get; set; }
		RuleOperand Operand { get; set; }
		IEnumerable<IRuleRequest> Rules { get; set; }
	}

	public class RuleRequest : IRuleRequest
	{
		public virtual string Name { get; set; }
		public virtual object Args { get; set; }
		public virtual bool Negate { get; set; }
		public virtual RuleOperand Operand { get; set; }
		public virtual IEnumerable<IRuleRequest> Rules { get; set; }

		public RuleRequest()
		{
			Negate = false;
			Operand = RuleOperand.None;
			Rules = new HashSet<RuleRequest>();
		}
	}
}