using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Threading.Tasks;
using ICS.Mobile.Components;
using ICS.Mobile.Services;
using ICS.Portal.Data.Custom;
using ICS.Portal.Data.Images;
using ICS.Portal.Data.Queries.Models;
//using ICS.Portal.Web.Helpers;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;

using Newtonsoft.Json.Linq;

namespace ICS.Mobile.Pages.Workflow.Components
{
    public partial class Equipment
    {
        #region Injections
        [Inject]
        protected IJSRuntime JSRuntime { set; get; } = default!;

        [Inject]
        protected SettingsService SettingsService { set; get; } = default!;

        [Parameter]
        public WorkflowRunnerLogic? runner { set; get; }
        #endregion

        #region Private Variables and Properties

        private Mobile.Components.ICSDialogBox DialogBox;
        private DataPicker? mfgPicker;
        private BluonAgeAndWarranty? bluonDecoder;
        private BluonNameplateReader? bluonNameplateReader;
        private bool bUseBluon = true; // set to FALSE to disable bluon features by default, and set API key to "" in settings
        private bool bTriedBluon = false; // per-use/session flag
        private string? _EquipmentManufacturerTemp = null;
        private string? EquipmentManufacturerTemp
        {
            set
            {
                if (value is null)
                {
                    _EquipmentManufacturerTemp = null;
                }
                else
                {
                    if (value.Length > 20)
                    {
                        value = value.Substring(0, 20);
                    }
                    if (mfgPicker is not null)
                    {
                        // Try to select - returns true if found, false if not
                        bool? found = mfgPicker.SelectIfFound(value);
                        if (found is null) found = false;

                        if (found.GetValueOrDefault(true))
                        {
                            value = null;
                        }
                    }
                    _EquipmentManufacturerTemp = value;
                }
            }
            get { return _EquipmentManufacturerTemp; }
        }


        

        /// <summary>
        /// Prevent add button if allow add is false
        /// </summary>
        private bool AllowAdd = false;

        /// <summary>
        /// Currently Adding a new piece of equipment.
        /// </summary>
        private bool AddInProgress = false;

        /// <summary>
        /// Prevent edit button if allow edit is false
        /// </summary>
        private bool AllowEdit = false;

        /// <summary>
        /// Skip attributes on edit and displays
        /// </summary>
        private bool IgnoreAttributes = false;

        private bool SingleSelection = false;

        private WFEquipmentModes mode = WFEquipmentModes.Selecting;

        /// <summary>
        /// Controls which panel is currently displayed
        /// </summary>
        private WFEquipmentModes Mode
        {
            set
            {
                mode = value;
                if (mode == WFEquipmentModes.Consumables)
                {
                    PreviousDisabled = false;
                }
            }
            get
            {
                return mode;
            }
        }

        private WFEditModes editMode = WFEditModes.None;

        /// <summary>
        /// Controls which panel is currently displayed
        /// </summary>
        private WFEditModes EditMode
        {
            set
            {
                editMode = value;
                if (editMode == WFEditModes.OpenConsumable)
                {
                    PreviousDisabled = false;
                }
            }
            get
            {
                return editMode;
            }
        }

        /// <summary>
        /// Keeps track of the last missing mode - facilitates missing values play through.
        /// </summary>
        private WFEquipmentModes PreviousWFEquipmentMode = WFEquipmentModes.EquipmentType;

        /// <summary>
        /// Provides attributes
        /// </summary>
        private LookupsResult? Lookups = null;

        /// <summary>
        /// List of equipment
        /// </summary>
        private List<WorkflowEquipment> EquipmentList = new();

        /// <summary>
        /// Selected or Active equipment
        /// </summary>
        private WorkflowEquipment? SelectedEquipment = null;

        /// <summary>
        /// Current selected attribute from within the SelectedEquipment.Attributes
        /// </summary>
        private EquipmentAttribute? SelectedAttribute = null;

        /// <summary>
        /// Used to track position of SelectedEquipment.Attributes.
        /// </summary>
        private int SelectedAttributeIndex = 0;

        /// <summary>
        /// Current selected consumable from within the Equipment.Consumables
        /// </summary>
        private EquipmentAttribute? SelectedConsumable = null;

        /// <summary>
        /// List of available groups to the equipment group property
        /// </summary>
        private List<string> GroupList
        {
            get
            {
                return WorkflowEquipmentBuilder.GetEquipmentGroupList(EquipmentList);
            }
        }

        /// <summary>
        /// Current Selected Equipment Group
        /// </summary>
        private string? SelectedGroup = null;

        /// <summary>
        /// For capturing the unit 1 image - this value is saved on the equipment UnitImage1FileName
        /// </summary>
        private ImageInsertInput? UnitImage1 = null;

        /// <summary>
        /// For capturing the unit 2 image - this value is saved on the equipment UnitImage2FileName
        /// </summary>
        private ImageInsertInput? UnitImage2 = null;

        /// <summary>
        /// For capturing the data plate - this value is saved on the equipment DataPlateFileName
        /// </summary>
        private ImageInsertInput? DataPlateImage = null;


        /// <summary>
        /// For dataPlate raw text back from AI Vision
        /// </summary>
        private string? DataPlateText = null;

        /// <summary>
        /// For dataPlate text as an IENUMERABLE list that can be used across steps
        /// </summary>
        public List<string> DataPlateList = new List<string>(); // defaults to not null

        private List<string> DataPlateListNumericsOnly
        {
            get
            {
                List<string> newlist = DataPlateList.Where(line => GeneralFunctions.HasNumerics(line)).ToList();
                return newlist;
            }
        }


        private bool RetiredGroupPresent
        {
            get
            {
                return EquipmentList.Any(e => e.IsRetired);
            }
        }

        private bool IsDirty = false;


        private List<string> GroupListCopy
        {
            get
            {
                return GroupList.ToArray().ToList();
            }
        }

        #endregion

        #region Multi-Select, Filter, and Sort Properties

        /// <summary>
        /// Enables multi-select mode for bulk operations & if enabled remember next/prev state
        /// </summary>
        private bool EquipmentFilterMode = true;
        private bool MultiSelectMode = false;
        private bool MultiNextDisabledState = true;
        private bool MultiPrevDisabledState = true;

        /// <summary>
        /// Collection of equipment selected for bulk operations
        /// </summary>
        private HashSet<WorkflowEquipment> MultiSelectedEquipment = new();

        /// <summary>
        /// Search/filter text for filtering equipment list
        /// </summary>
        private string SearchFilterText = string.Empty;

        /// <summary>
        /// Sort mode enumeration
        /// </summary>
        private enum EquipmentSortMode
        {
            UnitTag = 0,
            UnitTagDesc = 1,
            Manufacturer = 2,
            ManufacturerDesc = 3,
        }

        /// <summary>
        /// Current sort mode
        /// </summary>
        private EquipmentSortMode CurrentSortMode = EquipmentSortMode.Manufacturer;

        /// <summary>
        /// Controls visibility of bulk actions popup
        /// </summary>
        private bool ShowBulkActionsPopup = false;

        /// <summary>
        /// Controls visibility of Move to Group popup
        /// </summary>
        private bool ShowMoveToGroupPopup = false;

        /// <summary>
        /// New group name input for creating a group during move
        /// </summary>
        private string NewGroupName = string.Empty;

        #endregion

        #region Filtered and Sorted Equipment

        /// <summary>
        /// Returns equipment filtered by search text
        /// </summary>
        private IEnumerable<WorkflowEquipment> FilteredEquipmentList
        {
            get
            {
                if (string.IsNullOrWhiteSpace(SearchFilterText))
                    return EquipmentList;

                var searchLower = SearchFilterText.ToLowerInvariant().Trim();
                return EquipmentList.Where(e =>
                    (!string.IsNullOrEmpty(e.Make) && e.Make.ToLowerInvariant().Contains(searchLower)) ||
                    (!string.IsNullOrEmpty(e.Model) && e.Model.ToLowerInvariant().Contains(searchLower)) ||
                    (!string.IsNullOrEmpty(e.Serial) && e.Serial.ToLowerInvariant().Contains(searchLower)) ||
                    (!string.IsNullOrEmpty(e.UnitTag) && e.UnitTag.ToLowerInvariant().Contains(searchLower))
                );
            }
        }

        /// <summary>
        /// Returns sorted equipment for a specific group
        /// </summary>
        private IEnumerable<WorkflowEquipment> GetSortedEquipmentForGroup(string? group, bool retiredOnly = false)
        {
            IEnumerable<WorkflowEquipment> filtered;

            if (retiredOnly)
            {
                filtered = FilteredEquipmentList.Where(e => e.IsRetired);
            }
            else if (group == null)
            {
                filtered = FilteredEquipmentList.Where(e => e.EquipmentGroup == null && e.IsRetired == false);
            }
            else
            {
                filtered = FilteredEquipmentList.Where(e => e.EquipmentGroup == group && e.IsRetired == false);
            }

            return SortEquipment(filtered);
        }

        /// <summary>
        /// Sorts equipment based on current sort mode
        /// </summary>
        private IEnumerable<WorkflowEquipment> SortEquipment(IEnumerable<WorkflowEquipment> equipment)
        {

            if (CurrentSortMode == EquipmentSortMode.UnitTag)
            {
                return equipment.OrderBy(e => e.UnitTag ?? "zzz") // nulls sort last
                               .ThenBy(e => e.Make ?? "")
                               .ThenBy(e => e.Model ?? "")
                               .ThenBy(e => e.Serial ?? "");
            }

            if (CurrentSortMode == EquipmentSortMode.UnitTagDesc)
            {
                return equipment.OrderByDescending(e => e.UnitTag ?? "aaa") // nulls sort first
                               .ThenByDescending(e => e.Make ?? "")
                               .ThenByDescending(e => e.Model ?? "")
                               .ThenByDescending(e => e.Serial ?? "");
            }

            if (CurrentSortMode == EquipmentSortMode.Manufacturer)
            {
                return equipment.OrderBy(e => e.Make ?? "zzz")
                               .ThenBy(e => e.UnitTag ?? "")
                               .ThenBy(e => e.Model ?? "")
                               .ThenBy(e => e.Serial ?? "");
            }

            if (CurrentSortMode == EquipmentSortMode.ManufacturerDesc)
            {
                return equipment.OrderByDescending(e => e.Make ?? "zzz")
                               .ThenByDescending(e => e.UnitTag ?? "")
                               .ThenByDescending(e => e.Model ?? "")
                               .ThenByDescending(e => e.Serial ?? "");
            }

            // else default to UnitTag ascending

            return equipment.OrderBy(e => e.UnitTag ?? "zzz") // nulls sort last
                               .ThenBy(e => e.Make ?? "")
                               .ThenBy(e => e.Model ?? "")
                               .ThenBy(e => e.Serial ?? "");

        }

        /// <summary>
        /// Gets the filtered count for a group
        /// </summary>
        private int FilteredGroupCount(string group)
        {
            if (group == "Retired")
            {
                return FilteredEquipmentList.Count(e => e.IsRetired);
            }
            return FilteredEquipmentList.Count(e => e.EquipmentGroup == group && e.IsRetired == false);
        }

        #endregion

        #region Multi-Select Methods

