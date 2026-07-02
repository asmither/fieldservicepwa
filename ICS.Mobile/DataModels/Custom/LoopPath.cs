using System.Text;

namespace ICS.Portal.Data.Custom
{
    public class LoopPath
    {
        private readonly List<LoopPathItem> loopPathItems;
        public LoopPath()
        {
            loopPathItems = new();
        }

        public LoopPath(string? path)
        {
            loopPathItems = new();
            if (!string.IsNullOrEmpty(path))
            {
                string[] items = path.Split("|");
                for (int idx = 0; idx < items.Length; idx++)
                {
                    loopPathItems.Add(new(items[idx]));
                }
            }
        }

        //public LoopPathItem? First()
        //{
        //    if (loopPathItems.Count != 0)
        //    {
        //        return loopPathItems[0];
        //    }
        //    return null;
        //}
        //public void SetSplitOption(int stepId, string? option)
        //{
        //    var loopPathItem = loopPathItems.LastOrDefault();

        //    if (loopPathItem is not null && loopPathItem.StepId == stepId && loopPathItem.IsParallelSplit)
        //    {
        //        loopPathItem.Option = option;
        //    }
        //    else
        //    {
        //        loopPathItems.Add(new(stepId, option));
        //    }
        //}

        //{"Options":"Terminating Option\u00A6True\u00A6True\u00A6\u00A6A\u00A6False\u00A6False\u00A6\u00A6B\u00A6True\u00A6False\u00A6\u00A6C\u00A6False\u00A6False"}

        /// <summary>
        /// Sets the path for looping by either incrementing the current loop or setting the loop to the new step id
        /// </summary>
        /// <param name="workflowStepResultId"></param>
        public void AttachOrIncrementLoop(int workflowStepResultId)
        {
            var loopPathItem = loopPathItems.LastOrDefault();
            if (loopPathItem is not null && loopPathItem.StepId == workflowStepResultId)
            {
                loopPathItem.Index++;
            }
            else
            {
                loopPathItems.Add(new(workflowStepResultId, 0));
            }
        }

        //public void AttachParallelSplitOption(int workflowStepResultId, string option)
        //{
        //    var loopPathItem = loopPathItems.LastOrDefault();
        //    if (loopPathItem is not null && loopPathItem.StepId == workflowStepResultId)
        //    {
        //        loopPathItem.Option = option;
        //    }
        //    else
        //    {
        //        loopPathItems.Add(new(workflowStepResultId, option));
        //    }
        //}

        public override string ToString()
        {
            if (loopPathItems.Count != 0)
            {
                StringBuilder builder = new StringBuilder();
                bool firstLoop = true;

                foreach (var item in loopPathItems)
                {
                    if (!firstLoop)
                    {
                        builder.Append("|");
                    }
                    firstLoop = false;
                    if (item.IsLoop)
                    {
                        builder.Append($"{item.StepId}.{item.Index}");
                    }
                    else
                    {
                        builder.Append($"{item.StepId}:{item.Option}");
                    }
                }
                return builder.ToString();
            }
            return String.Empty;
        }

        //public bool IsInParallelSplit()
        //{
        //    if (Current() is not null && Current().Option is not null) return true;
        //    return false;
        //}

        //public bool IsExitingParallelSplit()
        //{
        //    var loopPathItem = loopPathItems.LastOrDefault();
        //    if (loopPathItem is not null && loopPathItem.IsParallelSplit && loopPathItem.Option == "EXIT")
        //    {
        //        return true;
        //    }
        //    return false;
        //}
        public LoopPathItem? Current()
        {
            if (loopPathItems.Count != 0)
            {
                return loopPathItems.Last();
            }
            return null;
        }

        public void Detach()
        {
            if (loopPathItems.Count != 0)
            {
                loopPathItems.RemoveAt(loopPathItems.Count - 1);
            }
        }
    }
    public class LoopPathItem
    {
        private readonly bool isLoop;
        public bool IsLoop => isLoop;
        public bool IsParallelSplit => !isLoop;
        public LoopPathItem(string pathItem)
        {
            isLoop = pathItem.Contains('.');

            string[] pathItemParts = pathItem.Split('.', ':');
            StepId = int.Parse(pathItemParts[0]);
            if (isLoop)
            {
                Index = int.Parse(pathItemParts[1]);
            }
            else
            {
                Option = pathItemParts[1];
            }
        }

        //public LoopPathItem(int stepId, string? option)
        //{
        //    isLoop = false;
        //    StepId = stepId;
        //    Option = option;
        //}

        public LoopPathItem(int stepId, int index)
        {
            isLoop = true;
            StepId = stepId;
            Index = index;
        }

        public int StepId { set; get; }
        public int? Index { set; get; }

        public string? Option { set; get; }

    }
}
