//using ICS.Mobile.Components.ScratchPad;
using ICS.Mobile.Helpers;
using ICS.Mobile.Services;
using ICS.Mobile.Services.ServiceModels;
using ICS.Portal.Data.Custom;
using ICS.Portal.Data.Queries.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Drawing;

namespace ICS.Mobile.Pages.CompanyDirectory
{
    public partial class CompanyDirectoryPage : PageBase
    {
        [Inject]
        protected NavigationManager navManager { set; get; } = default!;

        [Inject]
        public IWorkflowData dataManager { set; get; } = default!;

        [Inject]
        private JSUI ui { set; get; } = default!;

        [Inject]
        private IJSRuntime JS { get; set; } = default!;

        private ElementReference InputControl;  // search box

        private List<CompanyDirectoryResult>? DirectoryAll;
        private List<CompanyDirectoryResult>? DirectoryFiltered;

        // Property to control alphabet navigation visibility
        private bool ShowAlphabetNav = true;

        private string? ActiveLetter = null;

        private static readonly string[] AvailableLetters =
            "ABCDEFGHIJKLMNOPQRSTUVWXYZ".ToCharArray().Select(c => c.ToString()).ToArray();

        public Dictionary<string, bool> Links = new Dictionary<string, bool>
        {
            {"a", false },
            {"b", false },
            {"c", false },
            {"d", false },
            {"e", false },
            {"f", false },
            {"g", false },
            {"h", false },
            {"i", false },
            {"j", false },
            {"k", false },
            {"l", false },
            {"m", false },
            {"n", false },
            {"o", false },
            {"p", false },
            {"q", false },
            {"r", false },
            {"s", false },
            {"t", false },
            {"u", false },
            {"v", false },
            {"w", false },
            {"x", false },
            {"y", false },
            {"z", false }
        };

        private async Task CopyToClipboard(CompanyDirectoryResult employee)
        {
            var text = $"{employee.FirstName} {employee.LastName}";
            if (!string.IsNullOrEmpty(employee.Phone))
                text += $"\n{ICS.Portal.Data.Custom.GeneralFunctions.FormatPhoneNumber(employee.Phone)}";
            if (!string.IsNullOrEmpty(employee.Email))
                text += $"\n{employee.Email}";

            //try
            //{
            //    // Copy to user's clipboard
            //    await ScratchPadSvc.CopyToClipboardAsync(text);

            //    // passing in string topic to find parts topic, 
            //    string topicId = await ScratchPadSvc.FindTopicIdAsync("Contacts", true, "👤", "#3C7CB1");

            //    // set focus to parts topic            
            //    //if (!string.IsNullOrEmpty(topicId))
            //    //ScratchPadSvc.ActiveTopicId = topicId;

            //    var item = await ScratchPadSvc.AddItemAsync(topicId, text, ScratchPadItemType.Note);


            //    // then get guid back for real parts area or default to NULL to create item in current active topic


            //    scratchPadRef?.ShowToast("Copied & added to ScratchPad");

            //    //ScratchPadSvc.Show();
            //}
            //catch
            //{
            //    // Fallback or silent fail for older browsers
            //    scratchPadRef?.ShowToast("Error Copying To Clipbard");
            //}
        }

        private void OnCloseClick()
        {
            navManager.NavigateTo("/");
        }

        private void ReshowAlphaNav()
        {
            FilterReset();
            ShowAlphabetNav = true;
        }

        private void FilterReset()
        {
            SearchValue = "";
            ActiveLetter = null;
            DirectoryFiltered = DirectoryAll;
        }

