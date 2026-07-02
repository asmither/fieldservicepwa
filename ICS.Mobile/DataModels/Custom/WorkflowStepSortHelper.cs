using ICS.Portal.Data.Enumerations;
using ICS.Portal.Data.Queries.Models;

using System.Text.Json;

namespace ICS.Portal.Data.Custom
{

    /* Add Columns to WorkflowResultDetailResult.WorkflowStepValue
    public int StepOrder { set; get; }
    public int LoopPathIndexI { set; get; }
    public int LoopPathIndexII { set; get; }
    public int LoopPathIndexIII { set; get; }
    public int LoopPathIndexIV { set; get; }
    public int LoopPathIndexV { set; get; }
    */

    public class Step
    { 
        public int WorkflowStepResultId { set; get; }
        public int WorkflowStepId { set; get; }
        public int WorkflowDataTypeId { set; get; }
        public int Position { set; get; }
        public int? SplitStepId { set; get; }
        public string? SplitOption { set; get; }
        public string? WorkflowDataTypeDetail { set; get; }
        public bool IsSplitOrSwitch()
        {
            return WorkflowDataTypeId == (int)WorkflowDataTypes.Split || WorkflowDataTypeId == (int)WorkflowDataTypes.AutoSwitch || WorkflowDataTypeId == (int)WorkflowDataTypes.ParallelSplit || WorkflowDataTypeId == (int)WorkflowDataTypes.ValueSwitch;
        }
        public int Order { set; get; }

        private List<string>? _SplitOptions = null;
        public List<string> SplitOptions()
        {
            if (_SplitOptions is null)
            {
                WorkflowDataTypeDetail details = new(WorkflowDataTypeDetail);
                switch (WorkflowDataTypeId)
                {
                    case (int)WorkflowDataTypes.AutoSwitch:
                        if (details.Values.ContainsKey(nameof(AutoSwitcher)))
                        {
                            var autoSwitcher = JsonSerializer.Deserialize<AutoSwitcher>(details.Values[nameof(AutoSwitcher)]);
                            if (autoSwitcher is not null)
                            {
                                _SplitOptions = new();
                                foreach (var switcher in autoSwitcher.SwitchSelectors)
                                {
                                    _SplitOptions.Add(switcher.Name);
                                }
                            }
                        }
                        break;
                    case (int)WorkflowDataTypes.ParallelSplit:
                        if (details.Values.ContainsKey("Options"))
                        {
                            string options = details.Values["Options"];
                            ParallelSplitOptions parallelSplitOptions = new ParallelSplitOptions(options);
                            _SplitOptions = parallelSplitOptions.GetOptionNames();
                            _SplitOptions.Add(parallelSplitOptions.GetTerminatingOptionName());
                        }
                        break;
                    case (int)WorkflowDataTypes.Split:
                        if (details.Values.ContainsKey("Options"))
                        {
                            string options = details.Values["Options"];
                            _SplitOptions = options.Split(',', '|').ToList();
                        }
                        break;

                    case (int)WorkflowDataTypes.ValueSwitch:
                        if (details.Values.ContainsKey(nameof(ValueSwitcher)))
                        {
                            var valueSwitcher = JsonSerializer.Deserialize<ValueSwitcher>(details.Values[nameof(ValueSwitcher)]);
                            if (valueSwitcher is not null)
                            {
                                _SplitOptions = new();
                                foreach (var switcher in valueSwitcher.ValueSelectors)
                                {
                                    _SplitOptions.Add(switcher.Name!);
                                }
                            }
                        }
                        break;
                }
            }
            return _SplitOptions ?? new();
        }
    }

    public class WorkflowStepSortHelper
    {
        private readonly List<WorkflowResultDetailResult.WorkflowStep> allSteps;
        private readonly List<WorkflowResultDetailResult.WorkflowStepValue> allValues;

