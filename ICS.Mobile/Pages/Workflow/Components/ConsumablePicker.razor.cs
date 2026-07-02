using ICS.Portal.Data.Custom;

using Microsoft.AspNetCore.Components;

using System.Text.Json;

namespace ICS.Mobile.Pages.Workflow.Components
{
    public partial class ConsumablePicker
    {
        protected override void NoteChanged()
        {
            NextDisabled = !IsValid();
        }
        protected override void ImageChanged()
        {
            NextDisabled = !IsValid();
        }

        private void SetQuantity(SelectableConsumable consumable, ChangeEventArgs args)
        {
            if (args is not null && consumable is not null)
            {
                if (args.Value is not null)
                {
                    if (decimal.TryParse(args.Value.ToString(), out decimal result))
                    {
                        if (consumable.Quantity != result)
                        {
                            consumable.Quantity = result;
                            UpdateValue();
                            StateHasChanged();
                        }
                    }
                }
            }
        }

        private void UpdateValue()
        {
            bool invoke = false;

            List<SelectableConsumable> selected = Consumables.FindAll(c => c.IsSelected == true);
            if (selected.Count == 0)
            {
                Input.Value = null;
            }
            else
            {
                List<IConsumableDTO> DTOs = selected.Cast<IConsumableDTO>().ToList();
                string json = JsonSerializer.Serialize(DTOs);
                Input.Value = json;
            }
            NextDisabled = !IsValid();
        }

        private void SetValue(SelectableConsumable value)
        {
            value.IsSelected = !value.IsSelected;
            UpdateValue();
        }

        [Parameter]
        public List<SelectableConsumable> Consumables { set; get; }

        protected override bool IsValid()
        {
            if (WorkflowStep!.IsRequired)
            {
                if (string.IsNullOrEmpty(Input.Value))
                {
                    return false;
                }
            }
            return base.IsValid();
        }

        protected override void OnParametersSet()
        {
            Initialized = false;

            base.OnParametersSet();

            if (!string.IsNullOrEmpty(Input.Value))
            {
                try
                {
                    List<ConsumableDTO> DTOs = JsonSerializer.Deserialize<List<ConsumableDTO>>(Input.Value);
                    foreach (var dto in DTOs)
                    {
                        var consumable = Consumables.FirstOrDefault(c => c.Uid == dto.Uid);
                        if (consumable is not null)
                        {
                            consumable.Quantity = dto.Quantity;
                            consumable.Value = dto.Value;
                            consumable.IsSelected = true;
                        }
                    }
                }
                catch(Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }

            NextDisabled = !IsValid();
            Initialized = true;
        }
    }
}