using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;

namespace ASTDAT.Web.Infrastructure
{
    public class JwtAuthMessageHandler : DelegatingHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var path = request.RequestUri?.AbsolutePath ?? "";
            if (path.EndsWith("/api/Token", StringComparison.OrdinalIgnoreCase))
                return await base.SendAsync(request, cancellationToken);

            if (request.Headers.Authorization != null
                && string.Equals(request.Headers.Authorization.Scheme, "Bearer", StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrEmpty(request.Headers.Authorization.Parameter))
            {
                var key = System.Configuration.ConfigurationManager.AppSettings["JwtSigningKey"] ?? "";
                if (!string.IsNullOrEmpty(key))
                {
                    string err;
                    var principal = SimpleJwt.ValidateToPrincipal(request.Headers.Authorization.Parameter, key, out err);
                    if (principal != null)
                    {
                        Thread.CurrentPrincipal = principal;
                        if (HttpContext.Current != null) HttpContext.Current.User = principal;
                        var ctx = request.GetRequestContext();
                        if (ctx != null) ctx.Principal = principal;
                    }
                }
            }

            return await base.SendAsync(request, cancellationToken);
        }
    }
}
