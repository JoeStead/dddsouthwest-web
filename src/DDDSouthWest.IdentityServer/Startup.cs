﻿using DDDSouthWest.IdentityServer.Framework;
using IdentityServer4.Services;
using IdentityServer4.Validation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DDDSouthWest.IdentityServer
{
    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", false, true)
                .AddJsonFile($"appsettings.{env.EnvironmentName.ToLower()}.json", true)
                .AddEnvironmentVariables();

            Configuration = builder.Build();
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();

            services.AddAuthServerAppSettingsOptions(Configuration);
            
            var websiteUrl = Configuration["DDDSouthWestIdentityServer:WebsiteUrl"];
            services.AddIdentityServer()
                .AddTemporarySigningCredential()
                .AddInMemoryIdentityResources(Config.GetIdentityResources())
                .AddInMemoryApiResources(Config.GetApiResources()) // only used for API
                .AddInMemoryClients(Config.GetClients(websiteUrl))
                .AddProfileService<CustomProfileService>();

            services.AddTransient<IUserStore, CustomUserStore>();
            services.AddTransient<IProfileService, CustomProfileService>();
            services.AddTransient<IResourceOwnerPasswordValidator, CustomValidator>();
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            var forwardedHeadersOptions = new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
            };
            forwardedHeadersOptions.KnownProxies.Clear();
            forwardedHeadersOptions.KnownNetworks.Clear();

            app.UseForwardedHeaders(forwardedHeadersOptions);

            if (env.IsDevelopment())
            {
                loggerFactory.AddConsole(LogLevel.Debug);
                app.UseDeveloperExceptionPage();
            }

            app.UseIdentityServer();

            app.UseStaticFiles();
            app.UseMvcWithDefaultRoute();
        }
    }
}