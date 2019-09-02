using Enflow;
using System.Collections.Generic;
using System.Linq;
using WorQLess.Attributes;

namespace WorQLess.Workflows
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

            return WorkflowContainer
                .ApplyEvaluate(retorno);
        }
    }
}