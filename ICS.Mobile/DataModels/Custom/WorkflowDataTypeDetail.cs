using System.Text.Json;

namespace ICS.Portal.Data.Custom;

public class WorkflowDataTypeDetail
{
    private readonly Dictionary<string, string> values;
    public Dictionary<string, string> Values => values;
    public WorkflowDataTypeDetail(string? json)
    {
        if (!string.IsNullOrEmpty(json))
        {
            values = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
        }
        else
        {
            values = new Dictionary<string, string>();
        }
    }

    public WorkflowDataTypeDetail(Dictionary<string, string> values)
    {
        this.values = values;
    }
    public decimal? MinValue
    {
        get
        {
            if (values.ContainsKey(nameof(MinValue)))
            {
                return decimal.Parse(values[nameof(MinValue)]);
            }
            return null;
        }
    }
    public decimal? MaxValue
    {
        get
        {
            if (values.ContainsKey(nameof(MaxValue)))
            {
                return decimal.Parse(values[nameof(MaxValue)]);
            }
            return null;
        }
    }
    public int? MinLength
    {
        get
        {
            if (values.ContainsKey(nameof(MinLength)))
            {
                return int.Parse(values[nameof(MinLength)]);
            }
            return null;
        }
    }
    public int? MaxLength
    {
        get
        {
            if (values.ContainsKey(nameof(MaxLength)))
            {
                return int.Parse(values[nameof(MaxLength)]);
            }
            return null;
        }
    }
    public DateOnly? MinDate
    {
        get
        {
            if (values.ContainsKey(nameof(MinDate)))
            {
                return DateOnly.Parse(values[nameof(MinDate)]);
            }
            return null;
        }
    }
    public DateOnly? MaxDate
    {
        get
        {
            if (values.ContainsKey(nameof(MaxDate)))
            {
                return DateOnly.Parse(values[nameof(MaxDate)]);
            }
            return null;
        }
    }
    public bool? InterruptExisting
    {
        get
        {
            if (values.ContainsKey(nameof(InterruptExisting)))
            {
                return bool.Parse(values[nameof(InterruptExisting)]);
            }
            return null;
        }
    }
    public int? WorkflowId
    {
        get
        {
            if (values.ContainsKey(nameof(WorkflowId)))
            {
                return int.Parse(values[nameof(WorkflowId)]);
            }
            return null;
        }
    }
    public int? DecimalPlaces
    {
        get
        {
            if (values.ContainsKey(nameof(DecimalPlaces)))
            {
                return int.Parse(values[nameof(DecimalPlaces)]);
            }
            return null;
        }
    }
    public int? SequenceQueryId
    {
        get
        {
            if (values.ContainsKey(nameof(SequenceQueryId)))
            {
                return int.Parse(values[nameof(SequenceQueryId)]);
            }
            return null;
        }
    }
    public int? DataQueryId
    {
        get
        {
            if (values.ContainsKey(nameof(DataQueryId)))
            {
                return int.Parse(values[nameof(DataQueryId)]);
            }
            return null;
        }
    }
    public bool? AllowAdditions
    {
        get
        {
            if (values.ContainsKey(nameof(AllowAdditions)))
            {
                return bool.Parse(values[nameof(AllowAdditions)]);
            }
            return null;
        }
    }
    public bool AllowAdd
    {
        get
        {
            if (values.ContainsKey(nameof(AllowAdd)))
            {
                return bool.Parse(values[nameof(AllowAdd)]);
            }
            return false;
        }
    }
    public bool AllowEdit
    {
        get
        {
            if (values.ContainsKey(nameof(AllowEdit)))
            {
                return bool.Parse(values[nameof(AllowEdit)]);
            }
            return false;
        }
    }
    public bool IgnoreAttributes
    {
        get
        {
            if (values.ContainsKey(nameof(IgnoreAttributes)))
            {
                return bool.Parse(values[nameof(IgnoreAttributes)]);
            }
            return false;
        }
    }

    public bool SingleSelection
    {
        get
        {
            if (values.ContainsKey(nameof(SingleSelection)))
            {
                return bool.Parse(values[nameof(SingleSelection)]);
            }
            return false;
        }
        set
        {
            values[nameof(SingleSelection)] = value.ToString()!;
        }
    }

    public int? WorkflowDataStepId
    {
        get
        {
            if (values.ContainsKey(nameof(WorkflowDataStepId)))
            {
                return int.Parse(values[nameof(WorkflowDataStepId)]);
            }
            return null;
        }
    }
    public List<string> ParallelSplitOptions
    {
        get
        {
            if (values.ContainsKey(nameof(Options)))
            {
                string options = values[nameof(Options)];
                ParallelSplitOptions splitOptions = new ParallelSplitOptions(options);
                List<string> result = splitOptions.GetOptionNames();
                result.Add(splitOptions.GetTerminatingOptionName());
                return result;
            }
            return null;
        }
    }