        /// <summary>
        /// Toggle multi-select mode on/off
        /// </summary>
        private void ToggleMultiSelectMode()
        {
            MultiSelectMode = !MultiSelectMode;
            if (!MultiSelectMode)
            {
                // Clear selections when exiting multi-select mode
                MultiSelectedEquipment.Clear();
                ShowBulkActionsPopup = false;
                ShowMoveToGroupPopup = false;
                NextDisabled = MultiNextDisabledState; // remember what it was before we went into multistep mode
                PreviousDisabled = MultiPrevDisabledState; // remember what it was before we went into multistep mode
            }
            else
            {
                MultiNextDisabledState = NextDisabled;
                MultiPrevDisabledState = PreviousDisabled;
                NextDisabled = true;
                PreviousDisabled = true;
            }
        }

        /// <summary>
        /// Toggle selection of a single equipment item in multi-select mode
        /// </summary>
        private void ToggleMultiSelect(WorkflowEquipment equipment)
        {
            if (MultiSelectedEquipment.Contains(equipment))
            {
                MultiSelectedEquipment.Remove(equipment);
            }
            else
            {
                MultiSelectedEquipment.Add(equipment);
            }
        }

        /// <summary>
        /// Check if equipment is multi-selected
        /// </summary>
        private bool IsMultiSelected(WorkflowEquipment equipment)
        {
            return MultiSelectedEquipment.Contains(equipment);
        }

        /// <summary>
        /// Get CSS class for multi-select state
        /// </summary>
        private string MultiSelectStyle(WorkflowEquipment equipment)
        {
            return IsMultiSelected(equipment) ? "MultiSelected" : "";
        }

        /// <summary>
        /// Select all visible (filtered) equipment
        /// </summary>
        private void SelectAllVisible()
        {
            foreach (var eq in FilteredEquipmentList.Where(e => !e.IsRetired))
            {
                MultiSelectedEquipment.Add(eq);
            }
        }

        /// <summary>
        /// Deselect all equipment in Multiselect Mode
        /// </summary>
        private void DeselectAll()
        {
            MultiSelectedEquipment.Clear();
        }

        /// <summary>
        /// Show bulk actions popup
        /// </summary>
        private void ShowBulkActions()
        {
            if (MultiSelectedEquipment.Count > 0)
            {
                ShowBulkActionsPopup = true;
            }
        }

        /// <summary>
        /// Close all popups
        /// </summary>
        private void ClosePopups()
        {
            ShowBulkActionsPopup = false;
            ShowMoveToGroupPopup = false;
            NewGroupName = string.Empty;
        }

        private void ToggleEquipmentFilter()
        {
            if (MultiSelectMode)
                EquipmentFilterMode = true;
            else
                EquipmentFilterMode = !EquipmentFilterMode;
        }

        #endregion

        #region Bulk Action Methods

        /// <summary>
        /// Retire all selected equipment
        /// </summary>
        private async Task BulkRetire()
        {
            if (MultiSelectedEquipment.Count == 0) return;

            // add a little retirement humor
            var retirementMessages = new[] { "Celebrate Retirement", "Cant wait for Retirement", "Jealous of retiring", "Retirement is the goal" };
            var randomRetirementMessage = retirementMessages[new Random().Next(retirementMessages.Length)];

            ShowBulkActionsPopup = false;
            ShowMoveToGroupPopup = false;
            bool confirmed = await Confirm(
                $"Retire {MultiSelectedEquipment.Count} selected equipment units(s)?", randomRetirementMessage);

            if (confirmed)
            {
                await BulkTakeAction(1, null);
            }


        }

        /// <summary>
        /// Un-retire all selected equipment
        /// </summary>
        private async Task BulkUnRetire()
        {
            if (MultiSelectedEquipment.Count == 0) return;

            ShowBulkActionsPopup = false;
            ShowMoveToGroupPopup = false;
            bool confirmed = await Confirm(
                $"Un-Retire {MultiSelectedEquipment.Count} selected equipment units(s)?",
                "Confirm Un-Retire"
            );

            if (confirmed)
            {
                await BulkTakeAction(2, null);
            }

        }

        /// <summary>
        /// Show the move to group selection popup
        /// </summary>
        private void ShowMoveToGroup()
        {
            ShowBulkActionsPopup = false;
            ShowMoveToGroupPopup = true;
        }

        /// <summary>
        /// Move selected equipment to an existing group
        /// </summary>
        private async Task MoveToGroup(string? toGroupName)
        {
            ShowBulkActionsPopup = false;
            ShowMoveToGroupPopup = false;

            if (MultiSelectedEquipment.Count == 0 || string.IsNullOrWhiteSpace(toGroupName))
                return;

            bool confirmed = await Confirm(
                $"Move {MultiSelectedEquipment.Count} selected equipment units(s)?",
                $"Move to {toGroupName.Left(10)}"
            );

            if (confirmed)
            {
                await BulkTakeAction(3, toGroupName);
            }
        }

        private async Task BulkTakeAction(int actionToTake = 0, string? groupName = "")
        {
            if (MultiSelectedEquipment.Count == 0
                || (string.IsNullOrWhiteSpace(groupName) && actionToTake == 3)
                || (actionToTake < 1 || actionToTake > 5)
                ) return;

            IsDirty = true;

            foreach (var equipment in MultiSelectedEquipment)
            {
                equipment.IsDirty = true;

                // Get the item from the main clone list serving primary equipment control list
                var reqitem = EquipmentList.Find(e =>
                    (e.Id > 0 && equipment.Id > 0 && e.Id == equipment.Id) ||
                    (e.TemporaryId.HasValue && equipment.TemporaryId.HasValue && e.TemporaryId == equipment.TemporaryId)
                );

                if (reqitem is not null)
                {
                    IsDirty = true;
                    reqitem.IsDirty = true;
                    reqitem.ModifiedDate = DateTime.UtcNow;
                    reqitem.ModifiedBy = DataService.AppState.AuthorizedUser!.Id;

                    // is this also the currently selected item? If so, update that too
                    if (SelectedEquipment is not null &&
                        (
                            (SelectedEquipment.Id > 0 && equipment.Id > 0 && SelectedEquipment.Id == equipment.Id) ||
                            (SelectedEquipment.TemporaryId.HasValue && equipment.TemporaryId.HasValue && SelectedEquipment.TemporaryId == equipment.TemporaryId)
                        ))
                    {
                        SelectedEquipment!.IsDirty = reqitem.IsDirty;
                        SelectedEquipment!.ModifiedDate = reqitem.ModifiedDate;
                        SelectedEquipment!.ModifiedBy = reqitem.ModifiedBy;
                    }

                    // Now get the real item in the actual data and fix that and mark as DIRTY

                    var realitem = runner!.Dispatch!.GetEquipmentById(equipment.Id, equipment.TemporaryId);
                    if (realitem is not null)
                    {
                        realitem.IsDirty = true;
                        realitem.ModifiedDate = DateTime.UtcNow;
                        realitem.ModifiedBy = DataService.AppState.AuthorizedUser!.Id;

                        switch (actionToTake)
                        {

                            case 1: // Retire
                                equipment.IsRetired = true;
                                reqitem.IsRetired = true;
                                realitem.IsRetired = true;
                                if (SelectedEquipment is not null &&
                        (
                            (SelectedEquipment.Id > 0 && realitem.Id > 0 && SelectedEquipment.Id == realitem.Id) ||
                            (SelectedEquipment.TemporaryId.HasValue && realitem.TemporaryId.HasValue && SelectedEquipment.TemporaryId == realitem.TemporaryId)
                        ))
                                    SelectedEquipment!.IsRetired = true;
                                break;
                            case 2: // Un-Retire
                                equipment.IsRetired = false;
                                reqitem.IsRetired = false;
                                realitem.IsRetired = false;
                                if (SelectedEquipment is not null &&
                        (
                            (SelectedEquipment.Id > 0 && realitem.Id > 0 && SelectedEquipment.Id == realitem.Id) ||
                            (SelectedEquipment.TemporaryId.HasValue && realitem.TemporaryId.HasValue && SelectedEquipment.TemporaryId == realitem.TemporaryId)
                        ))
                                    SelectedEquipment!.IsRetired = false;
                                break;

                            case 3: // Move to Group
                                equipment.EquipmentGroup = groupName;
                                reqitem.EquipmentGroup = groupName;
                                realitem.EquipmentGroup = groupName;
                                if (SelectedEquipment is not null &&
                        (
                            (SelectedEquipment.Id > 0 && realitem.Id > 0 && SelectedEquipment.Id == realitem.Id) ||
                            (SelectedEquipment.TemporaryId.HasValue && realitem.TemporaryId.HasValue && SelectedEquipment.TemporaryId == realitem.TemporaryId)
                        ))
                                    SelectedEquipment!.EquipmentGroup = groupName;
                                break;

                            case 4: // Remove from Group
                                equipment.EquipmentGroup = null;
                                reqitem.EquipmentGroup = null;
                                realitem.EquipmentGroup = null;
                                if (SelectedEquipment is not null &&
                        (
                            (SelectedEquipment.Id > 0 && realitem.Id > 0 && SelectedEquipment.Id == realitem.Id) ||
                            (SelectedEquipment.TemporaryId.HasValue && realitem.TemporaryId.HasValue && SelectedEquipment.TemporaryId == realitem.TemporaryId)
                        ))
                                    SelectedEquipment!.EquipmentGroup = null;
                                break;

                            case 5:
                                // to the future!
                                // and beyond
                                break;
                        }

                        // Take an indexdb dump

                        await runner!.SaveDispatchAsync();

                    }

                }
            }

            MultiSelectedEquipment.Clear();
            ClosePopups();
            ToggleMultiSelectMode();
            StateHasChanged();
        }

        /// <summary>
        /// Create a new group and move selected equipment to it
        /// </summary>
        private async Task CreateAndMoveToNewGroup()
        {
            if (string.IsNullOrWhiteSpace(NewGroupName))
            {
                ErrorMessage = "Please enter a group name";
                return;
            }

            if (NewGroupName.Length > 36)
            {
                ErrorMessage = "Group name cannot exceed 36 characters";
                return;
            }

            ErrorMessage = null;

            // Add the new group
            TryAddEquipmentGroup(NewGroupName);

            // Move equipment to the new group
            await BulkTakeAction(3, NewGroupName);
        }

        /// <summary>
        /// Remove selected equipment from any group (set to null/ungrouped)
        /// </summary>
        private async Task RemoveFromGroup()
        {
            if (MultiSelectedEquipment.Count == 0) return;

            ShowBulkActionsPopup = false;
            ShowMoveToGroupPopup = false;

            bool confirmed = await Confirm(
                $"Remove {MultiSelectedEquipment.Count} selected unit(s) from their group?  The will appear at the top of the equipment list.",
                "Confirm Un-Grouping"
            );

            if (confirmed)
            {
                await BulkTakeAction(4, null);
            }
            else
            {
                ShowMoveToGroupPopup = true;
            }
        }

        private void ToggleSortMode()
        {
            // Loop through 0-3, wrap around
            CurrentSortMode = (EquipmentSortMode)(((int)CurrentSortMode + 1) % 4);
        }

        /// <summary>
        /// Set specific sort mode
        /// </summary>
        private void SetSortMode(EquipmentSortMode mode)
        {
            CurrentSortMode = mode;
        }

        /// <summary>
        /// Get sort mode display text
        /// </summary>
        private string SortModeText => (CurrentSortMode == EquipmentSortMode.Manufacturer || CurrentSortMode == EquipmentSortMode.ManufacturerDesc)
            ? "Mfg/Model"
            : "Unit/Tag";

