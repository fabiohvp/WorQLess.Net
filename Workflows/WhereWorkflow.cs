using Enflow;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using WorQLess.Attributes;
using WorQLess.Boosters;

namespace WorQLess.Workflows
{
    [Expose]
    public class WhereWorkflow<T, U> : Workflow<IEnumerable<T>, IEnumerable<U>>
        , IWorQLessWorkflowContainer
        , IRawArguments
    {
        public IWorkflowContainer WorkflowContainer { get; set; }
        public JArray Arguments { get; set; }

        protected override IEnumerable<U> ExecuteWorkflow(IEnumerable<T> candidate)
        {
            var initialParameter = Expression.Parameter(typeof(T));
            var where = new WhereBooster()
                .BuildExpression(WQL.TypeCreator, Arguments, typeof(T), initialParameter);

            var query = WorkflowContainer
                .ApplyRules(candidate.AsQueryable())
                .Where((Expression<Func<T, bool>>)where.Expression);

            var retorno = WorkflowContainer
                .ApplyProjection<T, U>(query);

            if (WorkflowContainer.Evaluate)
            {
                return retorno
                    .Take(WQL.Limit)
                    .ToList();
            }

            return retorno;
        }
    }
}