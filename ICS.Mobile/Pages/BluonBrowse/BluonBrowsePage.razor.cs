using ICS.Mobile.Pages.BluonBrowse.Components;
using ICS.Portal.Data.Commands.Models;
using ICS.Portal.Data.Custom;
using ICS.Mobile.Helpers;
using ICS.Portal.Data.Queries.Models;
using Microsoft.AspNetCore.Components;
using System.Text.Json;
using ICS.Mobile.Services.ServiceModels;
using Microsoft.JSInterop;
using ICS.Mobile.Services;
using System.Drawing;

namespace ICS.Mobile.Pages.BluonBrowse
{
    public partial class BluonBrowsePage : PageBase
    {
        [Inject]
        protected IJSRuntime JSRuntime { set; get; } = default!;


        #region Page Variablesf

        // View state
        private enum ViewMode
        {
            Home,
            Brands,
            Series,
            Models,
            ModelDetails,
            PartDetails,
            SearchResults
        }

        private ViewMode CurrentView = ViewMode.Home;
        private string SearchMode = "model"; // "part" or "model"
        private string SearchTerm = "";
        private string LastSearchTerm = "";
        private bool IsSearching = false;

        // Loading states
        private bool IsLoadingBrands = false;
        private bool IsLoadingSeries = false;
        private bool IsLoadingModels = false;
        private bool IsLoadingParts = false;
        private bool IsLoadingAlternatives = false;

        // Bluon API services
        private BluonBrands? bluonBrands;
        private BluonModels? bluonModels;
        private BluonParts? bluonParts;
        private BluonSerialDecoder? bluonDecoder;

        // Data collections
        private List<BluonBrands.Brands.Data>? AllBrands;
        private BluonBrands.Brands.Data? SelectedBrand;
        private List<BluonBrands.BrandSeries.Datum>? BrandSeries;
        private BluonBrands.BrandSeries.Datum? SelectedSeries;
        private List<BluonBrands.BrandModels.Datum>? BrandModels;
        private List<BluonBrands.BrandSeriesOems.Datum>? SeriesModels;
        private List<BluonBrands.BrandSeriesOems.Datum>? DisplayModels;
        private BluonBrands.BrandModels.Datum? SelectedModel;
        private List<BluonBrands.BrandPartsAndOem.Datum>? ModelParts;
        private BluonBrands.BrandPartsAndOem.Datum? SelectedPart;
        private List<BluonParts.PartAlternatives.Datum>? PartAlternatives;

        // Search results
        private List<BluonBrands.BrandPartsAndOem.Datum>? PartSearchResults;
        private List<BluonBrands.BrandSeriesOems.Datum>? ModelSearchResults;

        // Projects
        private List<MobileProject>? AllProjects = new List<MobileProject>();
        private MobileProject? CurrentProject;
        private bool ShowProjectModal = false;

        // Recent models
        private List<RecentModel> RecentModels = new List<RecentModel>();
        private bool ShowRecentModal = false;

        // Parts info
        private BluonParts.PartInfo.Root? SelectedPartInfo = null;
        private bool IsLoadingPartInfo = false;

        // Model documentation support
        private BluonModels.ModelManuals.Root? ModelManuals = null;
        private bool IsLoadingManuals = false;


        #endregion

        #region OnInit and LoadData

        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();
            
            InitializeBluonServices();
            await LoadData();

            Initialized = true;
        }

        private void InitializeBluonServices()
        {
            // Get API settings from settings
            string apiKey = SettingsService.BluonApiKey;
            string apiRoot = SettingsService.BluonApiRoot;
            string userId = $"{CurrentUserId}";

            bluonBrands = new BluonBrands(apiKey, apiRoot, userId, DataService as IBluonCache);
            bluonModels = new BluonModels(apiKey, apiRoot, userId, DataService as IBluonCache);
            bluonParts = new BluonParts(apiKey, apiRoot, userId, DataService as IBluonCache);
            bluonDecoder = new BluonSerialDecoder(apiKey, apiRoot, userId, DataService as IBluonCache);
        }

        public async Task LoadData()
        {
            await LoadSavedState();
            await LoadBrands();
        }

        #endregion

        #region Data Loading Methods

