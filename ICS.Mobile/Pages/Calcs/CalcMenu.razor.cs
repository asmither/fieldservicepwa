namespace ICS.Mobile.Pages.CalcMenu
{
    public partial class CalcMenuPage : PageBase
    {
        #region Page Variables
        
        private string UserName = string.Empty;
        
        #endregion
        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();

            UserName = DataService.AppState.UserName!;
            Initialized = true;

        }
    }
}
