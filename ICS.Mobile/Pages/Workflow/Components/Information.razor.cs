namespace ICS.Mobile.Pages.Workflow.Components
{
    public partial class Information
    {
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

        protected override void ImageChanged()
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

        protected override void OnInitialized()
        {
            base.OnInitialized();

            NextDisabled = !IsValid();

            Initialized = true;
        }
    }
}