        private async Task LoadBrands()
        {
            if (AllBrands != null) return; // Already loaded

            IsLoadingBrands = true;
            StateHasChanged();

            try
            {
                AllBrands = await bluonBrands!.GetAllBrands(50);
            }
            catch (Exception ex)
            {
                await ShowError($"Error loading brands: {ex.Message}");
            }
            finally
            {
                IsLoadingBrands = false;
                StateHasChanged();
            }
        }

        private async Task LoadSeriesAndModels()
        {
            if (SelectedBrand == null) return;

            IsLoadingSeries = true;
            StateHasChanged();

            try
            {
                var seriesTask = bluonBrands!.GetAllBrandSeries(SelectedBrand.Id, 50);
                var modelsTask = bluonBrands!.GetAllBrandModels(SelectedBrand.Id, 50);

                await Task.WhenAll(seriesTask, modelsTask);

                BrandSeries = await seriesTask;
                BrandModels = await modelsTask;
            }
            catch (Exception ex)
            {
                await ShowError($"Error loading series: {ex.Message}");
            }
            finally
            {
                IsLoadingSeries = false;
                StateHasChanged();
            }
        }

        private async Task LoadSeriesModels()
        {
            if (SelectedBrand == null || SelectedSeries == null) return;

            IsLoadingModels = true;
            StateHasChanged();

            try
            {
                SeriesModels = await bluonBrands!.GetAllBrandSeriesOems(
                    SelectedBrand.Id, SelectedSeries.Id, 50);
                DisplayModels = SeriesModels;
            }
            catch (Exception ex)
            {
                await ShowError($"Error loading models: {ex.Message}");
            }
            finally
            {
                IsLoadingModels = false;
                StateHasChanged();
            }
        }

        private async Task LoadModelParts()
        {
            if (SelectedBrand == null || SelectedModel == null) return;

            IsLoadingParts = true;
            StateHasChanged();

            try
            {
                ModelParts = await bluonBrands!.GetAllBrandOemParts(
                    SelectedBrand.Id, SelectedModel.Id, 50);
            }
            catch (Exception ex)
            {
                await ShowError($"Error loading parts: {ex.Message}");
            }
            finally
            {
                IsLoadingParts = false;
                StateHasChanged();
            }
        }

        private async Task LoadPartAlternatives()
        {
            if (SelectedPart == null) return;

            IsLoadingAlternatives = true;
            StateHasChanged();

            try
            {
                PartAlternatives = await bluonParts!.GetAllPartAlternatives(SelectedPart.Id, 50);
            }
            catch (Exception ex)
            {
                await ShowError($"Error loading alternatives: {ex.Message}");
            }
            finally
            {
                IsLoadingAlternatives = false;
                StateHasChanged();
            }
        }

        #endregion

        #region Navigation Methods

        private async Task NavigateToHome()
        {
            CurrentView = ViewMode.Brands;
            SelectedBrand = null;
            SelectedSeries = null;
            SelectedModel = null;
            SelectedPart = null;
            await SaveState();
        }

        private async Task NavigateToBrand()
        {
            if (SelectedBrand == null) return;
            CurrentView = ViewMode.Series;
            SelectedSeries = null;
            SelectedModel = null;
            SelectedPart = null;
            await SaveState();
        }

        private async Task NavigateToSeries()
        {
            if (SelectedSeries == null) return;
            CurrentView = ViewMode.Models;
            SelectedModel = null;
            SelectedPart = null;
            await SaveState();
        }

        #endregion

        #region Selection Handlers

        private async Task SelectBrand(BluonBrands.Brands.Data brand)
        {
            SelectedBrand = brand;
            CurrentView = ViewMode.Series;
            await LoadSeriesAndModels();
            await SaveState();
        }

        private async Task SelectSeries(BluonBrands.BrandSeries.Datum series)
        {
            SelectedSeries = series;
            CurrentView = ViewMode.Models;
            await LoadSeriesModels();
            await SaveState();
        }

