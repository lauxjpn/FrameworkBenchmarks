// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using MySqlConnector;
using Npgsql;

namespace PlatformBenchmarks
{
    public class Startup
    {
        public Startup(IWebHostEnvironment hostingEnv)
        {
            var config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .AddJsonFile($"appsettings.{hostingEnv.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables()
                .AddEnvironmentVariables(prefix: "ASPNETCORE_")
                .AddCommandLine(Program.Args)
                .Build();

#if DATABASE
            var appSettings = config.Get<AppSettings>();
            
            Console.WriteLine($"Database: {appSettings.Database}");
            Console.WriteLine($"ConnectionString: {appSettings.ConnectionString}");
            
            if (appSettings.Database == DatabaseServer.PostgreSql)
            {
                BenchmarkApplication.Db = new RawDb(NpgsqlFactory.Instance, new ConcurrentRandom(), appSettings);
            }
            else if (appSettings.Database == DatabaseServer.MySql)
            {
                BenchmarkApplication.Db = new RawDb(MySqlConnectorFactory.Instance, new ConcurrentRandom(), appSettings);
            }
            else
            {
                throw new NotSupportedException($"{appSettings.Database} is not supported");
            }
#endif
        }

        public void Configure(IApplicationBuilder app)
        {
            
        }
    }
}
