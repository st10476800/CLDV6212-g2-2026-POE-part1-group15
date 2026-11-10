using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace CoffeeNChillGateway.Filters
{
    /// <summary>
    /// Adds the padlock to the endpoints in Swagger that need a token, and the 401 and 403 responses.
    /// Endpoints with [AllowAnonymous] get no padlock.
    /// </summary>
    public class AuthorizeOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            IList<object> metadata = context.ApiDescription.ActionDescriptor.EndpointMetadata;

            bool hasAuthorize = false;
            bool hasAllowAnonymous = false;

            for (int i = 0; i < metadata.Count; i++)
            {
                if (metadata[i] is AuthorizeAttribute)
                {
                    hasAuthorize = true;
                }
                if (metadata[i] is AllowAnonymousAttribute)
                {
                    hasAllowAnonymous = true;
                }
            }

            if (hasAuthorize == false || hasAllowAnonymous == true)
            {
                return;
            }

            if (operation.Responses.ContainsKey("401") == false)
            {
                operation.Responses.Add("401", new OpenApiResponse { Description = "Missing or invalid token." });
            }
            if (operation.Responses.ContainsKey("403") == false)
            {
                operation.Responses.Add("403", new OpenApiResponse { Description = "The token's role is not allowed to do this." });
            }

            // This points to the "Bearer" security definition we add in Program.cs
            OpenApiReference reference = new OpenApiReference();
            reference.Type = ReferenceType.SecurityScheme;
            reference.Id = "Bearer";

            OpenApiSecurityScheme scheme = new OpenApiSecurityScheme();
            scheme.Reference = reference;

            OpenApiSecurityRequirement requirement = new OpenApiSecurityRequirement();
            requirement.Add(scheme, new List<string>());

            if (operation.Security == null)
            {
                operation.Security = new List<OpenApiSecurityRequirement>();
            }
            operation.Security.Add(requirement);
        }
    }
}
