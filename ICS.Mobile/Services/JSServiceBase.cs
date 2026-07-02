using Microsoft.JSInterop;

using System.Buffers.Text;
using System.Text;

namespace ICS.Mobile.Services
{
    /// <summary>
    /// This class provides the minimum requirements for implementation of a JS Module in Blazor.
    /// Each JS module attached must provide a init function that accepts the Settings service as a parameter.
    /// Any additional code that needs to be executed during the initialized can be done by overriding the WaitForReference call.
    /// Consequently any clean up code can be executed by overriding the DisposeAsyncMethod
    /// </summary>
    public abstract class JSServiceBase : IAsyncDisposable
    {
        protected readonly IJSRuntime jsRuntime;
        protected readonly SettingsService settings;
        protected Lazy<IJSObjectReference> jsRef = new();
        private readonly string module;
        private bool isOfflineMode = false;


        /// <summary>
        /// Loads modules either from server as a file, or as text
        /// </summary>
        /// <param name="jsRuntime">IJSRuntime instance</param>
        /// <param name="settings">SettingService instance</param>
        /// <param name="js">File Name (module.js) or JavaScript as a string</param>
        public JSServiceBase(IJSRuntime jsRuntime, SettingsService settings, string js)
        {
            this.jsRuntime = jsRuntime;
            this.settings = settings;
            if(js.EndsWith(".js"))
            {
                module = $"{settings.JsModulePath}{js}";
            }
            else
            {
                module = $"data:text/javascript;base64,{Convert.ToBase64String(Encoding.UTF8.GetBytes(js))}";
            }
        }

        public virtual async ValueTask DisposeAsync()
        {
            if (jsRef.IsValueCreated)
            {
                await jsRef.Value.DisposeAsync();
            }
        }

        protected virtual async Task WaitForReferenceAsync()
        {
            if (!jsRef.IsValueCreated)
            {
                try
                {
                    jsRef = new(await jsRuntime.InvokeAsync<IJSObjectReference>("import", module));
                    if (jsRef.IsValueCreated == false)
                    {
                        throw new Exception($"Unable to create js object reference for JS: {module}");
                    }

                    // Try to initialize the module
                    try
                    {
                        var initResult = await jsRef.Value.InvokeAsync<bool>("init", settings);
                        if (!initResult)
                        {
                            // Module loaded but init returned false - might be offline
                            Console.WriteLine($"Warning: Module {module} init returned false. Module may have limited functionality.");
                            isOfflineMode = true;
                        }
                    }
                    catch (JSException jsEx) when (jsEx.Message.Contains("Could not find 'init'"))
                    {
                        // Module loaded but doesn't have init function - this is OK for some modules
                        Console.WriteLine($"Module {module} doesn't have an init function.");
                    }
                }
                catch (JSException jsEx) when (jsEx.Message.Contains("Failed to fetch dynamically imported module"))
                {
                    // This is the specific error you're seeing
                    Console.WriteLine($"Failed to dynamically import module {module}. App may be transitioning to offline mode.");
                    throw new InvalidOperationException($"Unable to load module {module}. Please ensure you're online or refresh the page.", jsEx);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error loading module {module}: {ex.Message}");
                    throw;
                }
            }
        }

        protected bool IsOfflineMode => isOfflineMode;
    }
}