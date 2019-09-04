using System.Linq;

namespace WorQLess
{
    public interface IWorQLessWorkflowContainer
    {
        IWorkflowContainer WorkflowContainer { get; set; }
    }

    public interface IWorQLessProjection
    {
        IWorQLessDynamic Projection { get; set; }
    }

    public interface IWorQLessWorkflowJoin
    {
        IQueryable PreviousWorkflowContainerResult { get; set; }
    }

    public interface IWorQLessDynamic
    {
        IFieldExpression FieldExpression { get; set; }
    }

    public interface IWorQLessDynamic2 : IWorQLessDynamic
    {
    }

    public interface IWorQLessRuleBooster : IWorQLessDynamic
    {
        object Value { get; set; }
    }
}