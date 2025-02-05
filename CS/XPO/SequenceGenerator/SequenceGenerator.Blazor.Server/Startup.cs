﻿using DevExpress.ExpressApp.ApplicationBuilder;
using DevExpress.ExpressApp.Blazor.ApplicationBuilder;
using DevExpress.ExpressApp.Blazor.Services;
using DevExpress.Persistent.Base;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Server.Circuits;
using SequenceGenerator.Blazor.Server.Services;
using DevExpress.ExpressApp.Core;
using DevExpress.ExpressApp.ApplicationBuilder.Internal;
using DevExpress.ExpressApp.DC;
using GenerateUserFriendlyId.Module;
using dxTestSolution.Module.BusinessObjects;
using Microsoft.AspNetCore.Http.Extensions;
using DevExpress.ExpressApp.Xpo;
using Microsoft.Extensions.Options;

namespace SequenceGenerator.Blazor.Server;

public class Startup {
    public Startup(IConfiguration configuration) {
        Configuration = configuration;
    }

    public IConfiguration Configuration { get; }

    // This method gets called by the runtime. Use this method to add services to the container.
    // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940

    public void ConfigureServices(IServiceCollection services) {
        services.AddSingleton(typeof(Microsoft.AspNetCore.SignalR.HubConnectionHandler<>), typeof(ProxyHubConnectionHandler<>));

        services.AddRazorPages();
        services.AddServerSideBlazor();
        services.AddHttpContextAccessor();
        services.AddScoped<CircuitHandler, CircuitHandlerProxy>();
        services.AddXaf(Configuration, builder => {
            builder.UseApplication<SequenceGeneratorBlazorApplication>();
            builder.Modules
                .Add<SequenceGenerator.Module.SequenceGeneratorModule>()
                .Add<SequenceGeneratorBlazorModule>();
            builder.ObjectSpaceProviders
                .Add((_serviceProvider) => {
                    XPObjectSpaceProviderOptions xPObjectSpaceProviderOptions = new XPObjectSpaceProviderOptions();
                    string connectionString = null;
                    if (Configuration.GetConnectionString("ConnectionString") != null) {
                        connectionString = Configuration.GetConnectionString("ConnectionString");
                    }
                    IXpoDataStoreProvider dataStoreProvider = XPObjectSpaceProvider.GetDataStoreProvider(connectionString, null, true);
                    GenerateUserFriendlyId.Module.SequenceGenerator.Initialize(dataStoreProvider);
                    xPObjectSpaceProviderOptions.ConnectionString = connectionString;
                    xPObjectSpaceProviderOptions.ThreadSafe = true;
                    xPObjectSpaceProviderOptions.UseSharedDataStoreProvider = true;
                    return new XPObjectSpaceProvider(dataStoreProvider, _serviceProvider.GetRequiredService<ITypesInfo>(), null, xPObjectSpaceProviderOptions.ThreadSafe, xPObjectSpaceProviderOptions.UseSeparateDataLayers);
                })
                .AddNonPersistent();
        });
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env) {
        if (env.IsDevelopment()) {
            app.UseDeveloperExceptionPage();
        } else {
            app.UseExceptionHandler("/Error");
            // The default HSTS value is 30 days. To change this for production scenarios, see: https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }
        app.UseHttpsRedirection();
        app.UseRequestLocalization();
        app.UseStaticFiles();
        app.UseRouting();
        app.UseXaf();
        app.UseEndpoints(endpoints => {
            endpoints.MapXafEndpoints();
            endpoints.MapBlazorHub();
            endpoints.MapFallbackToPage("/_Host");
            endpoints.MapControllers();
        });
    }
}
