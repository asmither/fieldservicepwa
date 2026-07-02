//namespace ICS.Portal.Data.Custom;
//public class WorkflowState
//{
//    public long Id { set; get; }

//    public List<WorkflowStateStep> workflowStateSteps { set; get; } = new List<WorkflowStateStep>();

//    public void Push(int workflowStepResultId, int loopIndex)
//    {
//        workflowStateSteps.Add(new WorkflowStateStep(workflowStepResultId, loopIndex));
//    }

//    public WorkflowStateStep? Pop()
//    {
//        if (workflowStateSteps.Count != 0)
//        {
//            var item = workflowStateSteps.Last();
//            workflowStateSteps.Remove(item);
//            return item;
//        }

//        return null;
//    }

//    public void Clear()
//    {
//        workflowStateSteps.Clear();
//    }
//}
