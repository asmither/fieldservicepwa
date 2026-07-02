namespace ICS.Portal.Data.Custom
{
    public class CalculationItems
    {
        public CalculationItems()
        {
            VariableMaps = new();
        }
        public CalculationItems(string? calculationItemsString)
        {
            VariableMaps = new();

            if(calculationItemsString is not null)
            {
                string[] details = calculationItemsString.Split('¦');
                foreach (var detail in details)
                {
                    if (detail.StartsWith("JSCRIPT"))
                    {
                        this.script = detail.Substring("JSCRIPT=".Length);
                    }
                    else if (detail.StartsWith("REPLACEDJSCRIPT"))
                    {
                        this.replacedScript = detail.Substring("REPLACEDJSCRIPT=".Length);
                    }
                    else if (detail.StartsWith("PRESENTATION"))
                    {
                        this.presentation = detail.Substring("PRESENTATION=".Length);
                    }
                    else if (detail.StartsWith("REPLACEDPRESENTATION"))
                    {
                        this.replacedPresentation = detail.Substring("REPLACEDPRESENTATION=".Length);
                    }
                    else
                    {
                        string[] parts = detail.Split("&");
                        string[] stepParts = parts[0].Split('=');
                        string[] variableParts = parts[1].Split('=');

                        int stepId = int.Parse(stepParts[1]);
                        string variableName = variableParts[1];

                        VariableMaps.Add(new(stepId, variableName));
                    }
                }
            }
        }

        public List<VariableMap> VariableMaps { get; }

        public override string ToString()
        {
            string result = string.Empty;

            if(VariableMaps.Count != 0)
            {
                string[] items = new string[VariableMaps.Count];
                for (int idx = 0; idx != items.Length; idx++)
                {
                    var map = VariableMaps[idx];
                    items[idx] = $"stepId={map.StepId}&variableName={map.VariableName}";
                }
                result = string.Join('¦', items);
            }

            if(script is not null)
            {
                if(result != string.Empty)
                {
                    result += "¦";
                }
                result += $"JSCRIPT={this.script}";
            }

            if(replacedScript is not null)
            {
                if (result != string.Empty)
                {
                    result += "¦";
                }
                result += $"REPLACEDJSCRIPT={this.replacedScript}";
            }
            if (presentation is not null)
            {
                if (result != string.Empty)
                {
                    result += "¦";
                }
                result += $"PRESENTATION={this.presentation}";
            }
            if (replacedPresentation is not null)
            {
                if (result != string.Empty)
                {
                    result += "¦";
                }
                result += $"REPLACEDPRESENTATION={this.presentation}";
            }
            return result;
        }

        public void SaveMap(int stepId, string variableName)
        {
            var existing = VariableMaps.FirstOrDefault(m => m.StepId == stepId);
            if(existing is not null)
            {
                existing.VariableName = variableName;
            }
            else
            {
                VariableMaps.Add(new(stepId, variableName));
            }
        }

        public string? GetVariableName(int stepId)
        {
            var existing = VariableMaps.FirstOrDefault(m => m.StepId == stepId);
            if(existing is not null)
            {
                return existing.VariableName;
            }
            return null;
        }

        private string? script;
        private string? replacedScript;
        public void SetScript(string script)
        {
            this.script = script;
        }
        public void SetReplacedScript(string? script)
        {
            replacedScript = script;
        }
        public string? GetReplaceScript()
        {
            return replacedScript;
        }

        public string? presentation;
        public string? replacedPresentation;
        public void SetPresentation(string presentation)
        {
            this.presentation = presentation;
        }
        public void SetReplacedPresentation(string? presentation)
        {
            this.replacedPresentation = presentation;
        }
        public string? GetReplacedPresentation()
        {
            return this.replacedPresentation;
        }
        public string? Script
        {
            get
            {
                return script;
            }
        }

        public string? Presentation
        {
            get
            {
                return presentation;
            }
        }
    }

    public class VariableMap
    {
        public VariableMap(int stepId, string? variableName)
        {
            StepId = stepId;
            VariableName = variableName;
        }

        public int StepId { set; get; }
        public string? VariableName { set; get; }
    }
}
