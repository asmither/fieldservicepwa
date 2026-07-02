//using ICS.Mobile.Services.ServiceModels;

//using Microsoft.JSInterop;

//namespace ICS.Mobile.Services
//{

//    public class ConnectionStateService : JSServiceBase
//    {
//        public ConnectionStateService(IJSRuntime js, SettingsService settings)
//            : base(js, settings, "connectionStateService.js")
//        {

//        }

//        public event Action<ConnectionStates>? OnConnectionStateChanged;

//        private void ConnectionStateChanged(object connectionState)
//        {
//            OnConnectionStateChanged?.Invoke(ConnectionStates.Unknown);
//        }

//        protected override async Task WaitForReferenceAsync()
//        {
//            if (!jsRef.IsValueCreated)
//            {
//                await base.WaitForReferenceAsync();
//                await jsRef.Value.InvokeVoidAsync("setRef", DotNetObjectReference.Create(this));
//            }
//        }

//        public async Task<ConnectionStates> GetConnectionState()
//        {
//            await WaitForReferenceAsync();
//            if(jsRef.IsValueCreated)
//            {
//                try
//                {
//                    return await jsRef.Value.InvokeAsync<ConnectionStates>("getConnectionState");
//                }
//                catch {}
//            }
//            return ConnectionStates.Unknown;
//        }
//        public override async ValueTask DisposeAsync()
//        {
//            if (jsRef.IsValueCreated)
//            {
//                await jsRef.Value.InvokeVoidAsync("dispose");
//                await jsRef.Value.DisposeAsync();
//            }
//        }
//    }
//}
// This is no longer in use