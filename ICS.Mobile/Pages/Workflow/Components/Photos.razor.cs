using ICS.Portal.Data.Images;

using Microsoft.AspNetCore.Components.Forms;

namespace ICS.Mobile.Pages.Workflow.Components
{
    public partial class Photos
    {
        #region Fields

        private int min = 0;
        private int max = 0;
        private List<ImageInsertInput>? Images;
        private ImageInsertInput? CurrentImage = null;
        private Mobile.Components.ICSDialogBox? DialogBox;
        private long? _lastLoadedStepResultId = null;
        private bool _isLoading = false;

        #endregion Fields

        #region Callbacks

        protected override void NoteChanged()
        {
            if (IsValid())
            {
                NextDisabled = false;
            }
            else
            {
                NextDisabled = true;
            }
        }

        #endregion Callbacks

        private bool Display(ImageInsertInput image)
        {
            if (image.Data is not null)
            {
                return true;
            }
            else
            {
                for (int fileIndex = 0; fileIndex < Images.IndexOf(image); fileIndex++)
                {
                    if (Images[fileIndex].Data is null)
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        private async Task DeleteImage(long imageId)
        {
            if (Images is not null)
            {
                if (await Confirm("Are you sure you want to delete this photo?"))
                {
                    var existingFile = Images.FirstOrDefault(i => i.Id == imageId);

                    if (existingFile is not null)
                    {
                        await DataService.DeleteImageAsync(existingFile);

                        existingFile.Data = null;
                        existingFile.OriginalFileName = null;
                        existingFile.ContentType = null;
                    }
                }

                Input!.Value = GetImagesValue();

                NextDisabled = !IsValid();
                StateHasChanged();
            }
        }

        public async Task FileChanged(InputFileChangeEventArgs e, long imageId)
        {
            ChildComponentIsBusy = true;
            ErrorMessage = null;
            NextDisabled = true;

            try
            {
                CurrentImage = Images!.FirstOrDefault(i => i.Id == imageId);

                if (CurrentImage is null)
                {
                    ErrorMessage = "No image slot selected";
                    return;
                }

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
                    CurrentImage.Data = resizedMs.ToArray();
                }
                else
                {
                    CurrentImage.Data = rawBytes;
                }

                CurrentImage.ContentType = e.File.ContentType;
                CurrentImage.OriginalFileName = e.File.Name;

                await DataService.SaveAndPostImageAsync(CurrentImage);
                Input!.Value = GetImagesValue();
                CurrentImage = null;
                StateHasChanged();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error uploading photo: {ex.Message}";
            }
            finally
            {
                NextDisabled = !IsValid();
                ChildComponentIsBusy = false;
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

        private string GetImagesValue()
        {
            List<string> fileNames = new List<string>();
            foreach (ImageInsertInput i in Images!)
            {
                if (i.ContentType is not null)
                {
                    fileNames.Add(i.GeneratedFileName);
                }
            }

            return string.Join(',', fileNames);
        }

        protected override bool IsValid()
        {
            if (WorkflowStep!.IsRequired)
            {
                int actualCount = 0;

                foreach (ImageInsertInput input in Images!)
                {
                    if (input.Data is not null)
                    {
                        actualCount++;
                    }
                }

                if (actualCount < min)
                {
                    return false;
                }
            }

            return base.IsValid();
        }

        protected override async Task OnParametersSetAsync()
        {
            var currentStepResultId = WorkflowStep?.WorkflowStepResultId;
            
            // Skip if already loading the same step (prevents duplicate loads)
            if (_isLoading && _lastLoadedStepResultId == currentStepResultId)
            {
                return;
            }
            
            // Only reload if not initialized OR if the step changed (navigating back/forth)
            if (!Initialized || _lastLoadedStepResultId != currentStepResultId)
            {
                _lastLoadedStepResultId = currentStepResultId;
                _isLoading = true;
                StateHasChanged();
                
                try
                {
                    min = (int)StepDetails.MinValue.Value;
                    max = (int)StepDetails.MaxValue.Value;
                    Images = new List<ImageInsertInput>();

                    for (int idx = 0; idx != max; idx++)
                    {
                        bool required = idx <= (min - 1);

                        Images.Add(new()
                        {
                            WorkflowResultId = WorkflowStep!.WorkflowResultId,
                            WorkflowStepResultId = WorkflowStep.WorkflowStepResultId!,
                            WorkflowStepLoopIndex = Input.LoopIndex,
                            ImageIndex = idx,
                            Required = required,
                            ImageLabel = $"Photo {idx + 1}"
                        });
                    }

                    if (!string.IsNullOrEmpty(StepDetails.Labels))
                    {
                        string[] labels = StepDetails.Labels.Split('~', StringSplitOptions.None);
                        for (int idx = 0; idx != max; idx++)
                        {
                            if (labels.Length > idx)
                            {
                                if (!string.IsNullOrEmpty(labels[idx]))
                                {
                                    Images[idx].ImageLabel = labels[idx];
                                }
                            }
                        }
                    }

                    if (!string.IsNullOrEmpty(Input!.Value))
                    {
                        List<string> fileNames = Input.Value.Split(',').ToList();

                        foreach (var input in Images)
                        {
                            foreach (string fileName in fileNames)
                            {
                                if (fileName.StartsWith(input.GeneratedFileNameWithoutExtension))
                                {
                                    input.ParseContentType(fileName);
                                    try
                                    {
                                        await DataService.HydrateImageAsync(input);
                                    }
                                    catch (Exception ex)
                                    {
                                        // Log but continue loading other images
                                        System.Diagnostics.Debug.WriteLine($"Failed to hydrate image {input.ImageIndex}: {ex.Message}");
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    ErrorMessage = ex.Message;
                }
                finally
                {
                    _isLoading = false;
                    NextDisabled = !IsValid();
                    Initialized = true;
                    StateHasChanged();
                }
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
