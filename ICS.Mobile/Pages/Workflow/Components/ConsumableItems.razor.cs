using ICS.Mobile.DataModels.Custom;
using ICS.Portal.Data.Queries.Models;

using Microsoft.AspNetCore.Components;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace ICS.Mobile.Pages.Workflow.Components
{
    public partial class ConsumableItems : WorkflowComponentsBasePage
    {
        #region Fields

        private List<LookupsResult.Attribute>? ConsumableAttributes = new();
        private List<ConsumableItem>? ConsumableItemsResults = new();
        private List<ConsumableItem>? ConsumableItemsAvailable = new();
        private List<string> ConsumableItemsList = new();
        private LookupsResult.Attribute? SelectedCategory = null;
        private Modes Mode = Modes.ExistingListWithAdd;
        private ConsumableItem? SelectedConsumableItem = null;

        #endregion Fields

        #region Parameters

        [Parameter]
        public List<LookupsResult.Attribute>? Attributes { set; get; }


        #endregion Parameters

        private void AddConsumableClick()
        {
            SelectedCategory = null;
            Mode = Modes.SelectingCategory;
            NextDisabled = true;
        }
        private void SetCategory(LookupsResult.Attribute attribute)
        {
            SelectedCategory = SelectedCategory?.Id == attribute.Id ? null : attribute;
            if (SelectedCategory != null)
            {
                BuildAvailableConsumableItems();
            }
            NextDisabled = SelectedCategory == null;
        }
        private void SetItem(ConsumableItem item)
        {
            SelectedConsumableItem = SelectedConsumableItem?.ConsumableId == item.ConsumableId ? null : item;
            NextDisabled = SelectedConsumableItem == null;
        }

        private string? _LastSelectedValue = null;
        private string? SelectedConsumableValue
        {
            // Added to handle DataPicker vs forever long list
            set
            {

                if (string.IsNullOrEmpty(value) || !value.Contains('~') || ConsumableItemsAvailable is null)
                {
                    NextDisabled = true;
                    SelectedConsumableItem = null;
                    _LastSelectedValue = null;
                    return;
                }

                var x = ConsumableItemsAvailable.Find(x => x.ConsumableId == value.Split('~')[1]);
                if (x != null)
                {
                    SelectedConsumableItem = x;
                    _LastSelectedValue = value;
                }
                else
                {
                    SelectedConsumableItem = null;
                    _LastSelectedValue = null;
                }

                NextDisabled = SelectedConsumableItem == null;
            }
            get
            {
                return _LastSelectedValue;
            }
        }

        private decimal? Quantity
        {
            set
            {
                SelectedConsumableItem!.Quantity = value;
                NextDisabled = SelectedConsumableItem!.Quantity.GetValueOrDefault(0) == 0;
            }
            get
            {
                return SelectedConsumableItem?.Quantity ?? null;
            }
        }
        private string? Notes
        {
            set
            {
                SelectedConsumableItem!.Notes = value;
            }
            get
            {
                return SelectedConsumableItem?.Notes ?? null;
            }
        }
        private void BuildAvailableConsumableItems()
        {
            if (ConsumableItemsAvailable is null)
                ConsumableItemsAvailable = new();

            ConsumableItemsAvailable?.Clear();
            ConsumableItemsList?.Clear();
            if (!string.IsNullOrEmpty(SelectedCategory!.OptionList))
            {
                string[] keyPairs = SelectedCategory.OptionList.Split(',');
                for (int idx = 0; idx != keyPairs.Length; idx++)
                {
                    string[] keyPair = keyPairs[idx].Split('~');
                    if (keyPair.Length == 2)
                    {
                        ConsumableItemsAvailable.Add(new ConsumableItem() { AttributeId = SelectedCategory.Id, ConsumableId = keyPair[1], Name = keyPair[0] });
                        ConsumableItemsList.Add(string.Concat(keyPair[0], "~", keyPair[1]));
                    }
                }
                
            }
        }
        private void AddSelectedConsumableItemToResult()
        {
            ConsumableItemsResults ??= new();

            ConsumableItemsResults.Add(new ConsumableItem()
            {
                AttributeId = SelectedConsumableItem!.AttributeId,
                ConsumableId = SelectedConsumableItem.ConsumableId,
                Name = SelectedConsumableItem.Name,
                Quantity = SelectedConsumableItem.Quantity,
                Notes = SelectedConsumableItem.Notes
            });

            SelectedCategory = null;
            SelectedConsumableItem = null;
        }
        private void DeleteConsumableItem(ConsumableItem item)
        {
            ConsumableItemsResults!.Remove(item);
            UpdateValue();
            NextDisabled = !IsValid();
        }
        public void UpdateValue()
        {
            string? value = null;
            if (ConsumableItemsResults?.Count != 0)
            {
                value = JsonSerializer.Serialize(ConsumableItemsResults);
            }
            if (Input!.Value != value)
            {
                Input.Value = value;
            }
        }
        protected override bool IsValid()
        {
            if (WorkflowStep!.IsRequired && string.IsNullOrEmpty(Input?.Value))
            {
                return false;
            }
            return base.IsValid();
        }

        #region Navigation

        private enum Modes
        {
            ExistingListWithAdd,
            SelectingCategory,
            SelectingItem,
            SettingQuantityAndNotes
        }
        private async Task Next()
        {
            switch (Mode)
            {
                case Modes.ExistingListWithAdd:
                    await MoveNext();
                    break;
                case Modes.SelectingCategory:
                    Mode = Modes.SelectingItem;
                    NextDisabled = SelectedConsumableItem == null;
                    break;
                case Modes.SelectingItem:
                    Mode = Modes.SettingQuantityAndNotes;
                    NextDisabled = Quantity.GetValueOrDefault(0) == 0;
                    break;
                case Modes.SettingQuantityAndNotes:
                    AddSelectedConsumableItemToResult();
                    UpdateValue();
                    Mode = Modes.ExistingListWithAdd;
                    NextDisabled = !IsValid();
                    break;
            }
        }
        private async Task Previous()
        {
            switch (Mode)
            {
                case Modes.ExistingListWithAdd:
                    await MovePrevious();
                    break;
                case Modes.SelectingCategory:
                    SelectedCategory = null;
                    SelectedConsumableItem = null;
                    Mode = Modes.ExistingListWithAdd;
                    NextDisabled = !IsValid();
                    break;
                case Modes.SelectingItem:
                    NextDisabled = SelectedCategory == null;
                    Mode = Modes.SelectingCategory;
                    break;
                case Modes.SettingQuantityAndNotes:
                    NextDisabled = SelectedConsumableItem == null;
                    Mode = Modes.SelectingItem;
                    break;
            }
        }

        #endregion Navigation

        protected override void OnParametersSet()
        {
            base.OnParametersSet();

            if (!string.IsNullOrEmpty(Input!.Value))
            {
                ConsumableItemsResults = JsonSerializer.Deserialize<List<ConsumableItem>>(Input.Value);
            }
            else
            {
                ConsumableItemsResults = new();
            }

            ConsumableAttributes = new();

            if (Attributes is not null)
            {
                foreach (var attribute in Attributes)
                {
                    if (attribute.IsConsumable)
                    {
                        ConsumableAttributes!.Add(attribute);
                    }
                }
            }

            Initialized = true;
            NextDisabled = !IsValid();
            PreviousDisabled = false;

        }
    }
}
