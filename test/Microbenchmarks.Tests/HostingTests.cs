// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Benchmarks.Framework;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Testing.xunit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.PlatformAbstractions;

namespace Microbenchmarks.Tests
{
    public class HostingTests : BenchmarkTestBase
    {
        [OSSkipCondition(OperatingSystems.MacOSX)]
        [OSSkipCondition(OperatingSystems.Linux)]
        [Benchmark]
        [BenchmarkVariation("Kestrel", "Microsoft.AspNetCore.Server.Kestrel")]
        [BenchmarkVariation("WebListener", "Microsoft.AspNetCore.Server.WebListener")]
        public void MainToConfigureOverhead(string variationServer)
        {
            var args = new[] { "--server", variationServer, "--captureStartupErrors", "true" };

            using (Collector.StartCollection())
            {
                var config = new ConfigurationBuilder()
                    .AddEnvironmentVariables(prefix: "ASPNETCORE_")
                    .AddCommandLine(args)
                    .SetBasePath(PlatformServices.Default.Application.ApplicationBasePath)
                    .Build();
                
                var builder = new WebHostBuilder()
                    .UseConfiguration(config)
                    .UseStartup(typeof(TestStartup))
                    .ConfigureServices(ConfigureTestServices);

                var host = builder.Build();
                host.Start();
                host.Dispose();
            }
        }

        private void ConfigureTestServices(IServiceCollection services)
        {
            services.AddSingleton(new TestServer());
            services.AddSingleton(Collector);
        }

        private class TestStartup
        {
            public void ConfigureServices(IServiceCollection services)
            {
            }

            public void Configure(IApplicationBuilder app, IMetricCollector collector)
            {
                collector.StopCollection();
            }

            public static void Main(string[] args)
            {
                var config = new ConfigurationBuilder()
                    .AddEnvironmentVariables(prefix: "ASPNETCORE_")
                    .AddCommandLine(args)
                    .SetBasePath(PlatformServices.Default.Application.ApplicationBasePath)
                    .Build();
                    
                var host = new WebHostBuilder()
                    .UseConfiguration(config)
                    .UseStartup<TestStartup>()
                    .Build();

                host.Run();
            }
        }

        private class TestServer : IServer
        {
            public TestServer()
            {
            }
        
            public void Dispose()
            {
            }

            public IFeatureCollection Features { get; }

            public void Start<TContext>(IHttpApplication<TContext> application)
            {
                // No-op, we don't want to actually start the server.
            }
        }
    }
}
