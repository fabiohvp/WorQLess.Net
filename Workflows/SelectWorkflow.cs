using Enflow;
using LinqKit;
using Newtonsoft.Json;
using System.Linq;
using WorQLess.Attributes;

namespace WorQLess.Workflows
{
    [Expose]
    public class SelectWorkflow<T, U> : Workflow<IQueryable<T>, U>
        , IWorQLessWorkflowContainer
    {
        public IWorkflowContainer WorkflowContainer { get; set; }

        protected override U ExecuteWorkflow(IQueryable<T> candidate)
        {
            //var _query = candidate.AsQueryable();
            //var fields = new Dictionary<string, IFieldExpression>();
            //var booster = new SelectBooster();
            //var type = _query.GetType();
            //var initialParameter = Expression.Parameter(type);
            //var jProperty = new JProperty("$select", ((IRawArguments)WorkflowContainer.Projection).Arguments);
            //var expression = Expression.Empty();
            //booster.Boost(WQL.TypeCreator, type, type, fields, jProperty, expression, initialParameter);

            var query = WorkflowContainer
                .ApplyRules(candidate);

            var retorno = WorkflowContainer.Projection.FieldExpression.GetLambdaExpression<IQueryable<T>, U>()
                .Invoke(query);

            return retorno;
            //var retorno = WorkflowContainer
            //    .ApplyProjection<T, U>(query);

            //return WorkflowContainer
            //    .ApplyEvaluate(retorno).AsQueryable();
        }
    }
}