        #endregion

        #region Filter Methods

        /// <summary>
        /// Clear the search filter
        /// </summary>
        private void ClearSearchFilter()
        {
            SearchFilterText = string.Empty;
        }

        /// <summary>
        /// Handle search input changes
        /// </summary>
        private void OnSearchFilterChanged(ChangeEventArgs e)
        {
            SearchFilterText = e.Value?.ToString() ?? string.Empty;
        }

        #endregion

        #region Equipment Property Wrappers

        /// <summary>
        /// Tries to set the EquipmentTypeId based on the TypeName
        /// </summary>
        private void TrySetEquipmentType()
        {
            var equipmentType = Lookups!.EquipmentTypeResult!.FirstOrDefault(e => e.TypeName == SelectedEquipment!.TypeName);
            if (equipmentType is null)
            {
                SelectedEquipment!.EquipmentTypeId = -1;
            }
            else
            {
                if (SelectedEquipment!.EquipmentTypeId != equipmentType.Id)
                {
                    SelectedEquipment!.EquipmentTypeId = equipmentType.Id;
                    SelectedEquipment!.SetEquipmentTypeAttributes(Lookups.AttributeResult, Lookups.EquipmentTypeAttributeResult, runner.Dispatch);
                }
            }
        }

        /// <summary>
        /// Gets the list of manufacturers consumed by DataLIstInput.
        /// </summary>
        private List<string>? EquipmentManufacturerList
        {
            get
            {
                if (Lookups is not null && Lookups.EquipmentManufacturerResult is not null)
                {
                    return Lookups.EquipmentManufacturerResult.Select(s => s.Name).ToList();
                }
                return null;
            }
        }

        /// <summary>
        /// Gets the list of equipment type names consumed by DataLIstInput.
        /// </summary>
        private List<string>? EquipmentTypeList
        {
            get
            {
                if (Lookups is not null && Lookups.EquipmentTypeResult is not null)
                {
                    return Lookups.EquipmentTypeResult.Select(s => s.TypeName).ToList();
                }
                return null;
            }
        }

        private List<string>? EquipmentLocations
        {
            get
            {
                if (Lookups is not null && Lookups.EquipmentLocationResult is not null)
                {
                    return Lookups.EquipmentLocationResult!.Select(s => s.Location).ToList();
                }
                return new();
            }
        }

        /// <summary>
        /// Wraps SelectedEquipment.TypeName
        /// </summary>
        private string EquipmentType
        {
            set
            {
                if (SelectedEquipment!.TypeName != value)
                {
                    SelectedEquipment.TypeName = value;
                    SelectedEquipment.IsDirty = true;
                    IsDirty = true;
                }

                TrySetEquipmentType();

                NextDisabled = SelectedEquipment.EquipmentTypeId == -1;
            }
            get
            {
                if (SelectedEquipment is not null)
                    return SelectedEquipment!.TypeName;
                else
                    return string.Empty;
            }
        }

        /// <summary>
        /// Wraps SelectedEquipment.Make;
        /// </summary>
        private string? EquipmentManufacturer
        {
            set
            {
                if (value is not null && value.Length > 20)
                {
                    ErrorMessage = "Manufacturer length cannot exceed 20 characters";
                    NextDisabled = true;
                    return;
                }

                ErrorMessage = null;

                if (SelectedEquipment!.Make != value)
                {
                    SelectedEquipment.Make = value;
                    SelectedEquipment.IsDirty = true;
                    IsDirty = true;
                    EquipmentManufacturerTemp = null;

                }

                NextDisabled = string.IsNullOrEmpty(SelectedEquipment!.Make);
            }
            get
            {
                if (SelectedEquipment is not null)
                    return SelectedEquipment!.Make;
                else
                    return string.Empty;
            }
        }

        /// <summary>
        /// Wraps SelectedEquipment.UnitTag;
        /// </summary>
        private string? UnitTag
        {
            set
            {
                if (value is not null && value.Length > 15)
                {
                    ErrorMessage = "Unit Tag length cannot exceed 15 characters";
                    NextDisabled = true;
                    return;
                }

                ErrorMessage = null;

                if (SelectedEquipment!.UnitTag != value)
                {
                    SelectedEquipment.UnitTag = value;
                    SelectedEquipment.IsDirty = true;
                    IsDirty = true;
                }

                NextDisabled = string.IsNullOrEmpty(SelectedEquipment.UnitTag);
            }
            get
            {
                if (SelectedEquipment is not null)
                    return SelectedEquipment!.UnitTag;
                else
                    return string.Empty;
            }
        }

        /// <summary>
        /// Wraps SelectedEquipment.EquipLocArea;
        /// </summary>
        private string? EquipmentLocation
        {
            set
            {
                if (value is not null && value.Length > 10)
                {
                    ErrorMessage = "Equipment Location length cannot exceed 10 characters";
                    NextDisabled = true;
                    return;
                }

                ErrorMessage = null;

                if (SelectedEquipment!.EquipLocArea != value)
                {
                    SelectedEquipment.EquipLocArea = value;
                    SelectedEquipment.IsDirty = true;
                    IsDirty = true;
                }

                NextDisabled = string.IsNullOrEmpty(SelectedEquipment.EquipLocArea);
            }
            get
            {
                if (SelectedEquipment is not null)
                    return SelectedEquipment!.EquipLocArea;
                else
                    return string.Empty;
            }
        }

        /// <summary>
        /// Wraps SelectedEquipment.Model
        /// </summary>
        private string? EquipmentModel
        {
            set
            {
                if (value is not null && value.Length > 30)
                {
                    ErrorMessage = "Model length cannot exceed 30 characters";
                    NextDisabled = true;
                    return;
                }

                ErrorMessage = null;

                if (SelectedEquipment!.Model != value)
                {
                    SelectedEquipment!.Model = value;
                    SelectedEquipment.IsDirty = true;
                    IsDirty = true;

                }

                NextDisabled = string.IsNullOrEmpty(SelectedEquipment!.Model);
            }
            get
            {
                if (SelectedEquipment is not null)
                    return SelectedEquipment!.Model;
                else
                    return string.Empty;
            }
        }
        //public void SetMfgManuallyEdited(int x)
        //{
        //    if (x == 1)
        //    {
        //        EquipmentManufacturer = EquipmentManufacturerTemp;
        //        if (EquipmentManufacturer == EquipmentManufacturerTemp)
        //        {
        //            EquipmentManufacturerTemp = null;
        //        }
        //    }
        //}

        public async Task SetMfgManuallyEdited(int x)
        {
            if (x == 1 && mfgPicker is not null)
            {
                // Try to select - returns true if found, false if not
                bool found = mfgPicker.SelectIfFound(EquipmentManufacturerTemp);

                if (found)
                {
                    EquipmentManufacturerTemp = null;
                }
            }
        }

        // <summary>
        /// Wraps SelectedEquipment.Serial
        /// </summary>
        /// 

        private string? EquipmentSerial
        {
            set
            {
                if (value is not null && value.Length > 36)
                {
                    ErrorMessage = "Serial Number length cannot exceed 36 characters";
                    NextDisabled = true;
                    return;
                }

                ErrorMessage = null;

                if (SelectedEquipment!.Serial != value)
                {
                    SelectedEquipment.Serial = value;
                    SelectedEquipment.IsDirty = true;
                    IsDirty = true;
                }

                NextDisabled = string.IsNullOrEmpty(SelectedEquipment!.Serial);
            }
            get
            {
                if (SelectedEquipment is not null)
                    return SelectedEquipment!.Serial;
                else
                    return string.Empty;
            }
        }

        /// <summary>
        /// Wraps SelectedEquipment.EquipmentGroup
        /// </summary>
        private string EquipmentGroup
        {
            set
            {
                if (value is not null && value.Length > 36)
                {
                    ErrorMessage = "Equipment Group length cannot exceed 36 characters";
                    NextDisabled = true;
                    return;
                }

                ErrorMessage = null;

                if (SelectedEquipment!.EquipmentGroup != value)
                {
                    TryAddEquipmentGroup(SelectedEquipment.EquipmentGroup);

                    SelectedEquipment.EquipmentGroup = value;
                    SelectedEquipment.IsDirty = true;
                    IsDirty = true;
                }

                NextDisabled = false;
            }
            get
            {
                if (SelectedEquipment is not null)
                    return SelectedEquipment!.EquipmentGroup;
                else
                    return string.Empty;
            }
        }

        /// <summary>
        /// Wraps SelectedEquipment.IsRetired
        /// </summary>
        private bool IsRetired
        {
            set
            {
                if (SelectedEquipment!.IsRetired != value)
                {
                    SelectedEquipment.IsRetired = value;
                    SelectedEquipment.IsDirty = true;
                    IsDirty = true;
                }

                if (value)
                {
                    TryAddEquipmentGroup("Retired");
                }

                NextDisabled = false;
            }
            get
            {
                if (SelectedEquipment is not null)
                    return SelectedEquipment!.IsRetired;
                else
                    return false;

            }
        }

        /// <summary>
        /// Wraps SelectedEquipment.MfgDate
        /// </summary>
        private DateOnly? ManufactureDate
        {
            set
            {
                if (value is not null && value.HasValue)
                {
                    DateTime? valueAsDateTime = null;
                    valueAsDateTime = value.Value.ToDateTime(TimeOnly.MinValue);

                    if (valueAsDateTime is null)
                        return;

                    if (SelectedEquipment is not null && SelectedEquipment.MfgDate.HasValue == false || SelectedEquipment.MfgDate.Value != valueAsDateTime)
                    {
                        SelectedEquipment.MfgDate = valueAsDateTime;
                        SelectedEquipment.IsDirty = true;
                        IsDirty = true;
                    }
                }
                else
                {
                    if (SelectedEquipment is not null)
                    {
                        SelectedEquipment.MfgDate = null;
                        SelectedEquipment.IsDirty = true;
                        IsDirty = true;
                    }
                }
            }
            get
            {
                if (SelectedEquipment is not null && SelectedEquipment.MfgDate is not null && SelectedEquipment.MfgDate.HasValue)
                {
                    return DateOnly.FromDateTime(SelectedEquipment.MfgDate.Value);
                }
                return null;
            }
        }

        /// <summary>
        /// SelectedEquipment.InstallationDate - optional
        /// </summary>
        private DateOnly? InstallDate
        {
            set
            {

                if (value.HasValue)
                {

                    DateTime? valueAsDateTime = null;
                    valueAsDateTime = value.Value.ToDateTime(TimeOnly.MinValue);

                    if (SelectedEquipment!.InstallationDate != valueAsDateTime)
                    {
                        SelectedEquipment.InstallationDate = valueAsDateTime;
                        SelectedEquipment.IsDirty = true;
                        IsDirty = true;

                    }
                }
                else
                {
                    if (SelectedEquipment.InstallationDate.HasValue)
                    {
                        SelectedEquipment.InstallationDate = null;
                        SelectedEquipment.IsDirty = true;
                        IsDirty = true;
                    }
                }
            }
            get
            {
                if (SelectedEquipment is not null && SelectedEquipment.InstallationDate.HasValue)
                {
                    return DateOnly.FromDateTime(SelectedEquipment.InstallationDate.Value.Date);
                }
                return null;
            }
        }

        #region Dataplate extracted Date Parsing Helpers

