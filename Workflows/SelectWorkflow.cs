using Enflow;
using LinqKit;
using System.Linq;
using WorQLess.Attributes;
using WorQLess.Models;

namespace WorQLess.Workflows
{
    [Expose]
    public class SelectWorkflow<T, U> : Workflow<IQueryable<T>, U>
        , IWorQLessWorkflow
    {
        public virtual IWorkflowContainer WorkflowContainer { get; set; }

        protected override U ExecuteWorkflow(IQueryable<T> candidate)
        {
            var query = WorkflowContainer
                .ApplyRules(candidate);

            var retorno = ((IWorQLessProjection)WorkflowContainer.Projection)
                .FieldExpression
                .GetLambdaExpression<IQueryable<T>, U>()
                .Invoke(query);

            return retorno;
        }
    }
}