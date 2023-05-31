using Microsoft.Graph;
using Microsoft.Identity.Client;
using System.Net.Http.Headers;
using System.Security;

namespace UploadFile.Helpers
{
    public class MsalAuthenticationProvider : IAuthenticationProvider
    {
        private static MsalAuthenticationProvider? _singleton;
        private IPublicClientApplication _clientApplication;
        private string[] _scopes;
        private string? _userId;
        private string _username;
        private string _password;


        private MsalAuthenticationProvider(IPublicClientApplication clientApplication, string[] scopes, string username, string password)
        {
            _clientApplication = clientApplication;
            _scopes = scopes;
            _userId = null;
            _username = username;
            _password = password;
        }

        public static MsalAuthenticationProvider GetInstance(IPublicClientApplication clientApplication, string[] scopes, string username, string password)
        {
            if (_singleton == null)
            {
                _singleton = new MsalAuthenticationProvider(clientApplication, scopes, username, password);
            }

            return _singleton;
        }

        public async Task AuthenticateRequestAsync(HttpRequestMessage request)
        {
            var accessToken = await GetTokenAsync();
                
            request.Headers.Authorization = new AuthenticationHeaderValue("bearer", accessToken);
        }


        public async Task<string> GetTokenAsync()
        {
            if (!string.IsNullOrEmpty(_userId))
            {
                try
                {
                    var account = await _clientApplication.GetAccountAsync(_userId);

                    if (account != null)
                    {
                        var silentResult = await _clientApplication.AcquireTokenSilent(_scopes, account).ExecuteAsync();
                        return silentResult.AccessToken;
                    }
                }
                catch (MsalUiRequiredException) { }
            }

            var result = await _clientApplication.AcquireTokenInteractive(_scopes).ExecuteAsync();
            _userId = result.Account.HomeAccountId.Identifier;
            return result.AccessToken;
        }
    }
}
