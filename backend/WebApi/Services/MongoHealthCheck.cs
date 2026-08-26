using Microsoft.Extensions.Diagnostics.HealthChecks;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace WebApi.Services
{
    public class MongoHealthCheck : IHealthCheck
    {
        private readonly IMongoClient _client;

        public MongoHealthCheck(IMongoClient client)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                var db = _client.GetDatabase("admin");
                await db.RunCommandAsync<BsonDocument>(new BsonDocument("ping", 1), cancellationToken: cancellationToken);
                return HealthCheckResult.Healthy("MongoDB ping OK");
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy("MongoDB ping failed", ex);
            }
        }
    }
}
