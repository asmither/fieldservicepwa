using ICS.Mobile.Helpers;
using ICS.Portal.Data.Queries.Models;
using Microsoft.AspNetCore.Components;

namespace ICS.Mobile.Pages.Workflow.Components
{
    public partial class CompleteSync
    {
        [Parameter]
        public EventCallback OnDeleteLocalCopy { set; get; }

        [Inject]
        private NavigationManager nm { set; get; } = default!;


        private async Task DeleteLocalCopy()
        {
            await OnDeleteLocalCopy.InvokeAsync();
        }

        private void GotoActivities(int d, string url = "/dispatch-detail")
        {
            nm.NavigateTo(Workflow!.RequireDispatch ? "/dispatch-detail" : "/");
            //if(Workflow.RequireDispatch)
            //{
            //    nm.NavigateTo("/dispatch-list");
            //}
            //else
            //{

            //}
            //// Button function to go back to the dispatch activities defaulting to THIS dispatch, 
            //// and forcing a dispatch server refresh (if posslibe) to pick up any complete settings.
            //// Note: this is async, even without an awaitand will 
            ////       navigate way and let Dispose still fire and onclick and stuff unwind right.

            //if (string.IsNullOrEmpty(url)) d = 0;

            //if (d != 0)
            //{
            //    if (d > 0) url = $"{url}/{d.ToString()}";
            //    nm.NavigateTo(url);
            //}
            //else
            //{
            //    // If no dispatch id, then just go to the dispatch list page.
            //    url = "/";
            //    nm.NavigateTo(url);
            //}
            

        }

        protected override void OnInitialized()
        {
            DataService.OnSyncCountChanged += OnSyncCountChanged;
            Initialized = true;
        }

        private void OnSyncCountChanged(int count)
        {
            StateHasChanged();
        }

        public void Dispose()
        {

            DataService.OnSyncCountChanged -= OnSyncCountChanged;
        }
    }
}
