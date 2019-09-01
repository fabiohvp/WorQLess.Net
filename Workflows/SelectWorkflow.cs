using System.Collections.Generic;
using System.Linq;
using WorQLess.Net.Attributes;
using Enflow;

namespace WorQLess.Net.Workflows
{
	[Expose]
	public class SelectWorkflow<T, U> : Workflow<IEnumerable<T>, IEnumerable<U>>
		, IWorQLessWorkflowContainer
	{
		public IWorkflowContainer WorkflowContainer { get; set; }

		protected override IEnumerable<U> ExecuteWorkflow(IEnumerable<T> candidate)
		{
			var query = WorkflowContainer
				.ApplyRules(candidate.AsQueryable());

			var retorno = WorkflowContainer
				.ApplyProjection<T, U>(query);

			if (WorkflowContainer.Evaluate)
			{
				return retorno
					.ToList();
			}

			return retorno;
		}
	}
}