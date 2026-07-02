using ICS.Portal.Data.Commands.Models;
using ICS.Portal.Data.Custom;
using ICS.Portal.Data.Images;
using ICS.Portal.Data.Queries.Models;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace ICS.Mobile.Pages.Workflow.Components
{
    public partial class ImageMedia
    {
        #region Fields and Parameters

        [Inject]
        protected IWorkflowData DataManager { set; get; } = default!;

        [Parameter, EditorRequired]
        public WorkflowResultDetailResult.Workflow? Workflow { set; get; } = null;

        [Parameter, EditorRequired]
        public WorkflowResultDetailResult.WorkflowStep? WorkflowStep { set; get; } = null;

        [Parameter, EditorRequired]
        public WorkflowStepResultValueSaveInput? Input { set; get; } = null;

        [Parameter, EditorRequired]
        public EventCallback ImageChanged { set; get; }

        [Parameter]
        public EventCallback<bool> OnWorkingStateChange { set; get; }

        [Parameter]
        public string? ImageLabel { set; get; }

        private ImageInsertInput? image = null;
        private bool Initialized { set; get; } = false;
        private long? _lastLoadedStepResultId = null;
        private bool _isLoadingFromServer = false;

        private string ImageLabelOrDefault
        {
            get
            {
                if (!string.IsNullOrEmpty(ImageLabel))
                {
                    return ImageLabel;
                }
                return "Take a Photo";
            }
        }
        private string RequirementText
        {
            get
            {
                if (WorkflowStep!.ImageRequirementId == 1)
                {
                    return "(Optional)";
                }

                if (WorkflowStep.ImageRequirementId == 2)
                {
                    return "(Required)";
                }

                return string.Empty;
            }
        }
        private string? ErrorMessage = null;
        public bool IsBusy { set; get; }

        private Mobile.Components.ICSDialogBox DialogBox;

        #endregion

        public async Task FileChanged(InputFileChangeEventArgs e)
        {
            ErrorMessage = null;
            await SetIsBusy(true);

            try
            {
                var fileName = e.File.Name;
                var contentType = e.File.ContentType ?? "";
                var extension = Path.GetExtension(fileName).ToLowerInvariant();

                // CHECK 1: Block by ContentType
                if (contentType.ToLowerInvariant().Contains("heic") ||
                    contentType.ToLowerInvariant().Contains("heif") ||
                    contentType.ToLowerInvariant().Contains("webp"))
                {
                    ErrorMessage = $"Format not supported (ContentType: {contentType}). Please change Camera Settings to 'Most Compatible' or switch from HEIC to JPG.";
                    return;
                }

                // CHECK 2: Block by extension
                var blockedExtensions = new[] { ".heic", ".heif", ".webp" };
                if (blockedExtensions.Contains(extension))
                {
                    ErrorMessage = $"Format not supported (Extension: {extension}). Please change Camera Settings to 'Most Compatible' or switch from HEIC to JPG.";
                    return;
                }

                // Read the raw file bytes to check magic bytes
                await using var stream = e.File.OpenReadStream(maxAllowedSize: 1024 * 1024 * 1024);
                using var ms = new MemoryStream();
                await stream.CopyToAsync(ms);
                var rawBytes = ms.ToArray();

                // CHECK 3: Magic bytes for HEIC
                if (IsHeicFile(rawBytes))
                {
                    ErrorMessage = "HEIC format detected. Please change Camera Settings to 'Most Compatible' or switch from HEIC to JPG.";
                    return;
                }

                // CHECK 4: Magic bytes for WebP
                if (IsWebPFile(rawBytes))
                {
                    ErrorMessage = "WebP format is not supported. Please convert to JPG or PNG.";
                    return;
                }

                // Process the file
                var imageExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp" };

                if (imageExtensions.Contains(extension))
                {
                    const int maxWidth = 720;
                    const int maxHeight = 1280;
                    var resizedFile = await e.File.RequestImageFileAsync(e.File.ContentType, maxWidth, maxHeight);

                    await using var resizedStream = resizedFile.OpenReadStream(maxAllowedSize: 1024 * 1024 * 1024);
                    using var resizedMs = new MemoryStream();
                    await resizedStream.CopyToAsync(resizedMs);
                    image!.Data = resizedMs.ToArray();
                }
                else
                {
                    image!.Data = rawBytes;
                }

                image.OriginalFileName = e.File.Name;
                image.ContentType = e.File.ContentType;

                await DataManager.SaveAndPostImageAsync(image!);
                Input!.ImageFileName = image.GeneratedFileName;

                await ImageChanged.InvokeAsync();
                StateHasChanged();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error uploading image: {ex.Message}";
            }
            finally
            {
                await SetIsBusy(false);
            }
        }

        private static bool IsHeicFile(byte[] data)
        {
            if (data.Length < 12) return false;

            bool hasFtyp = data[4] == 'f' && data[5] == 't' && data[6] == 'y' && data[7] == 'p';
            if (!hasFtyp) return false;

            string brand;
            try
            {
                brand = System.Text.Encoding.ASCII.GetString(data, 8, 4).ToLowerInvariant();
            }
            catch
            {
                return false;
            }

            var heicBrands = new[] { "heic", "heix", "hevc", "hevx", "mif1", "msf1", "heis", "hevs", "avif" };
            return heicBrands.Contains(brand);
        }

        private static bool IsWebPFile(byte[] data)
        {
            if (data.Length < 12) return false;

            return data[0] == 'R' && data[1] == 'I' && data[2] == 'F' && data[3] == 'F' &&
                   data[8] == 'W' && data[9] == 'E' && data[10] == 'B' && data[11] == 'P';
        }

        private static string GetDisplayFileName(ImageInsertInput input)
        {
            if (!string.IsNullOrEmpty(input.OriginalFileName))
                return input.OriginalFileName;
            
            if (!string.IsNullOrEmpty(input.ImageLabel))
                return input.ImageLabel;

            if (input.ContentType?.Contains("pdf") == true)
                return "Your Uploaded PDF File";
            
            if (input.ContentType?.Contains("video") == true)
                return "Your Uploaded Video";

            return "Your Uploaded File";
        }

        private async Task SetIsBusy(bool isBusy)
        {
            if (IsBusy != isBusy)
            {
                IsBusy = isBusy;

                if (OnWorkingStateChange.HasDelegate)
                {
                    await OnWorkingStateChange.InvokeAsync(isBusy);
                }
            }
        }

        private async Task DeleteImage(ImageInsertInput input)
        {
            if (await Confirm("Are you sure you want to delete this image?", "Checklist Photo"))
            {
                await DataManager.DeleteImageAsync(input);
                input.Data = null;
                input.OriginalFileName = null;
                input.ContentType = null;
                Input!.ImageFileName = null;
                await ImageChanged.InvokeAsync();
                StateHasChanged();
            }
        }

        public async Task LoadAsync()
        {
            if (WorkflowStep!.ImageRequirementId != 0)
            {
                image = new()
                {
                    WorkflowResultId = Workflow!.WorkflowResultId,
                    WorkflowStepResultId = WorkflowStep.WorkflowStepResultId,
                    WorkflowStepLoopIndex = Input!.LoopIndex,
                    ImageIndex = 0,
                    ImageLabel = WorkflowStep.ImageLabel
                };

                if (!string.IsNullOrEmpty(Input.ImageFileName))
                {
                    image.ParseContentType(Input.ImageFileName);
                    _isLoadingFromServer = true;
                    StateHasChanged();
                    try
                    {
                        await DataManager.HydrateImageAsync(image);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Failed to hydrate image: {ex.Message}");
                        ErrorMessage = $"Failed to load image: {ex.Message}";
                    }
                    finally
                    {
                        _isLoadingFromServer = false;
                        StateHasChanged();
                    }
                }
            }
        }

        protected override async Task OnParametersSetAsync()
        {
            await base.OnParametersSetAsync();

            var currentStepResultId = WorkflowStep?.WorkflowStepResultId;
            
            // Skip if already loading the same step (prevents duplicate loads)
            if (_isLoadingFromServer && _lastLoadedStepResultId == currentStepResultId)
            {
                return;
            }
            
            // Load if not initialized OR if the step changed (navigating back/forth)
            if (!Initialized || _lastLoadedStepResultId != currentStepResultId)
            {
                _lastLoadedStepResultId = currentStepResultId;
                await LoadAsync();
                Initialized = true;
                StateHasChanged();
            }
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
    }
}