        private async Task ShowAllBrandModels()
        {
            SelectedSeries = null;
            CurrentView = ViewMode.Models;
            DisplayModels = BrandModels?.Select(m => new BluonBrands.BrandSeriesOems.Datum
            {
                Id = m.Id,
                Model = m.Model,
                ModelNotes = m.ModelNotes,
                BrandName = m.BrandName,
                SystemType = m.SystemType,
                FunctionalPartsCount = m.FunctionalPartsCount,
                ManualsCount = m.ManualsCount,
                Image = m.Image
            }).ToList();
            await SaveState();
        }

        private async Task SelectModel(BluonBrands.BrandSeriesOems.Datum model)
        {
            // Convert BrandSeriesOems.Datum to BrandModels.Datum (like your existing code does)
            SelectedModel = new BluonBrands.BrandModels.Datum
            {
                Id = model.Id,
                Model = model.Model,
                ModelNotes = model.ModelNotes,
                BrandName = model.BrandName,
                SystemType = model.SystemType,
                FunctionalPartsCount = model.FunctionalPartsCount,
                ManualsCount = model.ManualsCount,
                Image = model.Image
            };

            CurrentView = ViewMode.ModelDetails;

            // Clear previous data
            ModelParts = null;
            ModelManuals = null;

            // Add to recent models (convert to BrandModels.Datum for this call)
            AddToRecentModels(SelectedModel);
            await SaveState();

            // Load both parts and manuals (do these sequentially for mobile optimization)
            await LoadModelParts();
            await LoadModelManuals();
        }

        private async Task SelectPart(BluonBrands.BrandPartsAndOem.Datum part)
        {
            SelectedPart = part;
            CurrentView = ViewMode.PartDetails;

            // Clear previous data
            PartAlternatives = null;
            SelectedPartInfo = null;

            await SaveState();

            // Load both part info and alternatives sequentially
            await LoadPartInfo();
            await LoadPartAlternatives();
        }

        #endregion

        #region Search Methods

        private void SetSearchMode(string mode)
        {
            SearchMode = mode;
            SearchTerm = "";
        }

        private async Task PerformSearch()
        {
            if (string.IsNullOrWhiteSpace(SearchTerm)) return;

            IsSearching = true;
            LastSearchTerm = SearchTerm.Trim();
            StateHasChanged();

            try
            {
                if (SearchMode == "part")
                {
                    var result = await bluonParts!.SearchByPart(LastSearchTerm, 1, 50);
                    PartSearchResults = result?.Data?.Select(p => new BluonBrands.BrandPartsAndOem.Datum
                    {
                        Id = p.Id,
                        Number = p.Number,
                        Type = p.Type,
                        Subtype = p.Subtype,
                        Description = p.Description,
                        Brand = p.Brand,
                        ReplacementsCount = p.ReplacementsCount
                    }).ToList();
                }
                else
                {
                    var result = await bluonModels!.ModelNameSearch(LastSearchTerm, 1, 50);
                    ModelSearchResults = result?.Data?.Select(m => new BluonBrands.BrandSeriesOems.Datum
                    {
                        Id = m.Id,
                        Model = m.Model,
                        BrandName = m.BrandName,
                        SystemType = m.SystemType,
                        FunctionalPartsCount = m.FunctionalPartsCount ?? 0,
                        ManualsCount = m.ManualsCount ?? 0
                    }).ToList();
                }

                CurrentView = ViewMode.SearchResults;
            }
            catch (Exception ex)
            {
                await ShowError($"Search error: {ex.Message}");
            }
            finally
            {
                IsSearching = false;
                StateHasChanged();
            }
        }

        private async Task SelectPartFromSearch(BluonBrands.BrandPartsAndOem.Datum part)
        {
            SelectedPart = part;
            CurrentView = ViewMode.PartDetails;
            await LoadPartAlternatives();
        }

        private async Task SelectModelFromSearch(BluonBrands.BrandSeriesOems.Datum model)
        {
            // Find the brand
            if (AllBrands != null && !string.IsNullOrEmpty(model.BrandName))
            {
                SelectedBrand = AllBrands.FirstOrDefault(b =>
                    string.Equals(b.Brand, model.BrandName, StringComparison.OrdinalIgnoreCase));
            }

            await SelectModel(model);
        }

        #endregion

        #region Project Management

        private void ShowProjects()
        {
            ShowProjectModal = true;
        }