        /// <summary>
        /// For dataPlate raw text back to edit as a proper date
        /// </summary>
        private DateOnly? pvdMfgDateTemp = null;
        private string? MfgDateTemp = null;
        private DateOnly? pvdInstDateTemp = null;
        private string? InstDateTemp = null;


        public void ClearDataplateReader()
        {
            DataPlateText = null;
            DataPlateList?.Clear();
            pvdMfgDateTemp = null; pvdInstDateTemp = null;
            bTriedBluon = false;
        }

        public async Task TryBluonNameplate()
        {
            if (bUseBluon && (SelectedEquipment is not null && bluonNameplateReader is not null))
            {
                var aaw = await bluonNameplateReader!.BluonNameplateReaderCould(DataPlateImage?.Data);//, SelectedEquipment.DataPlateFileName);
                if (aaw)
                {
                    Console.WriteLine("Bluon Nameplate Reader Results:");

                }
                else
                {
                    Console.WriteLine("Bluon Nameplate failed:");
                }
            }
        }
        public async Task TryBluonGetMfgDate()
        {
            try
            {
                if (bUseBluon && (SelectedEquipment is not null && bluonDecoder is not null))
                {
                    var aaw = await bluonDecoder!.AgeAndWarranty(SelectedEquipment.Make, SelectedEquipment.Serial);
                    bTriedBluon = true;
                    if (aaw is not null)
                    {
                        string? mfgDateIs = aaw.Data?.ManufacturingDate ?? null;
                        if (!string.IsNullOrEmpty(mfgDateIs))
                        {
                            DateTime? _manufactureDate = HvacDateParser.ParseDateText(mfgDateIs);
                            if (_manufactureDate is not null && _manufactureDate.HasValue)
                            {
                                ManufactureDate = new DateOnly(_manufactureDate.Value.Year, _manufactureDate.Value.Month, _manufactureDate.Value.Day);
                                StateHasChanged();
                            }
                        }
                    }

                }
            }
            catch { }
        }
        /// <summary>
        /// Bind and Set MfgDateEdit for intelligent parsing before trying to set the the InstallDate control
        /// </summary>
        private string? MfgDateEdit
        {
            set
            {
                if (!string.IsNullOrWhiteSpace(value) && GeneralFunctions.HasNumerics(value))
                {

                    DateTime? _manufactureDate = HvacDateParser.ParseDateText(value);
                    if (_manufactureDate.HasValue)
                    {
                        pvdMfgDateTemp = new DateOnly(_manufactureDate.Value.Year, _manufactureDate.Value.Month, _manufactureDate.Value.Day);

                        // Set the real datetime control with this valid date
                        ManufactureDate = pvdMfgDateTemp;

                        _ = JSRuntime.InvokeVoidAsync("scrollToTop");

                        // Set edit box
                        // pvsMfgDateTemp = $"{_installDate.Value:MM yy}";
                        MfgDateTemp = null;
                    }
                    else
                    {
                        // set edit box
                        MfgDateTemp = value;
                    }
                }
                else
                    MfgDateTemp = null;

            }

            get
            {
                if (pvdMfgDateTemp.HasValue)
                {
                    return $"{pvdMfgDateTemp:MM yy}";
                }
                else
                    return null;
            }

        }

        /// <summary>
        /// Bind Set InstDateEdit for intelligent parsing before trying to set the the InstallDate control
        /// </summary>
        private string? InstDateEdit
        {
            set
            {
                if (!string.IsNullOrWhiteSpace(value) && GeneralFunctions.HasNumerics(value))
                {

                    DateTime? _installDate = HvacDateParser.ParseDateText(value);
                    if (_installDate.HasValue)
                    {
                        pvdInstDateTemp = new DateOnly(_installDate.Value.Year, _installDate.Value.Month, _installDate.Value.Day);
                        // Set the real datetime control with this valid date
                        InstallDate = pvdInstDateTemp;

                        // scroll up to see it set
                        _ = JSRuntime.InvokeVoidAsync("scrollToTop");
                    }
                    else
                    {
                        // open manual edit box
                        InstDateTemp = value;
                    }
                }
                else
                    InstDateTemp = null;

            }

            get
            {
                if (pvdInstDateTemp.HasValue)
                {
                    return $"{pvdInstDateTemp:MM DD yyyy}";
                }
                else
                    return null;
            }

        }

        /// <summary>
        /// Set ManufacturerDate (1) or InstallationDate (2) for Real control if valid value
        /// </summary>
        private async Task SetDateManuallyEdited(int itemToDo = 0)
        {

            if (itemToDo == 1)
            {
                if (!string.IsNullOrWhiteSpace(MfgDateTemp) && GeneralFunctions.HasNumerics(MfgDateTemp))
                {
                    DateTime? _manufactureDate = HvacDateParser.ParseDateText(MfgDateTemp);
                    if (_manufactureDate.HasValue)
                    {
                        pvdMfgDateTemp = new DateOnly(_manufactureDate.Value.Year, _manufactureDate.Value.Month, _manufactureDate.Value.Day);

                        // Set the real datetime control with this valid date
                        ManufactureDate = pvdMfgDateTemp;
                        MfgDateTemp = null;
                    }
                }
                else
                    MfgDateTemp = null;

            }

            if (itemToDo == 2)
            {
                if (!string.IsNullOrWhiteSpace(InstDateTemp) && GeneralFunctions.HasNumerics(InstDateTemp))
                {
                    DateTime? _instDate = HvacDateParser.ParseDateText(InstDateTemp);
                    if (_instDate.HasValue)
                    {
                        pvdInstDateTemp = new DateOnly(_instDate.Value.Year, _instDate.Value.Month, _instDate.Value.Day);

                        // Set the real datetime control with this valid date
                        InstallDate = pvdInstDateTemp;
                        InstDateTemp = null;
                    }
                }
                else
                    InstDateTemp = null;


            }

            StateHasChanged();
            _ = JSRuntime.InvokeVoidAsync("scrollToTop");

        }

        #endregion

        /// <summary>
        /// SelectedEquipment.Note
        /// </summary>
        private string? Notes
        {
            set
            {
                if (SelectedEquipment!.Notes != value)
                {
                    SelectedEquipment.Notes = value;
                    SelectedEquipment.IsDirty = true;
                    IsDirty = true;
                }
                NextDisabled = false;
            }
            get
            {
                if (SelectedEquipment is not null)
                    return SelectedEquipment!.Notes;
                else
                    return string.Empty;
            }
        }

        #endregion Equipment Property Wrappers

        #region Attribute and Consumable Translators

        private async void DeleteConsumable(EquipmentAttribute consumable)
        {
            if (SelectedEquipment is not null)
            {
                if (await Confirm($"Delete consumable {consumable.Name}?", "Manage Consumables"))
                {
                    SelectedEquipment.Consumables.Remove(consumable);
                    StateHasChanged();
                }
            }
        }

        /// <summary>
        /// Provides access to the selected attribute actual value as a string - included for consistency in binding
        /// </summary>
        private string? SelectedAttributeValueAsString
        {
            set
            {
                if (SelectedAttribute!.SetStringValue(value))
                {
                    SelectedEquipment!.IsDirty = true;
                }
                NextDisabled = !SelectedAttribute.IsValid();
            }
            get
            {
                return SelectedAttribute?.GetStringValue();
            }
        }

        /// <summary>
        /// Provides access to the selected attribute actual value as a decimal
        /// </summary>
        private decimal? SelectedAttributeValueAsDecimal
        {
            set
            {
                if (SelectedAttribute is not null)
                {
                    if (SelectedAttribute!.SetDecimalValue(value))
                    {
                        SelectedEquipment!.IsDirty = true;
                    }

                    NextDisabled = !SelectedAttribute.IsValid();
                }
            }
            get
            {
                return SelectedAttribute?.GetDecimalValue();
            }
        }

        /// <summary>
        /// Provides access to the selected attribute actual value as a dateonly
        /// </summary>
        private DateOnly? SelectedAttributeValueAsDateOnly
        {
            set
            {
                if (SelectedAttribute!.SetDateOnlyValue(value))
                {
                    SelectedEquipment!.IsDirty = true;
                }

                NextDisabled = !SelectedAttribute.IsValid();
            }
            get
            {
                return SelectedAttribute?.GetDateOnlyValue();
            }
        }

        /// <summary>
        /// Provides access to the selected consumable actual quantity as a decimal
        /// </summary>
        private decimal? SelectedConsumableQuantity
        {
            set
            {
                if (SelectedConsumable!.Quantity != value)
                {
                    SelectedConsumable.Quantity = value;
                    SelectedEquipment!.IsDirty = true;
                }

                NextDisabled = !SelectedConsumable.IsValid();
            }
            get
            {
                return SelectedConsumable?.Quantity ?? 0;
            }
        }

        /// <summary>
        /// Provides access to the selected consumable actual value as string
        /// </summary>
        private string? SelectedConsumableValue
        {
            set
            {
                if (SelectedConsumable!.Value != value)
                {
                    SelectedConsumable.Value = value;
                    SelectedEquipment!.IsDirty = true;
                }

                NextDisabled = !SelectedConsumable.IsValid();
            }
            get
            {
                return SelectedConsumable?.Value;
            }
        }

        /// <summary>
        /// Provides access to the selected consumable actual note as string
        /// </summary>
        private string? SelectedConsumableNote
        {
            set
            {
                if (SelectedConsumable!.Notes != value)
                {
                    SelectedConsumable.Notes = value;
                    SelectedEquipment!.IsDirty = true;
                }

                NextDisabled = !SelectedConsumable.IsValid();
            }
            get
            {
                return SelectedConsumable?.Notes;
            }
        }

        #endregion Attribute Translators

        #region Images

        private async Task ProcessSerialNumber()
        {
            string[]? dptxt = await AiOCRDataplateImage(DataPlateImage, SelectedEquipment!.Serial, "Serial Number");
            if (dptxt is not null && dptxt.Length > 0)
            {
                EquipmentSerial = dptxt[0];
                StateHasChanged();

            }
        }
        private async Task ProcessDPPhoto()
        {

            try
            {
                string[]? dptxt = await AiOCRDataplateImage(DataPlateImage, "", "Read Entire Dataplate");
                if (dptxt is not null && dptxt.Length > 0)
                {
                    if (!string.IsNullOrEmpty(dptxt[0]))
                    {
                        string txt = dptxt[0].Replace("\a", "\r\n");
                        DataPlateText = string.Join("\r\n", txt);
                        DataPlateList = DataPlateText.Split("\r\n").ToList();
                        StateHasChanged();
                    }
                }
            }
            catch
            {
                Console.WriteLine("Error reading dataplate image");
            }
        }
        private async Task<string[]> AiOCRDataplateImage(ImageInsertInput? image, string boundOutput, string? caption, int showEditor = 0)
        {
            if (image.Data is not null)
            {

                ICSAiVisionReader aiVisionReader = new();
                string[]? ocrOutput = await aiVisionReader.ReadDataplate(null, null, image.Data!);
                if (ocrOutput is not null && ocrOutput.Length > 0)
                    return ocrOutput;

                aiVisionReader = null;

            }
            return Array.Empty<string>();

        }

        private async Task<string> ReadNameplate(ImageInsertInput? image)
        {
            if (image.Data is not null)
            {




            }
            //return Array.Empty<string>();
            return string.Empty;

        }

