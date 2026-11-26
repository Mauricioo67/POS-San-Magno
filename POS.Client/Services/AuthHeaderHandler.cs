using System.Net.Http.Headers;

namespace POS.Client.Services
{
    public class AuthHeaderHandler : DelegatingHandler
    {
        private readonly AuthStateService _auth;

        public AuthHeaderHandler(AuthStateService auth)
        {
            _auth = auth;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (_auth.IsAuthenticated && request.Headers.Authorization is null)
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _auth.Token);
            }
            return await base.SendAsync(request, cancellationToken);
        }
    }
}
