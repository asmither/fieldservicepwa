using ICS.Mobile.Helpers;
using ICS.Portal.Data.Images;

using Microsoft.AspNetCore.Components;

namespace ICS.Mobile.Pages.Workflow.Components
{
    public partial class ImageToText
    {
        #region Fields

        private ImageInsertInput? image = null;
        private ElementReference signaturePad;
        private int height = 200;
        private int width = 340;
        private string WidthPixels
        {
            get
            {
                return $"{width}px";
            }
        }
        private string HeightPixels
        {
            get
            {
                return $"{height}px";
            }
        }

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

        [Inject]
        JSUI ui { set; get; } = default!;

        private async Task DeleteImageAsync()
        {
            await DataService.DeleteImageAsync(image!);
            image.Data = null;
            image.OriginalFileName = null;
            image.ContentType = null;
            Input!.Value = null;

            NextDisabled = !IsValid();
        }
        private async Task SaveImageAsync()
        {
            ChildComponentIsBusy = true;

            string imageString = await ui.GetSignaturePadData(signaturePad);
            string imageStringData = imageString.Substring(22);

            image.ContentType = "image/png";
            image.Data = Convert.FromBase64String(imageStringData);

            await DataService.SaveAndPostImageAsync(image);

            Input!.Value = image.GeneratedFileName;

            NextDisabled = !IsValid();

            ChildComponentIsBusy = false;
        }
        protected override async Task OnInitializedAsync()
        {
            base.OnInitialized();

            image = new()
            {
                WorkflowResultId = Input!.WorkflowResultId,
                WorkflowStepResultId = Input.WorkflowStepResultId,
                WorkflowStepLoopIndex = Input.LoopIndex,
                ImageIndex = 0,
                OriginalFileName = "Signature.png"
            };

            if (!string.IsNullOrEmpty(Input!.Value))
            {
                image.ParseContentType(Input.Value);
                await DataService.HydrateImageAsync(image);
            }

            width = (int)await ui.GetBodyWidth();
            height = ((int)(width * .55));

            NextDisabled = !IsValid();

            Initialized = true;



        }
        protected override bool IsValid()
        {
            ErrorMessage = null;

            if (string.IsNullOrEmpty(Input!.Value))
            {
                if (WorkflowStep!.IsRequired)
                {
                    return false;
                }
            }

            return base.IsValid();
        }
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await ui.SetSignaturePadElement();
        }
    }
}
