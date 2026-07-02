using Microsoft.JSInterop;
using System;
using System.Threading.Tasks;


namespace ICS.Mobile.Services
{

    public interface IUpdateService
    {
        Task<bool> CheckForUpdateAsync();
        Task ForceUpdateAsync();
        Task<bool> SoftUpdateAsync();
        Task ClearCacheAsync();
        Task RegisterAutoUpdateCheckAsync();
        ValueTask DisposeAsync();
    }

    public class UpdateService : IUpdateService, IAsyncDisposable
    {
        private readonly IJSRuntime _jsRuntime;
        private IJSObjectReference? _updateManager;
        private DotNetObjectReference<UpdateService>? _dotNetRef;

        public UpdateService(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
        }

        private async Task EnsureUpdateManagerAsync()
        {
            if (_updateManager == null)
            {
                _updateManager = await _jsRuntime.InvokeAsync<IJSObjectReference>("eval", "window.UpdateManager");
            }
        }

        public async Task<bool> CheckForUpdateAsync()
        {
            try
            {
                await EnsureUpdateManagerAsync();
                var result = await _updateManager!.InvokeAsync<UpdateCheckResult>("checkForUpdate");

                if (!string.IsNullOrEmpty(result.Error))
                {
                    Console.WriteLine($"Update check error: {result.Error}");
                }

                return result.HasUpdate;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error checking for update: {ex.Message}");
                return false;
            }
        }

        public async Task ForceUpdateAsync()
        {
            try
            {
                await EnsureUpdateManagerAsync();
                await _updateManager!.InvokeVoidAsync("forceUpdate");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error forcing update: {ex.Message}");
                // Even if there's an error, try to reload
                await _jsRuntime.InvokeVoidAsync("location.reload", true);
            }
        }

        public async Task<bool> SoftUpdateAsync()
        {
            try
            {
                await EnsureUpdateManagerAsync();
                return await _updateManager!.InvokeAsync<bool>("softUpdate");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during soft update: {ex.Message}");
                return false;
            }
        }

        public async Task ClearCacheAsync()
        {
            try
            {
                await EnsureUpdateManagerAsync();
                await _updateManager!.InvokeVoidAsync("clearServiceWorkerCache");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error clearing cache: {ex.Message}");
            }
        }

        public async Task RegisterAutoUpdateCheckAsync()
        {
            try
            {
                await EnsureUpdateManagerAsync();
                _dotNetRef = DotNetObjectReference.Create(this);
                await _updateManager!.InvokeVoidAsync("registerAutoUpdateCheck", _dotNetRef);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error registering auto update check: {ex.Message}");
            }
        }

        [JSInvokable]
        public async Task OnUpdateAvailable()
        {
            // This method will be called from JavaScript when an update is detected
            // You can implement your notification logic here
            Console.WriteLine("Update available! Notifying user...");

            // Example: You might want to show a notification to the user
            // await ShowUpdateNotification();
        }

        public async ValueTask DisposeAsync()
        {
            if (_updateManager != null)
            {
                await _updateManager.DisposeAsync();
            }
            _dotNetRef?.Dispose();
        }

        private class UpdateCheckResult
        {
            public bool HasUpdate { get; set; }
            public string? Error { get; set; }
        }
    }

    // Extension method for service registration
    public static class UpdateServiceExtensions
    {
        public static IServiceCollection AddUpdateService(this IServiceCollection services)
        {
            services.AddScoped<IUpdateService, UpdateService>();
            return services;
        }
    }
}
