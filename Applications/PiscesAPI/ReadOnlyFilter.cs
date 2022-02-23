using Swashbuckle.AspNetCore.SwaggerGen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.OpenApi.Models;

namespace PiscesAPI
{
    /// <summary>
    /// modified from:
    /// https://github.com/jenyayel/SwaggerSecurityTrimming/blob/master/src/V3/SecurityTrimming.cs
    /// </summary>
    public class ReadOnlyFilter : IDocumentFilter
    {
        private IServiceProvider _provider;

        public ReadOnlyFilter(IServiceProvider provider)
        {
            if (provider == null) throw new ArgumentNullException(nameof(provider));

            this._provider = provider;
        }

        public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
        {
            var http = this._provider.GetRequiredService<IHttpContextAccessor>();
            var auth = this._provider.GetRequiredService<IAuthorizationService>();

            foreach (var description in context.ApiDescriptions)
            {
                var authAttributes = description.CustomAttributes().OfType<AuthorizeAttribute>();
                bool notShown = isForbiddenDueAnonymous(http, authAttributes) ||
                    isForbiddenDuePolicy(http, auth, authAttributes);

                if (!notShown)
                    continue; // user passed all permission checks

                var route = "/" + description.RelativePath.TrimEnd('/');
                var path = swaggerDoc.Paths[route];

                // remove method or entire path (if there are no more methods in this path)
                OperationType operation = Enum.Parse<OperationType>(description.HttpMethod, true);
                path.Operations.Remove(operation);
                if (path.Operations.Count == 0)
                {
                    swaggerDoc.Paths.Remove(route);
                }
            }
        }

        private static bool isForbiddenDueAnonymous(
            IHttpContextAccessor http,
            IEnumerable<AuthorizeAttribute> attributes)
        {
            return attributes.Any() && !http.HttpContext.User.Identity.IsAuthenticated;
        }

        private static bool isForbiddenDuePolicy(
            IHttpContextAccessor http,
            IAuthorizationService auth,
            IEnumerable<AuthorizeAttribute> attributes)
        {
            var policies = attributes
                .Where(p => !String.IsNullOrEmpty(p.Policy))
                .Select(a => a.Policy)
                .Distinct();

            var result = Task.WhenAll(policies.Select(p => auth.AuthorizeAsync(http.HttpContext.User, p))).Result;
            return result.Any(r => !r.Succeeded);
        }

    }
}