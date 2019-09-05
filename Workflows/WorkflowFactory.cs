using System;
using WorQLess.Extensions;
using WorQLess.Models;

namespace WorQLess.Workflows
{
    public static class WorkflowFactory
    {
        public static IWorQLessWorkflow Create(Type sourceType, Type returnType, IWorkflowContainer workflowContainer)
        {
            var workflow = (IWorQLessWorkflow)Reflection
                .CreateWorkflow(sourceType, workflowContainer.Name, new Type[] { sourceType, returnType }, workflowContainer.Args);

            workflow.WorkflowContainer = workflowContainer;
            return workflow;
        }
    }
}

