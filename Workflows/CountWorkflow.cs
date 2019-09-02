using Enflow;
using System.Collections.Generic;
using System.Linq;
using WorQLess.Attributes;

namespace WorQLess.Workflows
{
    [Expose]
    public class CountWorkflow<T> : Workflow<IEnumerable<T>, int>
        , IWorQLessWorkflowContainer
    {
        public IWorkflowContainer WorkflowContainer { get; set; }

        protected override int ExecuteWorkflow(IEnumerable<T> candidate)
        {
            var query = WorkflowContainer
                .ApplyRules(candidate.AsQueryable());

            var retorno = WorkflowContainer
                .ApplyProjection<T, T>(query)
                .Count();

            return retorno;
        }
    }
}