        private async Task DeleteImage(WFEquipmentModes equipmentMode)
        {
            switch (equipmentMode)
            {
                case WFEquipmentModes.UnitImage1:
                    if (await Confirm("Are you sure you want to delete the unit 1 photo?", "Delete Photo?"))
                    {
                        await DataService.DeleteImageAsync(UnitImage1!);
                        UnitImage1!.Data = null;
                        UnitImage1.OriginalFileName = null;
                        UnitImage1.ContentType = null;
                        SelectedEquipment!.UnitImage1FileName = null;
                        NextDisabled = true;
                    }
                    break;
                case WFEquipmentModes.UnitImage2:

                    if (await Confirm("Are you sure you want to delete the unit 2 photo?", "Delete Photo?"))
                    {
                        await DataService.DeleteImageAsync(UnitImage2!);
                        UnitImage2!.Data = null;
                        UnitImage2.OriginalFileName = null;
                        UnitImage2.ContentType = null;
                        SelectedEquipment!.UnitImage2FileName = null;
                        NextDisabled = true;
                    }
                    break;

                case WFEquipmentModes.DataPlatePicture:
                    if (await Confirm("Are you sure you want to delete the data plate photo?", "Delete Photo?"))
                    {
                        await DataService.DeleteImageAsync(DataPlateImage!);
                        DataPlateImage!.Data = null;
                        DataPlateImage.OriginalFileName = null;
                        DataPlateImage.ContentType = null;
                        SelectedEquipment!.DataPlateFileName = null;
                        NextDisabled = false;
                    }
                    break;

            }
        }
        public async Task Unit1FileChanged(InputFileChangeEventArgs e)
        {
            ErrorMessage = null;

            int maxWidth = 720;
            int maxHeigth = 1280;
            var file = await e.File.RequestImageFileAsync(e.File.ContentType, maxWidth, maxHeigth);
            Stream fileStream = file.OpenReadStream(file.Size);

            UnitImage1!.Data = new byte[file.Size];
            UnitImage1.ContentType = file.ContentType;
            UnitImage1.OriginalFileName = file.Name;
            SelectedEquipment!.UnitImage1FileName = UnitImage1.GeneratedFileName;

            MemoryStream memoryStream = new MemoryStream(UnitImage1.Data);
            await fileStream.CopyToAsync(memoryStream);
            memoryStream.Close();
            fileStream.Close();

            if (await SaveImageAsync(UnitImage1))
            {
                SelectedEquipment.UnitImage1FileName = UnitImage1.GeneratedFileName;
                SelectedEquipment.IsDirty = true;
                IsDirty = true;

                NextDisabled = false;
            }
        }
        public async Task Unit2FileChanged(InputFileChangeEventArgs e)
        {
            ErrorMessage = null;

            int maxWidth = 720;
            int maxHeigth = 1280;
            var file = await e.File.RequestImageFileAsync(e.File.ContentType, maxWidth, maxHeigth);
            Stream fileStream = file.OpenReadStream(file.Size);

            UnitImage2!.Data = new byte[file.Size];
            UnitImage2.ContentType = file.ContentType;
            UnitImage2.OriginalFileName = file.Name;
            SelectedEquipment!.UnitImage2FileName = UnitImage2.GeneratedFileName;

            MemoryStream memoryStream = new MemoryStream(UnitImage2.Data);
            await fileStream.CopyToAsync(memoryStream);
            memoryStream.Close();
            fileStream.Close();

            if (await SaveImageAsync(UnitImage2))
            {
                SelectedEquipment.UnitImage2FileName = UnitImage2.GeneratedFileName;
                SelectedEquipment.IsDirty = true;
                IsDirty = true;

                NextDisabled = false;
            }
        }
        public async Task DataPlateFileChanged(InputFileChangeEventArgs e)
        {
            ErrorMessage = null;

            int maxWidth = 720;
            int maxHeigth = 1280;
            var file = await e.File.RequestImageFileAsync(e.File.ContentType, maxWidth, maxHeigth);
            Stream fileStream = file.OpenReadStream(file.Size);

            DataPlateImage!.Data = new byte[file.Size];
            DataPlateImage.ContentType = file.ContentType;
            DataPlateImage.OriginalFileName = file.Name;
            SelectedEquipment!.DataPlateFileName = DataPlateImage!.GeneratedFileName;

            MemoryStream memoryStream = new MemoryStream(DataPlateImage.Data);
            await fileStream.CopyToAsync(memoryStream);
            memoryStream.Close();
            fileStream.Close();

            if (await SaveImageAsync(DataPlateImage))
            {
                SelectedEquipment!.DataPlateFileName = DataPlateImage.GeneratedFileName;
                SelectedEquipment.IsDirty = true;
                IsDirty = true;
                NextDisabled = false;

                //Get Notes from Dataplate Text
                if (!string.IsNullOrEmpty(DataPlateImage.GeneratedFileName))
                    _ = ProcessDPPhoto();
            }
        }

        #endregion Images

        #region Groups

        /// <summary>
        /// Show or hide equipment group items
        /// </summary>
        /// <param name="group"></param>
        private void ToggleGroup(string group)
        {
            if (SelectedGroup == group)
            {
                SelectedGroup = null;
            }
            else
            {
                SelectedGroup = group;
            }
            ErrorMessage = null;
        }

        private int GroupCount(string group)
        {
            if (group == "Retired")
            {
                return EquipmentList!.Count(e => e.IsRetired);
            }
            return EquipmentList!.Count(e => e.EquipmentGroup == group);
        }

        private bool IsGroupSelected(string group)
        {
            return SelectedGroup == group;
        }

        private string GroupSelectedStyle(string group)
        {
            if (group == SelectedGroup || (SelectedEquipment is not null && SelectedEquipment.EquipmentGroup == group))
            {
                return "selected";
            }
            return "";
        }

        #endregion Groups

        #region Equipment Methods Add/Edit

        /// <summary>
        /// Handler for the add new equipment button
        /// </summary>
        private void AddNewEquipment()
        {
            SetUpNewEquipment();
            AddInProgress = true;
            Mode = WFEquipmentModes.UnitImage1;
            EditMode = WFEditModes.None;
            NextDisabled = true;
        }

        /// <summary>
        /// Handler for the edit equipment button
        /// </summary>
        private async Task SetEditEquipment(WorkflowEquipment equipment)
        {
            if (!AllowEdit)
                return;

            if (SingleSelection)
            {
                if (CheckIfSelectedBefore(equipment))
                {
                    await DialogBox.WaitForDialogResultAsync(header: "Was Used Already", prompt: $"This equipment was selected previously in this same checklist. You may not select it again in this checklist, sorry.", okLabel: "OK", cancelLabel: "Ignore", defaultButton: 1);
                    return;
                }
            }

            bool imageChanged = false;

            SelectedEquipment = equipment;

            // reset dataplate Reader vars
            ClearDataplateReader();

            UnitImage1 = new()
            {
                EquipmentId = SelectedEquipment!.Id,
                TemporaryEquipmentId = SelectedEquipment.TemporaryId,
                ImageIndex = 1
            };
            if (!string.IsNullOrEmpty(SelectedEquipment.UnitImage1FileName))
            {
                UnitImage1.ParseContentType(SelectedEquipment.UnitImage1FileName);
                await DataService.HydrateImageAsync(UnitImage1);
                imageChanged = true;
            }

            UnitImage2 = new()
            {
                EquipmentId = SelectedEquipment!.Id,
                TemporaryEquipmentId = SelectedEquipment.TemporaryId,
                ImageIndex = 2
            };
            if (!string.IsNullOrEmpty(SelectedEquipment.UnitImage2FileName))
            {
                UnitImage2.ParseContentType(SelectedEquipment.UnitImage2FileName);
                await DataService.HydrateImageAsync(UnitImage2);
                imageChanged = true;
            }

            DataPlateImage = new()
            {
                EquipmentId = SelectedEquipment!.Id,
                TemporaryEquipmentId = SelectedEquipment.TemporaryId,
                ImageIndex = 3
            };
            if (!string.IsNullOrEmpty(SelectedEquipment.DataPlateFileName))
            {
                DataPlateImage.ParseContentType(SelectedEquipment.DataPlateFileName);
                await DataService.HydrateImageAsync(DataPlateImage);
                imageChanged = true;
            }

            if (imageChanged)
            {
                StateHasChanged();
            }

            Mode = WFEquipmentModes.Confirmation;
            NextDisabled = false;
        }

        /// <summary>
        /// Checks to see it the iterazg equipment is equal to the selected equipment
        /// </summary>
        private bool IsEquipmentSelected(DispatchDetailResult.Equipment equipment)
        {
            if (SelectedEquipment is not null && SelectedEquipment.Id == equipment.Id && SelectedEquipment.TemporaryId == equipment.TemporaryId)
            {
                return true;
            }

            return false;
        }

        private string SelectedEquipmentIcon(DispatchDetailResult.Equipment equipment)
        {
            if (SelectedEquipment is not null && SelectedEquipment.Id == equipment.Id && SelectedEquipment.TemporaryId == equipment.TemporaryId)
            {
                return "bi-check2-square";
            }

            return "bi-square";
        }

        /// <summary>
        /// Gets CSS display for selected equipment
        /// </summary>
        private string EquipmentSelectedStyle(DispatchDetailResult.Equipment equipment)
        {
            if (IsEquipmentSelected(equipment))
            {
                if (MultiSelectMode)
                    return "Selected DimMe";
                else
                    return "Selected";
            }
            return "";
        }

        /// <summary>
        /// Gets CSS display for previously selected equipment
        /// </summary>
        private string EquipmentSelectedAlreadyStyle(WorkflowEquipment equipment)
        {
            if (CheckIfSelectedBefore(equipment))
                return "SelectedAlready";
            return "";
        }

        private string GroupSelectedAlreadyStyle(string group)
        {
            if (SingleSelection)
            {
                if (runner is not null && runner.AllValues is not null && EquipmentList is not null)
                {
                    foreach (var equipment in EquipmentList.Where(e => e.EquipmentGroup == group))
                    {
                        WorkflowResultDetailResult.WorkflowStepValue? existingValue = null;

                        if (equipment.Id == 0)
                        {
                            existingValue = runner.AllValues.FirstOrDefault(v => v.Value == equipment.TemporaryId.ToString());
                        }
                        else
                        {
                            existingValue = runner.AllValues.FirstOrDefault(v => v.Value == equipment.Id.ToString() || v.Value == equipment.TemporaryId.ToString());
                        }

                        if (existingValue == null)
                        {
                            return string.Empty;
                        }
                    }

                    return "SelectedAlready";
                }
            }

            return string.Empty;
        }

