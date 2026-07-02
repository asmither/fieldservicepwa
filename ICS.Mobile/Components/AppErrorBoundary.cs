using ICS.Portal.Data.Custom;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace ICS.Mobile.Components
{
    /// <summary>
    /// ErrorBoundary that also records the crash in the app's own error log,
    /// so boundary screens are diagnosable from Settings > Error Messages
    /// instead of only the browser console.
    /// </summary>
    public class AppErrorBoundary : ErrorBoundary
    {
        [Inject]
        private IWorkflowData DataService { get; set; } = default!;

        [Inject]
        private NavigationManager Nav { get; set; } = default!;

        protected override async Task OnErrorAsync(Exception exception)
        {
            await base.OnErrorAsync(exception);
            try
            {
                var details = exception.ToString();
                if (details.Length > 800)
                {
                    details = details[..800];
                }

                await DataService.TryWriteError(details, $"UI crash: {new Uri(Nav.Uri).AbsolutePath}");
            }
            catch
            {
                // Never let error logging itself break the boundary.
            }
        }
    }
}
