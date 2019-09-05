using System.Collections.Generic;
using WorQLess.Models;

namespace WorQLess.Requests
{
    public interface IWorkflowRequest : IRequest
    {
        string Entity { get; set; }
        bool Evaluate { get; set; }
        string ProjectAs { get; set; }
        WorkflowOperand Operand { get; set; }
        ProjectionRequest Project { get; set; }
        IEnumerable<IRuleRequest> Rules { get; set; }
    }

    public class WorkflowRequest : IWorkflowRequest
    {
        public virtual string Name { get; set; }
        public virtual string Entity { get; set; }
        public virtual object Args { get; set; }
        public virtual string ProjectAs { get; set; }
        public virtual bool Evaluate { get; set; }
        public virtual WorkflowOperand Operand { get; set; }
        public virtual ProjectionRequest Project { get; set; }
        public virtual IEnumerable<IRuleRequest> Rules { get; set; }

        public WorkflowRequest()
        {
            Evaluate = true;
            Operand = WorkflowOperand.NewQuery;
            Rules = new HashSet<RuleRequest>();
            Project = new ProjectionRequest();
        }
    }
}