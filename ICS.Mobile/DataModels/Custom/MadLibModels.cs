using System;
using System.Collections.Generic;

namespace ICS.Portal.Data.Custom
{
    /// <summary>
    /// Represents a MadLib form definition
    /// </summary>
    public class MadLibForm
    {
        public int MadLibId { get; set; }
        public string MadLibName { get; set; } = string.Empty;
        public string Prompt { get; set; } = string.Empty;
        public string Caption { get; set; } = string.Empty;
        public string ReportName { get; set; } = string.Empty;
        public string HelpText { get; set; } = string.Empty;
        public bool IsVisible { get; set; } = true;
        public int ViewMode { get; set; } = 0; // 0=Classic step-by-step, 1+=Sentence mode
        public List<MadLibStep> Steps { get; set; } = new List<MadLibStep>();
        public List<MadLibView> Views { get; set; } = new List<MadLibView>();
    }
    
    /// <summary>
    /// Represents a view/panel for sentence mode in a MadLib form
    /// </summary>
    public class MadLibView
    {
        public int ViewId { get; set; }
        public int MadLibId { get; set; }
        public string ViewName { get; set; } = string.Empty;
        public int ViewOrder { get; set; } // Order of this view/panel (1, 2, 3...)
        public string ViewTemplate { get; set; } = string.Empty; // The sentence template with {StepId} placeholders
        public string HelpText { get; set; } = string.Empty;
        public bool IsVisible { get; set; } = true;
        public bool ShowAllFields { get; set; } = true; // If false, show fields one at a time as completed
    }

    /// <summary>
    /// Represents a step/field in a MadLib form
    /// </summary>
    public class MadLibStep
    {
        public long StepId { get; set; }
        public int MadLibId { get; set; }
        public string Prompt { get; set; } = string.Empty;
        public string Caption { get; set; } = string.Empty;
        public string HelpText { get; set; } = string.Empty;
        public int? MinSize { get; set; }
        public int? MaxSize { get; set; }
        public string StepValueRegEx { get; set; } = string.Empty;
        public char? MaskChar { get; set; }
        public PresetType? PresetType { get; set; }
        public int DispOrder { get; set; }
        public long? JumpToStep { get; set; }
        public string JumpToRegEx { get; set; } = string.Empty;
        public string DefaultValue { get; set; } = string.Empty;
        public DataType DataType { get; set; }
        public bool IsRequired { get; set; } = false;
        public string? SentenceLabel { get; set; } // Optional inline label for sentence mode
        public List<MadLibStepOption> Options { get; set; } = new List<MadLibStepOption>();
        
        // Runtime properties
        public string CurrentValue { get; set; } = string.Empty;
        public bool IsValid { get; set; } = true;
        public string ValidationMessage { get; set; } = string.Empty;
        public bool IsFocused { get; set; } = false;
        public bool IsTouched { get; set; } = false;
    }

    /// <summary>
    /// Options for choice-based step types (dropdown, bubbles)
    /// </summary>
    public class MadLibStepOption
    {
        public long StepOptionId { get; set; }
        public long StepId { get; set; }
        public string StepPrompt { get; set; } = string.Empty;
        public string StepValue { get; set; } = string.Empty;
        public string StepCaption { get; set; } = string.Empty;
        public string StepHelpText { get; set; } = string.Empty;
        public int? MinSize { get; set; }
        public int? MaxSize { get; set; }
        public string ValidationRegExp { get; set; } = string.Empty;
        public char? MaskChar { get; set; }
        public PresetType? PresetType { get; set; }
        public int StepOptionOrder { get; set; }
        public bool IsSelected { get; set; } = false;
    }

    /// <summary>
    /// Stores the user's response to a step
    /// </summary>
    public class MadLibStepValue
    {
        public int StepValueId { get; set; }
        public int MadLibId { get; set; }
        public long StepId { get; set; }
        public long? StepOptionId { get; set; }
        public DataType DataTypeId { get; set; }
        public Guid UserId { get; set; } = Guid.NewGuid();
        public DateTime DateTimeStarted { get; set; }
        public DateTime? DateTimeCompleted { get; set; }
        public string Prompt { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }

    /// <summary>
    /// Data types for MadLib fields
    /// </summary>
    public enum DataType
    {
        ShortString = 1,
        MemoField = 2,
        BooleanField = 3,
        IntegerNumber = 4,
        MoneyAmount = 5,
        ChoiceDropDown = 6,
        ChoiceBubbles = 7,
        PhotoFile = 8,
        InformationPanel = 9,
        TimeOnly = 10,
        DateOnly = 11
    }

    /// <summary>
    /// Preset validation types for string fields
    /// </summary>
    public enum PresetType
    {
        Email = 1,
        Phone = 2,
        SSN = 3
    }

    /// <summary>
    /// Date format options
    /// </summary>
    public enum DateFormat
    {
        MonthYear,
        MonthDayYear
    }

    /// <summary>
    /// Boolean field display options
    /// </summary>
    public enum BooleanDisplayType
    {
        YesNo,
        OnOff,
        OneZero,
        TrueFalse
    }
}
