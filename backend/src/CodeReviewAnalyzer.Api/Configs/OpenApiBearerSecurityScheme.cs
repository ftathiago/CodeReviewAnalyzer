using Microsoft.OpenApi.Models;
using System.Diagnostics.CodeAnalysis;

namespace CodeReviewAnalyzer.Api.Configs;

[ExcludeFromCodeCoverage]
internal class OpenApiBearerSecurityScheme : OpenApiSecurityScheme
{
    public OpenApiBearerSecurityScheme()
    {
        Description = "Using the Authorization header with the Bearer scheme.";
        Name = "Authorization";
        In = ParameterLocation.Header;
        Type = SecuritySchemeType.Http;
        Scheme = "bearer";
        Reference = new OpenApiReference
        {
            Type = ReferenceType.SecurityScheme,
            Id = "Bearer",
        };
    }
}
