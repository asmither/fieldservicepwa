using ICS.Portal.Data.Commands.Models;
using ICS.Portal.Data.Queries.Models;

namespace ICS.Mobile.DataModels.Converters;
public class Converters
{
    public static WorkflowStepResultValueSaveInput Convert(WorkflowResultDetailResult.WorkflowStepValue source)
    {
        return new WorkflowStepResultValueSaveInput
        {
            CompletedLatitude = source.CompletedLatitude,
            CompletedLongitude = source.CompletedLongitude,
            CompletedTimestamp = source.CompletedTimestamp,
            ImageFileName = source.ImageFileName,
            LoopIndex = source.LoopIndex,
            Note = source.Note,
            RowUserId = source.RowUserId,
            WorkflowResultId = source.WorkflowResultId,
            WorkflowStepResultId = source.WorkflowStepResultId,
            StartedLatitude = source.StartedLatitude,
            StartedLongitude = source.StartedLongitude,
            StartedTimestamp = source.StartedTimestamp,
            Value = source.Value,
            JsonPayload = source.JsonPayload
        };
    }

    public static long GenerateId(int workflowStepResultId, int loopIndex)
    {
        return long.Parse($"{workflowStepResultId.ToString().PadRight(11, '0')}{loopIndex.ToString().PadRight(4, '0')}");
    }

    public static long GenerateId(WorkflowResultDetailResult.WorkflowStep step, int loopIndex)
    {
        return long.Parse($"{step.WorkflowStepResultId!.ToString().PadRight(11, '0')}{loopIndex.ToString().PadRight(4, '0')}");
    }
}