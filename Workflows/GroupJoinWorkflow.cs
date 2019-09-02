using Enflow;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using WorQLess.Attributes;

namespace WorQLess.Workflows
{
    [Expose]
    public class GroupJoinWorkflow<T, U> : Workflow<IEnumerable<T>, IEnumerable<GroupJoinModel>>
        , IWorQLessWorkflowContainer
        , IWorQLessWorkflowJoin
        , IRawArguments
    {
        public IWorkflowContainer WorkflowContainer { get; set; }
        public IQueryable PreviousWorkflowContainerResult { get; set; }
        public JArray Arguments { get; set; }

        protected override IEnumerable<GroupJoinModel> ExecuteWorkflow(IEnumerable<T> candidate)
        {
            var query = WorkflowContainer
                .ApplyRules(candidate.AsQueryable());

            var groupByExpression = WQL.TypeCreator.BuildExpression
            (
                PreviousWorkflowContainerResult.ElementType,
                Arguments
            );
            var groupByLambda = groupByExpression.GetLambdaExpression<dynamic, U>();

            var retorno = query
                .Join
                (
                    (IQueryable<dynamic>)PreviousWorkflowContainerResult,
                    WorkflowContainer.Projection.FieldExpression.GetLambdaExpression<T, U>(),
                    groupByLambda,
                    (l, r) => new GroupJoinModel
                    {
                        Left = l,
                        Right = r
                    }
                );

            if (WorkflowContainer.Evaluate)
            {
                throw new InvalidOperationException("GroupBy workflow cannot be evaluated directly");
            }

            return retorno;
        }
    }

    public class GroupJoinModel
    {
        public object Left { get; set; }
        public object Right { get; set; }
    }
}