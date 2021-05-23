﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace PlatformBenchmarks
{
    public class Program
    {
        public static string[] Args;

        public static async Task Main(string[] args)
        {
            Args = args;

            Console.WriteLine(BenchmarkApplication.ApplicationName);
#if !DATABASE
            Console.WriteLine(BenchmarkApplication.Paths.Plaintext);
            Console.WriteLine(BenchmarkApplication.Paths.Json);
#else
            Console.WriteLine(BenchmarkApplication.Paths.Fortunes);
            Console.WriteLine(BenchmarkApplication.Paths.SingleQuery);
            Console.WriteLine(BenchmarkApplication.Paths.Updates);
            Console.WriteLine(BenchmarkApplication.Paths.MultipleQueries);
#endif
            DateHeader.SyncDateTimer();

            var host = BuildWebHost(args);
            var config = (IConfiguration)host.Services.GetService(typeof(IConfiguration));
            BatchUpdateString.DatabaseServer = config.Get<AppSettings>().Database;
#if DATABASE
            await BenchmarkApplication.Db.PopulateCache();
#endif
            await host.RunAsync();
        }

        public static IWebHost BuildWebHost(string[] args)
        {
            var config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .AddEnvironmentVariables()
                .AddEnvironmentVariables(prefix: "ASPNETCORE_")
                .AddCommandLine(args)
                .Build();

            var appSettings = config.Get<AppSettings>();
#if DATABASE
            Console.WriteLine($"Database: {appSettings.Database}");
            Console.WriteLine($"ConnectionString: {appSettings.ConnectionString}");

    #if NPGSQL
            if (appSettings.Database == DatabaseServer.PostgreSql)
    #elif MYSQLCONNECTOR
            if (appSettings.Database == DatabaseServer.MySql)
    #else
            if (false)
    #endif
            {
                BenchmarkApplication.Db = new RawDb(new ConcurrentRandom(), appSettings);
            }

            if (BenchmarkApplication.Db is null)
            {
                throw new NotSupportedException($"{appSettings.Database} is not supported by the used preprocessor directive.");
            }
#endif

            var host = new WebHostBuilder()
                .UseBenchmarksConfiguration(config)
                .UseKestrel((context, options) =>
                {
                    var endPoint = context.Configuration.CreateIPEndPoint();

                    options.Listen(endPoint, builder =>
                    {
                        builder.UseHttpApplication<BenchmarkApplication>();
                    });
                })
                .UseStartup<Startup>()
                .Build();

            return host;
        }
    }
}
