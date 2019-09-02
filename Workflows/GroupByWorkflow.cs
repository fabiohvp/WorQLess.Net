using Enflow;
using System;
using System.Collections.Generic;
using System.Linq;
using WorQLess.Attributes;

namespace WorQLess.Workflows
{
    [Expose]
    public class GroupByWorkflow<T, U> : Workflow<IEnumerable<T>, IEnumerable<IGrouping<U, T>>>
        , IWorQLessWorkflowContainer
    {
        public IWorkflowContainer WorkflowContainer { get; set; }

        protected override IEnumerable<IGrouping<U, T>> ExecuteWorkflow(IEnumerable<T> candidate)
        {
            var query = WorkflowContainer
                .ApplyRules(candidate.AsQueryable());

            var groupByExpression = WorkflowContainer.Projection.FieldExpression;
            var groupByLambda = groupByExpression.GetLambdaExpression<T, U>();

            var retorno = query
                .GroupBy(groupByLambda);

            if (WorkflowContainer.Evaluate)
            {
                throw new InvalidOperationException("GroupBy workflow cannot be evaluated directly");
            }

            return retorno;
        }
    }
}