        public WorkflowStepSortHelper(List<WorkflowResultDetailResult.WorkflowStep> allSteps, List<WorkflowResultDetailResult.WorkflowStepValue> allValues)
        {
            this.allSteps = allSteps;
            this.allValues = allValues;
        }
        public List<WorkflowResultDetailResult.WorkflowStepValue> SortSteps()
        {
            List<WorkflowResultDetailResult.WorkflowStep> result = new(allSteps.Count);
            List<Step> steps = new(allSteps.Count);

            foreach (var step in allSteps)
            {
                steps.Add(new()
                {
                    Position = step.Position,
                    SplitStepId = step.SplitStepId,
                    SplitOption = step.SplitOption,
                    WorkflowStepId = step.WorkflowStepId,
                    WorkflowDataTypeDetail = step.WorkflowDataTypeDetail,
                    WorkflowDataTypeId = step.WorkflowDataTypeId,
                    WorkflowStepResultId = step.WorkflowStepResultId
                });
            }

            SortSteps(steps);
            Dictionary<int, int> stepOrders = new(steps.Count);

            for (int idx = 0; idx != steps.Count; idx++)
            {
                stepOrders.Add(steps[idx].WorkflowStepResultId, idx);
            }


            bool artificialValues = allValues.Any(s => s.WorkflowStepResultValueId == 0);
            if (artificialValues)
            {
                for(int idx = 0; idx!= allValues.Count; idx++)
                {
                    allValues[idx].WorkflowStepResultValueId = idx;
                }
            }


            SetLoopIndexes(allValues);

            foreach (var value in allValues)
            {
                value.StepOrder = stepOrders[value.WorkflowStepResultId];
                
            }

            return allValues
                .OrderBy(s => s.LoopIndex)
                .ThenBy(s => s.StepOrder)
                .ThenBy(s => s.LoopPathIndexI)
                .ThenBy(s => s.LoopPathIndexII)
                .ThenBy(s => s.LoopPathIndexIII)
                .ThenBy(s => s.LoopPathIndexIV)
                .ThenBy(s => s.LoopPathIndexV)
                .ToList();
        }

        private void SortSteps(List<Step> steps)
        {
            var step = steps.FirstOrDefault(s => s.SplitOption is null && s.Position == 1);

            while (step is not null)
            {
                step.Order = order++;
                if (step.IsSplitOrSwitch())
                {
                    List<string> options = step.SplitOptions();
                    foreach (var option in options)
                    {
                        SortSteps(steps, step, option);
                    }
                }
                step = steps.FirstOrDefault(s => s.SplitStepId == step.SplitStepId && s.Position == step.Position + 1);
            }

            steps.Sort((a, b) => a.Order.CompareTo(b.Order));
        }


        private void SortSteps(List<Step> steps, Step splitStep, string splitOption)
        {
            int stepId = splitStep.WorkflowStepId;
            var step = steps.FirstOrDefault(s => s.SplitStepId == splitStep.WorkflowStepId && s.SplitOption == splitOption && s.Position == 1);
            while (step is not null)
            {
                step.Order = order++;
                if (step.IsSplitOrSwitch())
                {
                    List<string> options = step.SplitOptions();
                    foreach (var option in options)
                    {
                        SortSteps(steps, step, option);
                    }
                }
                step = steps.FirstOrDefault(s => s.SplitStepId == step.SplitStepId && s.SplitOption == splitOption && s.Position == step.Position + 1);
            }
        }


        private int order = 0;

        private void SetLoopIndexes(List<WorkflowResultDetailResult.WorkflowStepValue> values)
        {
            foreach (var value in values)
            {
                List<int> indexes = new();

                if (!string.IsNullOrEmpty(value.LoopPath))
                {
                    string[] paths = value.LoopPath.Split('|');
                    foreach (var path in paths)
                    {
                        if (path.Contains('.'))
                        {
                            var parts = path.Split(".");
                            indexes.Add(int.Parse(parts[1]));
                        }
                    }
                }

                for (int idx = 0; idx != indexes.Count; idx++)
                {
                    switch (idx)
                    {
                        case 0:
                            value.LoopPathIndexI = idx;
                            break;
                        case 1:
                            value.LoopPathIndexII = idx;
                            break;
                        case 2:
                            value.LoopPathIndexIII = idx;
                            break;
                        case 3:
                            value.LoopPathIndexIV = idx;
                            break;
                        case 4:
                            value.LoopPathIndexV = idx;
                            break;

                    }
                }
            }
        }
    }
}
