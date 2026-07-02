using System.Text.Json.Serialization;

namespace ICS.Portal.Data.Commands.Models;
public partial class WorkflowStepResultValueSaveInput
{
    public long Id
    {
        get
        {
            long id = long.Parse($"{WorkflowStepResultId.ToString()!.PadRight(11, '0')}{LoopIndex.ToString()!.PadRight(4, '0')}");
            return id;
        }
    }
}