        private async Task SelectProject(MobileProject project)
        {
            CurrentProject = project;
            ShowProjectModal = false;
            await SaveState();
        }

        private async Task CreateProject(MobileProject project)
        {
            project.Id = Guid.NewGuid().ToString();
            project.CreatedDate = DateTime.Now;
            AllProjects.Add(project);
            CurrentProject = project;
            await SaveProjects();
            ShowProjectModal = false;
        }

        private async Task AddPartToProject(BluonBrands.BrandPartsAndOem.Datum part)
        {
            if (CurrentProject == null)
            {
                // Create quick project
                CurrentProject = new MobileProject
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = $"Quick Project {DateTime.Now:MM/dd}",
                    CreatedDate = DateTime.Now,
                    Parts = new List<MobileProjectPart>()
                };
                AllProjects.Add(CurrentProject);
            }

            var projectPart = new MobileProjectPart
            {
                PartId = part.Id,
                PartNumber = part.Number,
                Description = $"{part.Description} {part.Subcategory}",
                Brand = part.Brand,
                Type = part.Type,
                Quantity = 1
            };

            var existing = CurrentProject.Parts.FirstOrDefault(p => p.PartId == part.Id);
            if (existing != null)
            {
                existing.Quantity++;
            }
            else
            {
                CurrentProject.Parts.Add(projectPart);
            }


            // passing in string topic to find parts topic, 
            // then get guid back for real parts area or default to NULL to create item in current active topic
            string topicId = await ScratchPadSvc.FindTopicIdAsync("Parts", true, "⚙️", "#7CB13C");

            // set focus to parts topic            
            if (!string.IsNullOrEmpty(topicId))
                ScratchPadSvc.ActiveTopicId = topicId;

            var item = await ScratchPadSvc.AddItemAsync(topicId, $"{projectPart.Brand} {projectPart.Type} {projectPart.PartNumber} - {projectPart.Description}", ScratchPadItemType.Note);


            // then get guid back for real parts area or default to NULL to create item in current active topic



