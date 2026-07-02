using ICS.Portal.Data.Custom;
using ICS.Portal.Data.Queries.Models;

using Microsoft.AspNetCore.Components;

using System.Text.Json;

namespace ICS.Mobile.Pages.Workflow.Components
{
    public partial class TruckStockItems
    {
        #region Fields

        private List<TruckStockItem>? TruckStockItemsAvailable;
        private List<string>? TruckStockItemsList;
        private List<TruckStockItem>? TruckStockItemsResults = new();
        private LookupsResult.Attribute? SelectedCategory = null;
        private Modes Mode = Modes.ExistingListWithAdd;
        private TruckStockItem? SelectedTruckStockItem = null;

        #endregion Fields

        #region Parameters

        [Parameter]
        public List<LookupsResult.Attribute>? Items { set; get; }

        #endregion Parameters

        private void AddTruckStopItemClick()
        {
            Mode = Modes.SelectingCategory;
            NextDisabled = true;
        }
        private void SetCategory(LookupsResult.Attribute category)
        {
            SelectedCategory = SelectedCategory?.Id == category.Id ? null : category;
            if (SelectedCategory != null)
            {
                BuildAvailableTruckStopItems();
            }
            NextDisabled = SelectedCategory == null;
        }
        private void SetItem(TruckStockItem item)
        {
            SelectedTruckStockItem = SelectedTruckStockItem?.Id == item.Id ? null : item;
            NextDisabled = SelectedTruckStockItem == null;
        }

        private string? _LastSelectedValue = null;
        private string? SelectedTrkStkValue
        {
            // Added to handle DataPicker vs forever long list
            set
            {

                if (string.IsNullOrEmpty(value) || !value.Contains('~') || TruckStockItemsAvailable is null)
                {
                    NextDisabled = true;
                    SelectedTruckStockItem = null;
                    _LastSelectedValue = null;
                    return;
                }
                
                string label = value.Split('~')[0];

                var x = TruckStockItemsAvailable.Find(x => x.Id == value.Split('~')[1]);
                if (x != null)
                {
                    SelectedTruckStockItem = x;
                    _LastSelectedValue = value;
                }
                else
                {
                    SelectedTruckStockItem = null;
                    _LastSelectedValue = null;
                }

                NextDisabled = SelectedTruckStockItem == null;
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
                SelectedTruckStockItem!.Quantity = value;
                NextDisabled = SelectedTruckStockItem.Quantity.GetValueOrDefault(0) == 0;
            }
            get
            {
                return SelectedTruckStockItem!.Quantity;
            }
        }
        private string? Notes
        {
            set
            {
                SelectedTruckStockItem!.Notes = value;
            }
            get
            {
                return SelectedTruckStockItem!.Notes;
            }
        }
        private void BuildAvailableTruckStopItems()
        {
            TruckStockItemsAvailable = new();
            TruckStockItemsList = new();
            if (!string.IsNullOrEmpty(SelectedCategory!.OptionList))
            {
                string[] keyPairs = SelectedCategory.OptionList.Split(',');
                for (int idx = 0; idx != keyPairs.Length; idx++)
                {
                    string[] keyPair = keyPairs[idx].Split('~');
                    if (keyPair.Length == 2)
                    {
                        TruckStockItemsAvailable.Add(new TruckStockItem() { Id = keyPair[1], Name = keyPair[0] });
                        TruckStockItemsList.Add(string.Concat(keyPair[0], "~", keyPair[1]));
                    }
                }
            }
        }
        private void AddSelectedConsumableToResult()
        {
            TruckStockItemsResults ??= new();

            TruckStockItemsResults.Add(new TruckStockItem()
            {
                Id = SelectedTruckStockItem!.Id,
                Name = SelectedTruckStockItem.Name,
                Quantity = SelectedTruckStockItem.Quantity,
                Notes = SelectedTruckStockItem.Notes
            });
            SelectedCategory = null;
            SelectedTruckStockItem = null;
        }
        private void DeleteTruckStockItem(TruckStockItem item)
        {
            TruckStockItemsResults!.Remove(item);
            UpdateValue();
            NextDisabled = !IsValid();
        }
        public void UpdateValue()
        {
            string? value = null;
            if (TruckStockItemsResults?.Count != 0)
            {
                value = JsonSerializer.Serialize(TruckStockItemsResults);
            }
            if (Input!.Value != value)
            {
                Input.Value = value;
            }
        }

        #region Navigation

        public enum Modes
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
                    NextDisabled = SelectedTruckStockItem == null;
                    break;
                case Modes.SelectingItem:
                    Mode = Modes.SettingQuantityAndNotes;
                    NextDisabled = Quantity.GetValueOrDefault(0) == 0;
                    break;
                case Modes.SettingQuantityAndNotes:
                    AddSelectedConsumableToResult();
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
                    SelectedTruckStockItem = null;
                    Mode = Modes.ExistingListWithAdd;
                    NextDisabled = !IsValid();
                    break;
                case Modes.SelectingItem:
                    NextDisabled = SelectedCategory == null;
                    Mode = Modes.SelectingCategory;
                    break;
                case Modes.SettingQuantityAndNotes:
                    NextDisabled = SelectedTruckStockItem == null;
                    Mode = Modes.SelectingItem;
                    break;
            }
        }

        #endregion Navigation
  
        protected override bool IsValid()
        {
            if (WorkflowStep!.IsRequired && string.IsNullOrEmpty(Input?.Value))
            {
                return false;
            }
            return base.IsValid();
        }       
        protected override void OnParametersSet()
        {
            base.OnParametersSet();

            if (!string.IsNullOrEmpty(Input!.Value))
            {
                TruckStockItemsResults = JsonSerializer.Deserialize<List<TruckStockItem>>(Input.Value);
            }
            else
            {
                TruckStockItemsResults = new();
            }

            if (Items is not null)
            {
                Items = Items.Where(x => !string.IsNullOrEmpty(x.OptionList)).ToList();
            }
            

            Initialized = true;
            NextDisabled = !IsValid();
            PreviousDisabled = false;
        }
    }
}