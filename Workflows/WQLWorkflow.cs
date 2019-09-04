using Enflow;

namespace WorQLess.Net.Workflows
{
    public abstract class WQLWorkflow<T, U> : Workflow<T, U>
    {
        IWorkflowContainer WorkflowContainer { get; set; }
    }
}
