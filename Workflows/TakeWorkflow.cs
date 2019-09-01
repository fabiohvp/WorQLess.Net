using System.Collections.Generic;
using System.Linq;
using Enflow;
using WorQLess.Net.Attributes;

namespace WorQLess.Net.Workflows
{
	[Expose]
	public class TakeWorkflow<T> : Workflow<IEnumerable<T>>
		, IWorQLessWorkflowContainer
	{
		public IWorkflowContainer WorkflowContainer { get; set; }

		public int Amount { get; set; }

		protected override IEnumerable<T> ExecuteWorkflow(IEnumerable<T> candidate)
		{
			var query = WorkflowContainer
				.ApplyRules(candidate.AsQueryable());

			var retorno = WorkflowContainer
				.ApplyProjection<T, T>(query)
				.Take(Amount);

			if (WorkflowContainer.Evaluate)
			{
				return retorno
					.ToList();
			}

			return retorno;
		}
	}
}