    public List<string>? Options
    {
        get
        {
            if (values.ContainsKey(nameof(Options)))
            {
                string options = values[nameof(Options)];
                return options.Split(',','|').ToList();
            }
            return null;
        }
    }
    public string? RegEx
    {
        get
        {
            if (values.ContainsKey("RegularExpression"))
            {
                return values["RegularExpression"];
            }
            return null;
        }
    }
    public int? LoopExitType
    {
        get
        {
            if (values.ContainsKey(nameof(LoopExitType)))
            {
                return int.Parse(values[nameof(LoopExitType)]);
            }
            return null;
        }
    }
    public string? LoopContinueText
    {
        get
        {
            if (values.ContainsKey(nameof(LoopContinueText)))
            {
                return values[nameof(LoopContinueText)];
            }
            return null;
        }
    }
    public string? LoopExitText
    {
        get
        {
            if (values.ContainsKey(nameof(LoopExitText)))
            {
                return values[nameof(LoopExitText)];
            }
            return null;
        }
    }
    public int? FixedLoopCount
    {
        get
        {
            if (values.ContainsKey(nameof(FixedLoopCount)))
            {
                return int.Parse(values[nameof(FixedLoopCount)]);
            }
            return null;
        }
    }
    public int? LoopToStepId
    {
        get
        {
            if (values.ContainsKey(nameof(LoopToStepId)))
            {
                return int.Parse(values[nameof(LoopToStepId)]);
            }
            return null;
        }
    }
    public bool? IsMasterLoop
    {
        get
        {
            if (values.ContainsKey(nameof(IsMasterLoop)))
            {
                return bool.Parse(values[nameof(IsMasterLoop)]);
            }
            return null;
        }
    }
    public string? Labels
    {
        get
        {
            if (values.ContainsKey(nameof(Labels)))
            {
                return values[nameof(Labels)];
            }
            return null;
        }
    }
    public string? WorkflowDataText
    {
        get
        {
            if (values.ContainsKey(nameof(WorkflowDataText)))
            {
                return values[nameof(WorkflowDataText)];
            }
            return null;
        }
    }
    public int TrueImageRequirementId
    {
        get
        {
            if (values.ContainsKey(nameof(TrueImageRequirementId)))
            {
                return int.Parse(values[nameof(TrueImageRequirementId)]);
            }
            return 0;
        }
    }
    public int FalseImageRequirementId
    {
        get
        {
            if (values.ContainsKey(nameof(FalseImageRequirementId)))
            {
                return int.Parse(values[nameof(FalseImageRequirementId)]);
            }
            return 0;
        }
    }
    public int TrueNoteRequirementId
    {
        get
        {
            if (values.ContainsKey(nameof(TrueNoteRequirementId)))
            {
                return int.Parse(values[nameof(TrueNoteRequirementId)]);
            }
            return 0;
        }
    }
    public int FalseNoteRequirementId
    {
        get
        {
            if (values.ContainsKey(nameof(FalseNoteRequirementId)))
            {
                return int.Parse(values[nameof(FalseNoteRequirementId)]);
            }
            return 0;
        }
    }
    public string? TruePrompt
    {
        get
        {
            if (values.ContainsKey(nameof(TruePrompt)))
            {
                return values[nameof(TruePrompt)];
            }
            return null;
        }
    }
    public string? FalsePrompt
    {
        get
        {
            if (values.ContainsKey(nameof(FalsePrompt)))
            {
                return values[nameof(FalsePrompt)];
            }
            return null;
        }
    }
    public string? TrueImageLabel
    {
        get
        {
            if (values.ContainsKey(nameof(TrueImageLabel)))
            {
                return values[nameof(TrueImageLabel)];
            }
            return null;
        }
    }
    public string? TrueNoteLabel
    {
        get
        {
            if (values.ContainsKey(nameof(TrueNoteLabel)))
            {
                return values[nameof(TrueNoteLabel)];
            }
            return null;
        }
    }
    public string? FalseImageLabel
    {
        get
        {
            if (values.ContainsKey(nameof(FalseImageLabel)))
            {
                return values[nameof(FalseImageLabel)];
            }
            return null;
        }
    }
    public string? FalseNoteLabel
    {
        get
        {
            if (values.ContainsKey(nameof(FalseNoteLabel)))
            {
                return values[nameof(FalseNoteLabel)];
            }
            return null;
        }
    }
    public int? TrueJumpConditionTypeId
    {
        get
        {
            if (values.ContainsKey(nameof(TrueJumpConditionTypeId)))
            {
                return int.Parse(values[nameof(TrueJumpConditionTypeId)]);
            }
            return null;
        }
    }
    public int? FalseJumpConditionTypeId
    {
        get
        {
            if (values.ContainsKey(nameof(FalseJumpConditionTypeId)))
            {
                return int.Parse(values[nameof(FalseJumpConditionTypeId)]);
            }
            return null;
        }
    }
    public string? TrueJumpConditionValue
    {
        get
        {
            if (values.ContainsKey(nameof(TrueJumpConditionValue)))
            {
                return values[nameof(TrueJumpConditionValue)];
            }
            return null;
        }
    }
    public string? FalseJumpConditionValue
    {
        get
        {
            if (values.ContainsKey(nameof(FalseJumpConditionValue)))
            {
                return values[nameof(FalseJumpConditionValue)];
            }
            return null;
        }
    }
    public int? TrueJumpStepId
    {
        get
        {
            if (values.ContainsKey(nameof(TrueJumpStepId)))
            {
                return int.Parse(values[nameof(TrueJumpStepId)]);
            }
            return null;
        }
    }
    public int? FalseJumpStepId
    {
        get
        {
            if (values.ContainsKey(nameof(FalseJumpStepId)))
            {
                return int.Parse(values[nameof(FalseJumpStepId)]);
            }
            return null;
        }
    }
    public bool IsValidForType(int workflowDataTypeId)
    {
        // TODO: Add validation
        return true;
    }
    public string Serialize()
    {
        var nonNullValues = values.Where(x => x.Value != null).ToDictionary(x => x.Key, x => x.Value);
        return JsonSerializer.Serialize(nonNullValues);
    }
}
