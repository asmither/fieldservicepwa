using System.Runtime.CompilerServices;
using System.Text.Json;

namespace ICS.Portal.Data.Custom
{
    public static class Extensions
    {
        public static string? ToJson(this object obj)
        {
            return JsonSerializer.Serialize(obj);
        }

        public static T? ToObject<T>(this string json)
        {
            if (!string.IsNullOrEmpty(json))
            {
                return JsonSerializer.Deserialize<T>(json);
            }

            return default;
        }

        public static T? JsonClone<T>(this T obj)
        {
            if (obj is null)
            {
                return default;
            }
            return obj.ToJson()!.ToObject<T>();
        }
    }

    public class AutoSwitcher
    {
        public int DataSourceId { set; get; }

        public List<SwitchSelector> SwitchSelectors { set; get; }

    }

    public class SwitchSelector
    {
        /// <summary>
        /// Also serves as the sort order
        /// </summary>
        public int Id { set; get; }
        /// <summary>
        /// Name for the selector
        /// </summary>
        public string Name { set; get; }

        /// <summary>
        /// List of entity ids that will trigger this selector
        /// </summary>
        public List<int> Ids { set; get; }

        /// <summary>
        /// Indicates that this is the default of catch all
        /// </summary>
        public bool IsDefault { set; get; }
    }

    public class ValueSwitcher
    {
        public static ValueSwitcher CreateNew()
        {
            ValueSwitcher result = new ValueSwitcher();
            result.ValueSelectors.Add(new ValueSelector()
            {
                Id = 0,
                Name = "Default",
                IsDefault = true
            });
            return result;
        }
        public int StepId { set; get; }
        public List<ValueSelector> ValueSelectors { set; get; } = new();

        public ValueSelector NewSelector()
        {
            var maxId = ValueSelectors.MaxBy(x => x.Id)?.Id ?? 0;
            maxId++;
            return new ValueSelector()
            {
                Id = maxId
            };
        }
    }

    public class ValueSelector
    {
        public int Id { set; get; }
        public string? Name { set; get; }
        public decimal? MinValue { set; get; }
        public decimal? MaxValue { set; get; }
        public bool IsDefault { set; get; }

        public ValueSelector Clone()
        {
            return new ValueSelector()
            {
                Id = Id,
                Name = Name,
                MinValue = MinValue,
                MaxValue = MaxValue,
                IsDefault = IsDefault
            };
        }
    }
}
