using Microsoft.Extensions.Configuration;
using Npgsql;
using System.Data;

namespace CodeReviewAnalyzer.Database.Contexts.Impl;

public class NpgConnectionFactory(IConfiguration configuration)
    : IConnectionFactory
{
    private readonly IConfiguration _configuration = configuration;

#pragma warning disable S3928 // The parameter %s is not declared in the argument list.
    public IDbConnection GetNewConnection()
    {
        var builder = new NpgsqlConnectionStringBuilder(_configuration.GetConnectionString("Default")
            ?? throw new ArgumentNullException("ConnectionStrings:Default"));
        builder.SearchPath = "public";

        return new NpgsqlConnection(builder.ConnectionString);
    }
#pragma warning restore S3928

}
