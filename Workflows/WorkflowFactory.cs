using System;
using WorQLess.Extensions;

namespace WorQLess.Net.Workflows
{
    public static class WorkflowFactory
    {
        public static object Create(Type sourceType, Type returnType, IWorkflowContainer workflowContainer)
        {
            var workflow = (IWorQLessWorkflowContainer)Reflection
                .CreateWorkflow(sourceType, workflowContainer.Name, new Type[] { sourceType, returnType }, workflowContainer.Args);

            workflow.WorkflowContainer = workflowContainer;
            return workflow;
        }
    }
}