        private async Task FilterSearch(string SpecialSearchValue)
        {

            if (string.IsNullOrEmpty(SpecialSearchValue))
            {
                SearchValue = "";
                ActiveLetter = null;
                FilterReset();
                return;
            }


            if (!string.IsNullOrEmpty(SpecialSearchValue))
            {


                ActiveLetter = SpecialSearchValue;
                SearchValue = SpecialSearchValue;
                FilterSearch();

                // Wait a bit for the DOM to update, then scroll to the first item with that letter
                await Task.Delay(50);

                // Find the first employee with this letter
                var firstEmployee = DirectoryFiltered?.FirstOrDefault(e =>
                    e.LastName.StartsWith(SpecialSearchValue, StringComparison.InvariantCultureIgnoreCase));

                if (firstEmployee?.LinkLetter != null)
                {
                    try
                    {
                        await JS.InvokeVoidAsync("scrollToElement", firstEmployee.LinkLetter);
                    }
                    catch (Exception ex)
                    {
                        // Log error if needed
                        Console.WriteLine($"Error scrolling to element: {ex.Message}");
                    }
                }

            }
        }
        private bool HasDudesForLetter(string letter)
        {
            if (DirectoryAll == null) return false;

            if (letter == "#")
            {
                return DirectoryAll.Any(b => !string.IsNullOrEmpty(b.LastName) && !char.IsLetter(b.LastName[0]));
            }

            return DirectoryAll.Any(b => b.LastName?.StartsWith(letter, StringComparison.OrdinalIgnoreCase) ?? false);
        }
        private void FilterSearch()
        {
            if (string.IsNullOrEmpty(SearchValue))
            {
                FilterReset();
                return;
            }


            if (SearchValue.Length < 3)
            {
                var searchTerms = SearchValue.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                DirectoryFiltered = DirectoryAll.Where(c =>
                    searchTerms.Any(term => c.LastName.StartsWith(term, StringComparison.InvariantCultureIgnoreCase))
                ).ToList();
            }
            else
            {
                // split the search term into words and AND each word allowing multiple results.  From DataPicker
                var searchTerms = SearchValue.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                DirectoryFiltered = DirectoryAll.Where(c =>
                       searchTerms.Any(term => c.FirstName.Contains(term, StringComparison.InvariantCultureIgnoreCase))
                    || searchTerms.Any(term => c.LastName.Contains(term, StringComparison.InvariantCultureIgnoreCase))
                    || searchTerms.Any(term => c.City.Contains(term, StringComparison.InvariantCultureIgnoreCase))
                    || searchTerms.Any(term => c.State.Contains(term, StringComparison.InvariantCultureIgnoreCase))
                    || searchTerms.Any(term => c.ZoneName.Contains(term, StringComparison.InvariantCultureIgnoreCase))
                ).ToList();
            }
            ShowAlphabetNav = false;
            StateHasChanged();
        }

        private string _SeachValue = "";
        private string SearchValue
        {
            set
            {
                _SeachValue = value;
            }
            get
            {
                return _SeachValue;
            }
        }

        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();
            await GetCompanyDirectory();
            // Fix title of Dispatch Page
            Initialized = true;
            StateHasChanged();
        }
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                try
                {
                    await JS.InvokeVoidAsync("initLetterStripDrag");
                }
                catch { }

            }
        }
        private async Task GetCompanyDirectory()
        {
            LoadCompanyDirectoryData(await DataService.GetCompanyDirectoryAsync((e) =>
            {
                LoadCompanyDirectoryData(e);
                StateHasChanged();
            }));
        }

        private void LoadCompanyDirectoryData(List<CompanyDirectoryResult> data)
        {
            DirectoryFiltered = new();

            DirectoryAll = data;
            if (DirectoryAll is not null && DirectoryAll.Any())
            {
                DirectoryAll.Sort((a, b) => a.LastName.CompareTo(b.LastName));

                // if contractor, only show Service Managers
                if (DataService.AppState.IsContractor)
                {
                    // remove all names in DirectoryAll except for ones with title "Service Manager" 
                    DirectoryAll = [.. DirectoryAll.Where(e => e.Title == "Service Manager")];
                }

                string? lastLetter = null;
                foreach (var employee in DirectoryAll)
                {
                    string firstLetter = employee.LastName[..1].ToLower();
                    if (firstLetter != lastLetter)
                    {
                        Links[firstLetter] = true;
                        employee.LinkLetter = firstLetter;
                        lastLetter = firstLetter;
                    }
                }

                for (int idx = 0; idx != DirectoryAll.Count; idx++)
                {
                    if (DirectoryAll[idx].LinkLetter is null)
                    {
                        DirectoryAll[idx].LinkLetter = $"e-{idx}";
                    }
                }

                DirectoryFiltered = DirectoryAll;
            }
        }
    }
}

namespace ICS.Portal.Data.Queries.Models
{
    public partial class CompanyDirectoryResult
    {
        public string? LinkLetter { set; get; }
    }
}