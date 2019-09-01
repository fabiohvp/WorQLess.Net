namespace WorQLess.Net
{
    public interface IWorQLessWorkflowContainer
    {
        IWorkflowContainer WorkflowContainer { get; set; }
    }

    public interface IWorQLessDynamic
    {
        IFieldExpression FieldExpression { get; set; }
    }

    public interface IWorQLessRuleBooster : IWorQLessDynamic
    {
        object Value { get; set; }
    }
}