        /// <summary>
        /// See if potential equipment is already used in running workflow
        /// </summary>
        /// <param name="equipment"></param>
        private bool CheckIfSelectedBefore(WorkflowEquipment equipment)
        {
            if (runner?.AllValues is null || equipment is null)
            {
                return false;
            }

            foreach (var eq in runner!.AllValues)
            {
                if (eq != runner.CurrentValue && eq.WorkflowDataTypeId == 18 && (eq.Value == equipment.TemporaryId.ToString() || eq.Value == equipment.Id.ToString()))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Handles equipment Selection
        /// </summary>
        /// <param name="equipment"></param>
        private async Task SelectEquipment(WorkflowEquipment equipment)
        {

            if (SingleSelection)
            {
                //Prevent selecting same equipment used already in this checklist
                var lastCurrStep = runner?.AllValues?.LastOrDefault();
                if (lastCurrStep is not null && (runner?.AllValues?.Exists(x => x.LoopIndex != lastCurrStep.LoopIndex) ?? false))
                {
                    if (CheckIfSelectedBefore(equipment))
                    {
                        int dlgRet = await DialogBox.WaitForDialogResultAsync(header: "Was Used Already", prompt: $"This equipment was selected previously in this same checklist. You may not select it again in this checklist, sorry.", okLabel: "OK", cancelLabel: "Ignore", defaultButton: 1);
                        return;
                    }
                }
            }

            if (SelectedEquipment is not null && SelectedEquipment.Id == equipment.Id && SelectedEquipment.TemporaryId == equipment.TemporaryId)
            {
                SelectedEquipment = null;
            }
            else
            {
                SelectedEquipment = equipment;
            }
            if (WorkflowStep!.IsRequired && SelectedEquipment == null)
            {
                NextDisabled = true;
            }
            else
            {
                NextDisabled = false;
            }
        }

        /// <summary>
        /// Creates new equipment instance and walks the values
        /// </summary>
        private void SetUpNewEquipment()
        {
            SelectedEquipment = WorkflowEquipmentBuilder.CreateNewEquipment(DataService.AppState.AuthorizedUser!.Id, runner!.Dispatch!, Lookups!);

            UnitImage1 = new()
            {
                TemporaryEquipmentId = SelectedEquipment.TemporaryId,
                EquipmentId = SelectedEquipment.Id,
                ImageIndex = 1
            };
            UnitImage2 = new()
            {
                TemporaryEquipmentId = SelectedEquipment.TemporaryId,
                EquipmentId = SelectedEquipment.Id,
                ImageIndex = 2
            };
            DataPlateImage = new()
            {
                TemporaryEquipmentId = SelectedEquipment.TemporaryId,
                EquipmentId = SelectedEquipment.Id,
                ImageIndex = 3
            };
        }

        #endregion Equipment Methods Add/Edit

        #region Navigation

        /// <summary>
        /// When editing a single value
        private void EditEquipment(WFEquipmentModes mode)
        {
            EditMode = WFEditModes.EditEquipment;
            Mode = mode;
        }

        /// <summary>
        /// Sets the selected attribute and changes mode EditAttribute and Attributes
        /// </summary>
        private void EditAttribute(EquipmentAttribute equipmentAttribute)
        {
            SelectedAttribute = equipmentAttribute;
            SelectedAttribute.LastModifiedBy = DataService.AppState.AuthorizedUser?.Id ?? 0;
            SelectedAttribute.LastModifiedDate = DateTime.UtcNow;
            EditMode = WFEditModes.EditAttribute;
            Mode = WFEquipmentModes.Attributes;
        }

        /// <summary>
        /// Sets the selected consumable and changes mode EditConsumable and Consumables
        /// </summary>
        private void EditConsumable(EquipmentAttribute equipmentAttribute)
        {
            SelectedConsumable = equipmentAttribute;
            SelectedConsumable.LastModifiedBy = DataService.AppState.AuthorizedUser?.Id ?? 0;
            SelectedConsumable.LastModifiedDate = DateTime.UtcNow;
            EditMode = WFEditModes.EditConsumable;
            Mode = WFEquipmentModes.Consumables;
        }

        /// <summary>
        /// Creates a new consumable based on the attribute and sets modes to add consumable and Consumables
        /// </summary>
        /// <param name="equipmentAttribute"></param>
        private void AddConsumable(EquipmentAttribute equipmentAttribute)
        {
            SelectedConsumable = SelectedEquipment!.CreateNewConsumable(equipmentAttribute);
            SelectedConsumable.LastModifiedBy = DataService.AppState.AuthorizedUser?.Id ?? 0;
            SelectedConsumable.LastModifiedDate = DateTime.UtcNow;
            EditMode = WFEditModes.AddConsumable;
            Mode = WFEquipmentModes.Consumables;
            PreviousDisabled = false;
            NextDisabled = true;
        }

        private void SetEquipmentMode(WFEquipmentModes mode, bool nextDisabled)
        {
            PreviousWFEquipmentMode = mode;
            Mode = mode;
            NextDisabled = nextDisabled;
        }

        ///// <summary>
        ///// Navigates to a previous missing equipment value
        ///// </summary>
        private void GoToPreviousMissingEquipmentValue()
        {
            if (string.IsNullOrEmpty(SelectedEquipment!.EquipmentGroup) && PreviousWFEquipmentMode > WFEquipmentModes.EquipmentGroup)
            {
                SetEquipmentMode(WFEquipmentModes.EquipmentGroup, false);
            }
            else if (string.IsNullOrEmpty(SelectedEquipment.EquipLocArea) && PreviousWFEquipmentMode > WFEquipmentModes.EquipmentLocation)
            {
                SetEquipmentMode(WFEquipmentModes.EquipmentLocation, string.IsNullOrEmpty(SelectedEquipment.EquipLocArea));
            }
            else if (string.IsNullOrEmpty(SelectedEquipment.UnitTag) && PreviousWFEquipmentMode > WFEquipmentModes.UnitTag)
            {
                SetEquipmentMode(WFEquipmentModes.UnitTag, string.IsNullOrEmpty(SelectedEquipment.UnitTag));
            }
            else if (SelectedEquipment.EquipmentTypeId == -1 && PreviousWFEquipmentMode > WFEquipmentModes.EquipmentType)
            {
                SetEquipmentMode(WFEquipmentModes.EquipmentType, true);
            }
            else if (!SelectedEquipment!.InstallationDate.HasValue && PreviousWFEquipmentMode > WFEquipmentModes.InstallDate)
            {
                SetEquipmentMode(WFEquipmentModes.InstallDate, false);
            }
            else if (!SelectedEquipment.MfgDate.HasValue && PreviousWFEquipmentMode > WFEquipmentModes.ManufactureDate)
            {
                SetEquipmentMode(WFEquipmentModes.ManufactureDate, false);
            }
            else if (string.IsNullOrEmpty(SelectedEquipment.Serial) && PreviousWFEquipmentMode > WFEquipmentModes.EnterSerialNumber)
            {
                SetEquipmentMode(WFEquipmentModes.EnterSerialNumber, string.IsNullOrEmpty(SelectedEquipment.Serial));
            }
            else if (string.IsNullOrEmpty(SelectedEquipment.Model) && PreviousWFEquipmentMode > WFEquipmentModes.EnterModel)
            {
                SetEquipmentMode(WFEquipmentModes.EnterModel, string.IsNullOrEmpty(SelectedEquipment.Model));
            }
            else if (string.IsNullOrEmpty(SelectedEquipment!.Make) && PreviousWFEquipmentMode > WFEquipmentModes.ChooseManufacturer)
            {
                SetEquipmentMode(WFEquipmentModes.ChooseManufacturer, string.IsNullOrEmpty(SelectedEquipment.Make));
            }
            else if (string.IsNullOrEmpty(SelectedEquipment.DataPlateFileName) && PreviousWFEquipmentMode > WFEquipmentModes.DataPlatePicture)
            {
                SetEquipmentMode(WFEquipmentModes.DataPlatePicture, false);
            }
            else if (string.IsNullOrEmpty(SelectedEquipment.UnitImage2FileName) && PreviousWFEquipmentMode > WFEquipmentModes.UnitImage2)
            {
                SetEquipmentMode(WFEquipmentModes.UnitImage2, false);
            }
            else if (string.IsNullOrEmpty(SelectedEquipment.UnitImage1FileName) && PreviousWFEquipmentMode > WFEquipmentModes.UnitImage1)
            {
                SetEquipmentMode(WFEquipmentModes.UnitImage1, false);
            }
            else
            {
                EditMode = WFEditModes.None;
                Mode = WFEquipmentModes.Confirmation;
            }
        }

        /// <summary>
        /// Sets up the mode for adding missing equipment values.
        /// </summary>
        private void AutoPlayMissingEquipmentValues()
        {
            EditMode = WFEditModes.AutoplayEquipment;
            PreviousWFEquipmentMode = WFEquipmentModes.AutoPlay;
            GoToNextMissingEquipmentValues();
        }

        /// <summary>
        /// Navigate to the next missing equipment value.
        /// </summary>
        private void GoToNextMissingEquipmentValues()
        {
            if (string.IsNullOrEmpty(SelectedEquipment!.UnitImage1FileName) && PreviousWFEquipmentMode < WFEquipmentModes.UnitImage1)
            {
                SetEquipmentMode(WFEquipmentModes.UnitImage1, true);
            }
            else if (string.IsNullOrEmpty(SelectedEquipment!.UnitImage2FileName) && PreviousWFEquipmentMode < WFEquipmentModes.UnitImage2)
            {
                SetEquipmentMode(WFEquipmentModes.UnitImage2, true);
            }
            else if (string.IsNullOrEmpty(SelectedEquipment!.DataPlateFileName) && PreviousWFEquipmentMode < WFEquipmentModes.DataPlatePicture)
            {
                SetEquipmentMode(WFEquipmentModes.DataPlatePicture, false);
            }
            else if (string.IsNullOrEmpty(SelectedEquipment.Make) && PreviousWFEquipmentMode < WFEquipmentModes.ChooseManufacturer)
            {
                SetEquipmentMode(WFEquipmentModes.ChooseManufacturer, string.IsNullOrEmpty(SelectedEquipment.Make));
            }
            else if (string.IsNullOrEmpty(SelectedEquipment.Model) && PreviousWFEquipmentMode < WFEquipmentModes.EnterModel)
            {
                SetEquipmentMode(WFEquipmentModes.EnterModel, string.IsNullOrEmpty(SelectedEquipment.Model));
            }
            else if (string.IsNullOrEmpty(SelectedEquipment.Serial) && PreviousWFEquipmentMode < WFEquipmentModes.EnterSerialNumber)
            {
                SetEquipmentMode(WFEquipmentModes.EnterSerialNumber, string.IsNullOrEmpty(SelectedEquipment.Serial));
            }
            else if (!SelectedEquipment.MfgDate.HasValue && PreviousWFEquipmentMode < WFEquipmentModes.ManufactureDate)
            {
                SetEquipmentMode(WFEquipmentModes.ManufactureDate, false);
            }
            else if (!SelectedEquipment.InstallationDate.HasValue && PreviousWFEquipmentMode < WFEquipmentModes.InstallDate)
            {
                SetEquipmentMode(WFEquipmentModes.InstallDate, false);
            }
            else if (SelectedEquipment.EquipmentTypeId == -1 && PreviousWFEquipmentMode < WFEquipmentModes.EquipmentType)
            {
                SetEquipmentMode(WFEquipmentModes.EquipmentType, true);
            }
            else if (string.IsNullOrEmpty(SelectedEquipment.UnitTag) && PreviousWFEquipmentMode < WFEquipmentModes.UnitTag)
            {
                SetEquipmentMode(WFEquipmentModes.UnitTag, string.IsNullOrEmpty(SelectedEquipment.UnitTag));
            }
            else if (string.IsNullOrEmpty(SelectedEquipment.EquipLocArea) && PreviousWFEquipmentMode < WFEquipmentModes.EquipmentLocation)
            {
                SetEquipmentMode(WFEquipmentModes.EquipmentLocation, string.IsNullOrEmpty(SelectedEquipment.EquipLocArea));
            }
            else if (string.IsNullOrEmpty(SelectedEquipment.EquipmentGroup) && PreviousWFEquipmentMode < WFEquipmentModes.EquipmentGroup)
            {
                SetEquipmentMode(WFEquipmentModes.EquipmentGroup, false);
            }
            else if (string.IsNullOrEmpty(SelectedEquipment.Notes) && PreviousWFEquipmentMode < WFEquipmentModes.Note)
            {
                SetEquipmentMode(WFEquipmentModes.Note, false);
            }
            else
            {
                EditMode = WFEditModes.None;
                Mode = WFEquipmentModes.Confirmation;
            }
        }

        /// <summary>
        /// Sets up the modes for adding missing attribute values.
        /// </summary>
        private void AutoPlayMissingAttributeValues()
        {
            SelectedAttributeIndex = -1;
            Mode = WFEquipmentModes.Attributes;
            EditMode = WFEditModes.AutoplayAttributes;
            GoToNextMissingAttributeValue();
        }

        /// <summary>
        /// Navigates to the next missing attribute value
        /// </summary>
        private void GoToNextMissingAttributeValue()
        {
            SelectedAttributeIndex = SelectedEquipment!.NextMissingAttributeIndex(SelectedAttributeIndex);

            if (SelectedAttributeIndex != -1)
            {
                SelectedAttribute = SelectedEquipment.Attributes[SelectedAttributeIndex];
                NextDisabled = SelectedAttribute.Required && string.IsNullOrEmpty(SelectedAttribute.Value);
            }
            else
            {
                SelectedAttribute = null;
                EditMode = WFEditModes.None;
                Mode = WFEquipmentModes.Confirmation;
            }
        }

        /// <summary>
        /// Navigates to a previous missing attribute value
        /// </summary>
        private void GoToPreviousMissingAttributeValue()
        {
            SelectedAttributeIndex = SelectedEquipment!.PreviousMissingAttributeIndex(SelectedAttributeIndex);

            if (SelectedAttributeIndex != -1)
            {
                SelectedAttribute = SelectedEquipment.Attributes[SelectedAttributeIndex];

                if (SelectedAttribute.Required)
                {
                    NextDisabled = string.IsNullOrEmpty(SelectedAttribute.Value);
                }
                else
                {
                    NextDisabled = false;
                }
            }
            else
            {
                SelectedAttribute = null;
                EditMode = WFEditModes.None;
                Mode = WFEquipmentModes.Confirmation;
            }
        }

        private void TryAddEquipmentGroup(string? groupName)
        {
            if (!string.IsNullOrEmpty(groupName))
            {
                if (!GroupList.Contains(groupName))
                {
                    GroupList.Add(groupName);
                }
            }
        }

        /// <summary>
        /// Handles the next button click
        /// </summary>
        private async Task Next()
        {
            PreviousDisabled = false;

            if (EditMode == WFEditModes.AutoplayEquipment)
            {
                await SaveEquipmentAsync();
                GoToNextMissingEquipmentValues();
            }
            else if (EditMode == WFEditModes.AutoplayAttributes)
            {
                await SaveEquipmentAsync();
                GoToNextMissingAttributeValue();
            }
            else if (EditMode == WFEditModes.AddConsumable)
            {
                SelectedEquipment!.Consumables.Add(SelectedConsumable!.Clone());
                SelectedConsumable = null;
                await SaveEquipmentAsync();
                EditMode = WFEditModes.OpenConsumable;
            }
            else if (EditMode == WFEditModes.EditConsumable)
            {
                SelectedConsumable = null;
                await SaveEquipmentAsync();
                Mode = WFEquipmentModes.Confirmation;
                EditMode = WFEditModes.None;
            }
            else if (EditMode == WFEditModes.EditEquipment)
            {
                await SaveEquipmentAsync();
                Mode = WFEquipmentModes.Confirmation;
                EditMode = WFEditModes.None;
            }
            else if (EditMode == WFEditModes.EditAttribute)
            {
                SelectedAttribute = null;
                SelectedAttributeIndex = 0;
                await SaveEquipmentAsync();
                Mode = WFEquipmentModes.Confirmation;
                EditMode = WFEditModes.None;
            }
            else if (EditMode == WFEditModes.OpenConsumable)
            {
                EditMode = WFEditModes.None;
                Mode = WFEquipmentModes.Confirmation;
            }
            else // Edit mode = none (adding)
            {
                switch (Mode)
                {
                    case WFEquipmentModes.Selecting:

                        if (SelectedEquipment is not null)
                        {
                            if (SelectedEquipment.TemporaryId is not null)
                            {
                                Input.Value = SelectedEquipment.TemporaryId.ToString();
                            }
                            else
                            {
                                Input.Value = SelectedEquipment.Id.ToString();
                            }
                        }

                        SetInputPayload();

                        await MoveNext();

                        break;

                    case WFEquipmentModes.UnitImage1:

                        Mode = WFEquipmentModes.UnitImage2;
                        NextDisabled = string.IsNullOrEmpty(SelectedEquipment!.UnitImage2FileName);
                        break;

                    case WFEquipmentModes.UnitImage2:

                        Mode = WFEquipmentModes.DataPlatePicture;
                        NextDisabled = string.IsNullOrEmpty(SelectedEquipment!.DataPlateFileName);
                        break;

                    case WFEquipmentModes.DataPlatePicture:

                        Mode = WFEquipmentModes.ChooseManufacturer;
                        NextDisabled = string.IsNullOrEmpty(SelectedEquipment!.Make);
                        break;

                    case WFEquipmentModes.ChooseManufacturer:

                        Mode = WFEquipmentModes.EnterModel;
                        NextDisabled = string.IsNullOrEmpty(SelectedEquipment!.Model);
                        break;

                    case WFEquipmentModes.EnterModel:

                        Mode = WFEquipmentModes.EnterSerialNumber;
                        NextDisabled = string.IsNullOrEmpty(SelectedEquipment!.Serial);
                        break;

                    case WFEquipmentModes.EnterSerialNumber:

                        if (!string.IsNullOrEmpty(SelectedEquipment.Serial) && !string.IsNullOrEmpty(SelectedEquipment.Make))
                        {
                            _ = TryBluonGetMfgDate();
                        }

                        Mode = WFEquipmentModes.ManufactureDate;
                        NextDisabled = false;
                        break;

                    case WFEquipmentModes.ManufactureDate:

                        Mode = WFEquipmentModes.InstallDate;
                        NextDisabled = false;
                        break;

                    case WFEquipmentModes.InstallDate:

                        Mode = WFEquipmentModes.EquipmentType;
                        NextDisabled = string.IsNullOrEmpty(SelectedEquipment!.EqType);
                        break;

                    case WFEquipmentModes.EquipmentType:

                        Mode = WFEquipmentModes.UnitTag;
                        NextDisabled = string.IsNullOrEmpty(SelectedEquipment!.UnitTag);
                        break;

                    case WFEquipmentModes.UnitTag:

                        Mode = WFEquipmentModes.EquipmentLocation;
                        NextDisabled = string.IsNullOrEmpty(SelectedEquipment!.EquipLocArea);
                        break;

                    case WFEquipmentModes.EquipmentLocation:

                        Mode = WFEquipmentModes.EquipmentGroup;
                        NextDisabled = false;
                        break;

                    case WFEquipmentModes.EquipmentGroup:

                        Mode = WFEquipmentModes.IsRetired;
                        NextDisabled = false;
                        break;

                    case WFEquipmentModes.IsRetired:

                        Mode = WFEquipmentModes.Note;
                        NextDisabled = false;
                        break;

                    case WFEquipmentModes.Note:

                        if (SelectedEquipment!.HasAttributes())
                        {
                            SelectedAttributeIndex = 0;
                            SelectedAttribute = SelectedEquipment.Attributes[SelectedAttributeIndex];
                            Mode = WFEquipmentModes.Attributes;
                            NextDisabled = !SelectedAttribute.IsValid();
                        }
                        else if (SelectedEquipment.HasAvailableConsumables())
                        {
                            Mode = WFEquipmentModes.Consumables;
                            NextDisabled = false;
                        }
                        else
                        {
                            await SaveEquipmentAsync();
                            Mode = WFEquipmentModes.Confirmation;
                            NextDisabled = false;
                        }
                        break;

                    case WFEquipmentModes.Attributes:

                        if (SelectedEquipment!.Attributes.Count - 1 > SelectedAttributeIndex)
                        {
                            SelectedAttributeIndex++;
                            SelectedAttribute = SelectedEquipment.Attributes[SelectedAttributeIndex];
                            NextDisabled = !SelectedAttribute.IsValid();
                        }
                        else if (SelectedEquipment.HasAvailableConsumables())
                        {
                            Mode = WFEquipmentModes.Consumables;
                            NextDisabled = false;
                        }
                        else
                        {
                            await SaveEquipmentAsync();
                            Mode = WFEquipmentModes.Confirmation;
                            NextDisabled = false;
                        }
                        break;

                    case WFEquipmentModes.Consumables:
                        await SaveEquipmentAsync();
                        Mode = WFEquipmentModes.Confirmation;
                        NextDisabled = false;
                        break;

                    case WFEquipmentModes.Confirmation:

                        PreviousDisabled = true;

                        if (SelectedEquipment is not null)
                        {
                            if (CheckIfSelectedBefore(SelectedEquipment))
                            {
                                int dlgRet = await DialogBox.WaitForDialogResultAsync(header: "Was Used Already", prompt: $"This equipment was selected previously in this same checklist. You may not select it again in this checklist, sorry.", okLabel: "OK", cancelLabel: "Ignore", defaultButton: 1);
                                SelectedEquipment = null;
                            }
                            else
                            {
                                if (SelectedEquipment.TemporaryId is not null)
                                {
                                    Input.Value = SelectedEquipment.TemporaryId.ToString();
                                }
                                else
                                {
                                    Input.Value = SelectedEquipment.Id.ToString();
                                }

                                if (SelectedEquipment.IsRetired)
                                {
                                    SelectedGroup = "Retired";
                                }
                                else
                                {
                                    SelectedGroup = SelectedEquipment.EquipmentGroup;
                                }
                            }
                        }

                        AddInProgress = false;
                        Mode = WFEquipmentModes.Selecting;

                        break;
                }
            }
        }

        /// <summary>
        /// Handles the previous button click
        /// </summary>
        private async Task Previous()
        {
            NextDisabled = false;

            if (EditMode == WFEditModes.EditAttribute)
            {
                SelectedAttribute = null;
                SelectedAttributeIndex = 0;
                Mode = WFEquipmentModes.Confirmation;
                EditMode = WFEditModes.None;
            }
            else if (EditMode == WFEditModes.EditEquipment)
            {
                Mode = WFEquipmentModes.Confirmation;
                EditMode = WFEditModes.None;
            }
            else if (EditMode == WFEditModes.AddConsumable)
            {
                if (SelectedConsumable is not null)
                {
                    SelectedEquipment!.Consumables.Remove(SelectedConsumable);
                }
                EditMode = WFEditModes.OpenConsumable;
                Mode = WFEquipmentModes.Consumables;
                SelectedConsumable = null;
            }
            else if (EditMode == WFEditModes.OpenConsumable)
            {
                Mode = WFEquipmentModes.Confirmation;
                EditMode = WFEditModes.None;
            }
            else if (EditMode == WFEditModes.AutoplayAttributes)
            {
                GoToPreviousMissingAttributeValue();
            }
            else if (EditMode == WFEditModes.AutoplayEquipment)
            {
                GoToPreviousMissingEquipmentValue();
            }
            else
            {
                switch (Mode)
                {
                    case WFEquipmentModes.UnitImage1:

                        if (EquipmentList!.IndexOf(SelectedEquipment) == -1)
                        {
                            if (await Confirm("Moving to the previous step will delete the new equipment.  <br><b>Proceed?</b>", "Abort Edits?"))
                            {
                                SelectedEquipment = null;
                                AddInProgress = false;
                                Mode = WFEquipmentModes.Selecting;
                            }
                        }

                        break;

                    case WFEquipmentModes.UnitImage2:

                        Mode = WFEquipmentModes.UnitImage1;
                        break;

                    case WFEquipmentModes.DataPlatePicture:

                        Mode = WFEquipmentModes.UnitImage2;
                        break;

                    case WFEquipmentModes.ChooseManufacturer:

                        Mode = WFEquipmentModes.DataPlatePicture;
                        break;

                    case WFEquipmentModes.EnterModel:

                        Mode = WFEquipmentModes.ChooseManufacturer;
                        break;

                    case WFEquipmentModes.EnterSerialNumber:

                        Mode = WFEquipmentModes.EnterModel;
                        break;

                    case WFEquipmentModes.ManufactureDate:

                        Mode = WFEquipmentModes.EnterSerialNumber;
                        break;

                    case WFEquipmentModes.InstallDate:

                        Mode = WFEquipmentModes.ManufactureDate;
                        break;

                    case WFEquipmentModes.EquipmentType:

                        Mode = WFEquipmentModes.InstallDate;
                        break;

                    case WFEquipmentModes.UnitTag:

                        Mode = WFEquipmentModes.EquipmentType;
                        break;

                    case WFEquipmentModes.EquipmentLocation:

                        Mode = WFEquipmentModes.UnitTag;
                        break;

                    case WFEquipmentModes.EquipmentGroup:

                        Mode = WFEquipmentModes.EquipmentLocation;
                        break;

                    case WFEquipmentModes.IsRetired:

                        Mode = WFEquipmentModes.EquipmentGroup;
                        break;

                    case WFEquipmentModes.Note:

                        Mode = WFEquipmentModes.IsRetired;
                        break;

                    case WFEquipmentModes.Attributes:

                        if (SelectedAttributeIndex > 0)
                        {
                            SelectedAttributeIndex--;
                            SelectedAttribute = SelectedEquipment!.Attributes[SelectedAttributeIndex];
                        }
                        else
                        {
                            Mode = WFEquipmentModes.Note;
                        }

                        break;

                    case WFEquipmentModes.Consumables:

                        if (SelectedEquipment!.HasAttributes())
                        {
                            Mode = WFEquipmentModes.Attributes;
                        }
                        else
                        {
                            Mode = WFEquipmentModes.Note;
                        }
                        break;

                    case WFEquipmentModes.Confirmation:

                        if (AddInProgress)
                        {
                            if (SelectedEquipment!.HasAvailableConsumables())
                            {
                                Mode = WFEquipmentModes.Consumables;
                            }
                            else if (SelectedEquipment!.HasAttributes())
                            {
                                Mode = WFEquipmentModes.Attributes;
                            }
                            else
                            {
                                Mode = WFEquipmentModes.Note;
                            }
                        }
                        else
                        {
                            Mode = WFEquipmentModes.Selecting;

                        }
                        break;

                    case WFEquipmentModes.Selecting:

                        await MovePrevious();
                        break;
                }
            }
        }

        #endregion Navigation

        #region Click Handling Methods

        /// <summary>
        /// Handles header click - routes to multi-select toggle or single select
        /// </summary>
        private async Task HandleEquipmentHeaderClick(WorkflowEquipment equipment)
        {
            if (MultiSelectMode)
            {
                ToggleMultiSelect(equipment);
            }
            else
            {
                await SelectEquipment(equipment);
            }
        }

        /// <summary>
        /// Handles body click - routes to edit mode (but not in multi-select mode)
        /// </summary>
        private async Task HandleEquipmentBodyClick(WorkflowEquipment equipment)
        {
            if (MultiSelectMode)
            {
                // In multi-select mode, body tap also toggles selection
                ToggleMultiSelect(equipment);
            }
            else
            {
                await SetEditEquipment(equipment);
            }
        }

        #endregion

        #region Render Helper Methods

        /// <summary>
        /// Renders the equipment details (unit tag, location, model, serial)
        /// This is extracted to avoid code duplication
        /// </summary>
        private RenderFragment RenderEquipmentDetails(WorkflowEquipment equipment) => builder =>
        {
            int seq = 0;
            // CurrentSortMode == EquipmentSortMode.Manufacturer

            // Unit Tag and Location
            if (!string.IsNullOrEmpty(equipment.UnitTag) && !string.IsNullOrEmpty(equipment.EquipLocArea))
            {
                builder.OpenElement(seq++, "strong");
                builder.AddAttribute(seq++, "style", "font-weight: 800;");
                if (CurrentSortMode == EquipmentSortMode.Manufacturer || CurrentSortMode == EquipmentSortMode.ManufacturerDesc)
                {

                    builder.AddContent(seq++, $"{equipment.UnitTag} ");
                }
                else
                {
                    builder.AddContent(seq++, $"{equipment.Make} ");
                }
                builder.CloseElement();
                builder.OpenElement(seq++, "br");
                builder.AddContent(seq++, $" ({equipment.EquipLocArea})");
                builder.CloseElement();
            }
            else if (!string.IsNullOrEmpty(equipment.UnitTag))
            {
                builder.AddContent(seq++, equipment.UnitTag);
                builder.OpenElement(seq++, "br");
                builder.CloseElement();
            }
            else if (!string.IsNullOrEmpty(equipment.EquipLocArea))
            {
                builder.AddContent(seq++, $"Unit in ({equipment.EquipLocArea})");
                builder.OpenElement(seq++, "br");
                builder.CloseElement();
            }
            else
            {
                builder.AddContent(seq++, "Unit: (unknown)");
                builder.OpenElement(seq++, "br");
                builder.CloseElement();
            }

            // Model
            builder.AddContent(seq++, equipment.Model);
            builder.OpenElement(seq++, "br");
            builder.CloseElement();

            // Serial
            if (!string.IsNullOrEmpty(equipment.Serial))
            {
                builder.AddContent(seq++, equipment.Serial);
            }

            // Retired indicator (for ungrouped equipment display)
            if (equipment.IsRetired)
            {
                builder.OpenElement(seq++, "span");
                builder.AddAttribute(seq++, "class", "text-bg-danger");
                builder.AddContent(seq++, "** Unit has been retired!");
                builder.CloseElement();
            }
        };

        #endregion

        #region Save Methods

        private void SetInputPayload()
        {


            var payloadEquipment = WorkflowEquipmentBuilder.GetPayloadEquipment(EquipmentList);
            if (payloadEquipment.Count == 0)
            {
                Input!.JsonPayload = null;
            }
            else
            {
                Input!.JsonPayload = JsonSerializer.Serialize(payloadEquipment);
            }
        }

        private async Task SaveEquipmentAsync()
        {
            if (SelectedEquipment is not null)
            {
                if (SelectedEquipment.IsDirty == true)
                {
                    SelectedEquipment.ModifiedDate = DateTime.UtcNow;
                    SelectedEquipment.ModifiedBy = DataService.AppState.AuthorizedUser!.Id;

                    if (EquipmentList!.IndexOf(SelectedEquipment) == -1)
                    {
                        EquipmentList.Insert(0, SelectedEquipment);
                    }

                    runner.UpdateEquipment(SelectedEquipment);

                    await runner!.SaveDispatchAsync();
                }
            }
        }

        #endregion Save Methods

        protected override bool IsValid()
        {
            if (WorkflowStep!.IsRequired)
            {
                return SelectedEquipment is not null;
            }
            return true;
        }

        protected override void OnParametersSet()
        {
            base.OnParametersSet();

            Initialized = false;

            try
            {
                Lookups = runner.Lookups;
                EquipmentList = WorkflowEquipmentBuilder.GenerateEquipmentList(runner.Dispatch, runner.Lookups);
                if (!string.IsNullOrEmpty(Input!.JsonPayload))
                {
                    try
                    {
                        List<WorkflowEquipment>? payloadEquipmentList = JsonSerializer.Deserialize<List<WorkflowEquipment>>(Input.JsonPayload);
                        if (payloadEquipmentList is not null && payloadEquipmentList.Count != 0)
                        {
                            WorkflowEquipmentBuilder.MergePayloadEquipment(EquipmentList, payloadEquipmentList);
                        }
                    }
                    catch { } // Shape of object changed - nothing to do here!!
                }

                foreach (var equipment in EquipmentList)
                {
                    if (equipment.IsRetired)
                    {
                        TryAddEquipmentGroup("Retired");
                        break;
                    }
                }

                if (string.IsNullOrEmpty(Input!.Value))
                {
                    SelectedEquipment = null;
                    SelectedGroup = null;
                }
                else
                {
                    SelectedEquipment = WorkflowEquipmentBuilder.EquipmentFromList(EquipmentList, 0, null, Input.Value);
                    if (SelectedEquipment is null)
                    {
                        SelectedGroup = null;
                    }
                    else
                    {
                        EquipmentList.Remove(SelectedEquipment);
                        EquipmentList.Sort((a, b) => a.Make.CompareTo(b.Make));
                        EquipmentList.Insert(0, SelectedEquipment);
                        SelectedGroup = SelectedEquipment.EquipmentGroup;
                    }
                }

                if (StepDetails is not null)
                {
                    AllowAdd = StepDetails.AllowAdd;
                    AllowEdit = StepDetails.AllowEdit;
                    IgnoreAttributes = StepDetails.IgnoreAttributes;
                    SingleSelection = StepDetails.SingleSelection;
                }

                // Use Bluon Decoder if API Key is configured
                InitializeBluonServices();

                NextDisabled = !IsValid();

                Initialized = true;
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                Console.WriteLine(ex.Message);
            }
        }

        private void InitializeBluonServices()
        {
            if (bluonDecoder is null)
            {
                // Get API settings from settings
                string? apiKey = SettingsService.BluonApiKey ?? string.Empty;
                string? apiRoot = SettingsService.BluonApiRoot ?? string.Empty;
                string? userId = DataService?.AppState?.EmployeeId?.ToString() ?? "0";

                // Initialize Bluon Decoder Class
                if (!string.IsNullOrWhiteSpace(apiKey) && !string.IsNullOrWhiteSpace(apiRoot))
                {
                    bluonDecoder = new BluonAgeAndWarranty(apiKey, apiRoot, userId, DataService as IBluonCache);
                    bluonNameplateReader = new BluonNameplateReader(apiKey, apiRoot, userId, null);
                }
            }
            else
                bTriedBluon = true;
        }

        private async Task<bool> Confirm(string message, string heading = "Please Confirm", string okbtn = "Yes", string cancelbtn = "No")
        {
            if (string.IsNullOrEmpty(heading)) heading = "FYI";
            int ret = await DialogBox.WaitForDialogResultAsync(header: heading, prompt: message, okLabel: okbtn, cancelLabel: cancelbtn, defaultButton: 2);
            if (ret == 2)
                return false;
            else
                return true;
        }

        private async Task<bool> SaveImageAsync(ImageInsertInput image)
        {
            try
            {
                await DataService.SaveAndPostImageAsync(image);
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return false;
            }
            return true;
        }
    }
}