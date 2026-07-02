using System.Text;

namespace ICS.Portal.Data.Custom
{
    public class ParallelSplitOption
    {
        public string Name { get; set; }
        public bool Required { get; set; }
        public bool IsTerminator { get; }

        public ParallelSplitOption(string name, bool required, bool isTerminator)
        {
            Name = name;
            Required = required;
            IsTerminator = isTerminator;
        }
        public ParallelSplitOption(string splitOptionData)
        {
            string[] parts = splitOptionData.Split("¦");
            Name = parts[0];
            Required = bool.Parse(parts[1]);
            IsTerminator = bool.Parse(parts[2]);
        }

        public override string ToString()
        {
            return $"{Name}¦{Required}¦{IsTerminator}";
        }
    }

    public class ParallelSplitOptions
    {
        public List<ParallelSplitOption> Options { set; get; }

        public ParallelSplitOptions(string optionSet)
        {
            Options = new();

            if (optionSet.Contains("¦"))
            {
                string[] sets = optionSet.Split("¦¦");
                foreach (var set in sets)
                {
                    Options.Add(new(set));
                }
            }
            else
            {
                // This can be removed after all workflows have been updated to use the new format
                string[] parts = optionSet.Split("|");
                string[] optionNames = parts[0].Split(",");
                foreach (var optionName in optionNames)
                {
                    Options.Add(new(optionName, false, false));
                }
                Options.Add(new(parts[1], false, true));
            }
        }

        public ParallelSplitOptions()
        {
            Options = new();
        }

        public override string ToString()
        {
            bool first = true;
            StringBuilder stringBuilder = new StringBuilder();
            foreach (var option in Options)
            {
                if (!first)
                {
                    stringBuilder.Append("¦¦");
                }
                stringBuilder.Append(option.ToString());
                first = false;
            }
            return stringBuilder.ToString();
        }

        public bool AllPathsExecuted(List<string> previousValues)
        {
            var options = GetOptionNames();
            foreach (var option in options)
            {
                if (!previousValues.Contains(option))
                {
                    return false;
                }
            }
            return true;
        }

        public List<string> GetOptionNames()
        {
            return Options.Where(s => s.IsTerminator == false).Select(s => s.Name).ToList();
        }

        public string GetTerminatingOptionName()
        {
            return Options.FirstOrDefault(s => s.IsTerminator == true)!.Name;
        }
    }
}
