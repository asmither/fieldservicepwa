using ICS.Mobile.Http;
using ICS.Portal.Auth.Models;
using ICS.Portal.Data.Custom;

using System.Net.Http.Headers;

namespace ICS.Mobile.Services
{
    public class HTTPService
    {
        private readonly SettingsService settings;
        private readonly IDXDBService service;
      
        public HTTPService(SettingsService settings)
        {
            this.settings = settings;
        }
        public HttpPing GetHttpPingClient()
        {
            var client = new HttpClient();
            client.BaseAddress = new(settings.ApiUrl);
            client.Timeout = TimeSpan.FromSeconds(settings.PingTimeoutDefault);
            return new HttpPing(client);
        }
        public HttpAuthentication GetHttpAuthenticationClient(AuthorizedUser? user = null)
        {
            var client = new HttpClient();
            client.BaseAddress = new(settings.ApiUrl);
            client.Timeout = TimeSpan.FromSeconds(settings.QueryTimeoutDefault);
            if(user is not null)
            {
                SetAuthorizationHeader(client, user);
            }
           
            return new HttpAuthentication(client);
        }
        public HttpCommands GetHttpCommandsClient(AuthorizedUser user)
        {
            var client = new HttpClient();
            client.BaseAddress = new(settings.ApiUrl);
            client.Timeout = TimeSpan.FromSeconds(settings.CommandTimeoutDefault);
            SetAuthorizationHeader(client, user);
            return new HttpCommands(client);
        }
        
        public HttpQueries GetHttpQueriesClient(AuthorizedUser user)
        {
            var client = new HttpClient();
            client.BaseAddress = new(settings.ApiUrl);
            client.Timeout = TimeSpan.FromSeconds(settings.QueryTimeoutDefault);
            SetAuthorizationHeader(client, user);
            return new HttpQueries(client);
        }

        public HttpClient GetImagesHttpClient()
        {
            var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(settings.ImageTimeoutDefault);
            return client;
        }
        private void SetAuthorizationHeader(HttpClient client, AuthorizedUser user)
        {
            if (user is not null)
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(nameof(AuthorizedUser), user.Token);
            }
        }
    }
}
