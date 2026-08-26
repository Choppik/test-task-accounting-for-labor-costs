using FluentValidation.AspNetCore;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using MongoDB.Driver;
using System;
using WebApi.Commands;
using WebApi.Services;

namespace WebApi
{
    public class Startup
    {
        private const string DevCorsPolicy = "AllowLocalDev";
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            // CORS для разработки (frontend на localhost:3000)
            services.AddCors(options =>
            {
                options.AddPolicy(DevCorsPolicy, builder =>
                {
                    builder.WithOrigins("http://localhost:3000")
                           .AllowAnyHeader()
                           .AllowAnyMethod();
                });
            });

            services.AddControllers()
                    .AddFluentValidation(fv => fv.RegisterValidatorsFromAssemblyContaining<Startup>());

            var mongoConnection = Configuration.GetConnectionString("MongoDb") ?? "mongodb://localhost:27017";
            services.AddSingleton<IMongoClient>(sp => new MongoClient(mongoConnection));

            var dbName = Configuration["Mongo:Database"] ?? "AccountingForLaborCostsDb";
            services.AddSingleton(sp => sp.GetRequiredService<IMongoClient>().GetDatabase(dbName));

            services.AddSingleton<MongoHealthCheck>();

            services.AddHealthChecks()
                    .AddCheck<MongoHealthCheck>("mongodb", timeout: TimeSpan.FromSeconds(3));

            services.AddMediatR(typeof(Startup).Assembly);

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "AccountingForLaborCosts API", Version = "v1" });
            });

            services.AddTransient<MongoSeeder>();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseMiddleware<ExceptionMiddleware>();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseCors(DevCorsPolicy);

            app.UseRouting();

            app.UseAuthorization();

            app.UseSwagger();
            app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "AccountingForLaborCosts API V1"));

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapHealthChecks("/health");
            });

            using (var scope = app.ApplicationServices.CreateScope())
            {
                var seeder = scope.ServiceProvider.GetService<MongoSeeder>();
                seeder?.SeedAsync().GetAwaiter().GetResult();
            }
        }
    }
}
