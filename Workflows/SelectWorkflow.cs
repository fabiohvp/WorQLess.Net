using Enflow;
using LinqKit;
using System;
using System.Linq;
using System.Linq.Expressions;
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

            var t = typeof(T);
            var u = typeof(U);

            var retorno = ((Expression<Func<IQueryable<T>, U>>)
                ((IWorQLessProjection)WorkflowContainer.Projection)
                .FieldExpression
                .Expression)
                .Invoke(query);

            return retorno;
        }
    }
}