            ScratchPadSvc.Show();
            //await SaveProjects();
            await ShowToast($"Added {part.Number} to ScratchPad");


        }

        #endregion

        #region Documentation Methods

        private async Task LoadModelManuals()
        {
            if (SelectedModel == null) return;

            IsLoadingManuals = true;
            StateHasChanged();

            try
            {
                ModelManuals = await bluonModels!.GetManualsForModel(SelectedModel.Id);
            }
            catch (Exception ex)
            {
                await ShowError($"Error loading model manuals: {ex.Message}");
            }
            finally
            {
                IsLoadingManuals = false;
                StateHasChanged();
            }
        }

        private async Task OpenDocument(string documentUrl)
        {
            try
            {
                // Open the document in a new tab/window  
                await JSRuntime.InvokeAsync<object>("open", new object[] { documentUrl, "_blank" });
            }
            catch (Exception ex)
            {
                await ShowError($"Error opening document: {ex.Message}");
            }
        }

        #endregion

        #region Recent Models

        private void ShowRecent()
        {
            ShowRecentModal = true;
        }

        private void AddToRecentModels(BluonBrands.BrandModels.Datum model)
        {
            var recent = new RecentModel
            {
                Id = model.Id,
                Model = model.Model,
                BrandName = model.BrandName,
                SystemType = model.SystemType,
                ViewedDate = DateTime.Now
            };

            // Remove if already exists
            RecentModels.RemoveAll(r => r.Id == model.Id);

            // Add to beginning
            RecentModels.Insert(0, recent);

            // Keep only last 10
            if (RecentModels.Count > 10)
            {
                RecentModels = RecentModels.Take(10).ToList();
            }

            SaveRecentModels();
        }

        private async Task SelectRecentModel(RecentModel recent)
        {
            ShowRecentModal = false;

            // Find and set the brand
            if (AllBrands != null && !string.IsNullOrEmpty(recent.BrandName))
            {
                SelectedBrand = AllBrands.FirstOrDefault(b =>
                    string.Equals(b.Brand, recent.BrandName, StringComparison.OrdinalIgnoreCase));

                if (SelectedBrand != null)
                {
                    await LoadSeriesAndModels();
                }
            }

            var model = new BluonBrands.BrandSeriesOems.Datum
            {
                Id = recent.Id,
                Model = recent.Model,
                BrandName = recent.BrandName,
                SystemType = recent.SystemType
            };

            await SelectModel(model);
        }

        #endregion

        #region State Management

        private async Task SaveState()
        {
            try
            {
                DataService.AppState.BluonProjects = AllProjects != null ? JsonSerializer.Serialize(AllProjects) : null;
                DataService.AppState.BluonCurrentView = CurrentView.ToString();

                DataService.AppState.BluonView=SearchMode;

                if (SelectedBrand != null)
                    DataService.AppState.BluonView = SelectedBrand.Id;
                if (SelectedSeries != null)
                    DataService.AppState.BluonView = SelectedSeries.Id.ToString();
                if (SelectedModel != null)
                    DataService.AppState.BluonView = SelectedModel.Id;

                await DataService.SaveAppStateInstance();
            }
            catch { }
        }

        private async Task LoadSavedState()
        {
            try
            {
                // Load projects

                var projectsJson = DataService.AppState.BluonProjects;
                if (!string.IsNullOrEmpty(projectsJson))
                {
                    AllProjects = JsonSerializer.Deserialize<List<MobileProject>>(projectsJson) ?? new List<MobileProject>();
                }

                // Load current project
                var currentProjectId = DataService.AppState.BluonCurrentProjectId;
                if (!string.IsNullOrEmpty(currentProjectId) && AllProjects is not null)
                {
                    CurrentProject = AllProjects.FirstOrDefault(p => p.Id == currentProjectId);
                }

                // Load recent models
                var recentJson = DataService.AppState.BluonRecentModels;
                if (!string.IsNullOrEmpty(recentJson))
                {
                    RecentModels = JsonSerializer.Deserialize<List<RecentModel>>(recentJson) ?? new List<RecentModel>();
                }
            }
            catch { }
        }

        private async Task SaveProjects()
        {
            try
            {
                var json = JsonSerializer.Serialize(AllProjects);
                DataService.AppState.BluonView = json;

                if (CurrentProject != null)
                {
                    DataService.AppState.BluonView = CurrentProject.Id;
                }

                await DataService.SaveAppStateInstance();
            }
            catch { }
        }

        private async void SaveRecentModels()
        {
            try
            {
                var json = JsonSerializer.Serialize(RecentModels);
                DataService.AppState.BluonView = json;
                
                await DataService.SaveAppStateInstance();

            }
            catch { }
        }

        #endregion

        #region Enhanced Part Loading Methods

        private async Task LoadPartInfo()
        {
            if (SelectedPart == null) return;

            IsLoadingPartInfo = true;
            StateHasChanged();

            try
            {
                SelectedPartInfo = await bluonParts!.GetPartInfo(SelectedPart.Id);
            }
            catch (Exception ex)
            {
                await ShowError($"Error loading part information: {ex.Message}");
            }
            finally
            {
                IsLoadingPartInfo = false;
                StateHasChanged();
            }
        }

        #endregion

        #region Helper Methods

        public void HomePageClick()
        {
            PageNavManager.NavigateTo("/");
        }

        private async Task ShowError(string message)
        {
            // TODO: Implement error display
            Console.WriteLine($"Error: {message}");
        }

        private async Task ShowToast(string message)
        {
            // TODO: Implement toast notification
            Console.WriteLine($"Toast: {message}");
        }

        #endregion
    }

    #region Data Models

    public class MobileProject
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string? CustomerName { get; set; }
        public DateTime CreatedDate { get; set; }
        public List<MobileProjectPart> Parts { get; set; } = new();
    }

    public class MobileProjectPart
    {
        public string PartId { get; set; } = "";
        public string? PartNumber { get; set; }
        public string? Description { get; set; }
        public string? Brand { get; set; }
        public string? Type { get; set; }
        public int Quantity { get; set; } = 1;
    }

    public class RecentModel
    {
        public string Id { get; set; } = "";
        public string Model { get; set; } = "";
        public string BrandName { get; set; } = "";
        public string SystemType { get; set; } = "";
        public DateTime ViewedDate { get; set; }
    }

    #endregion
}