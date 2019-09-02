using Enflow;
using System.Collections.Generic;
using System.Linq;
using WorQLess.Net.Attributes;

namespace WorQLess.Net.Workflows
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

            var x = WorkflowContainer.Projection.FieldExpression;
            var z = x.GetLambdaExpression<T, U>();

            // var retorno = Workflow
            // 	.ApplyProjection<T, U>(query);

            var a = query
                .GroupBy(z);

            // .Select(o => new GroupByModel<T, U>
            // {
            // 	Data = o
            // });

            if (WorkflowContainer.Evaluate)
            {
                return a
                    .Take(WQL.Limit)
                    .ToList();
            }

            return a;
        }
    }

    // public class GroupByModel<T, U>
    // {
    // 	public IGrouping<U, T> Data { get; set; }
    // }
}