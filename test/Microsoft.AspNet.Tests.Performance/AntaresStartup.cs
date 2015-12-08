﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading;
using Benchmarks.Utility.Azure;
using Benchmarks.Utility.Helpers;
using Benchmarks.Utility.Logging;
using Microsoft.Framework.Logging;
using Xunit;

namespace Microsoft.AspNet.Tests.Performance
{
    public class AntaresStartup : IDisposable
    {
        private readonly string _location = "North Central US";
        private readonly ILoggerFactory _loggerFactory;
        private readonly string _username;
        private readonly string _password;
        private readonly Random _rand;

        private ILogger _log;
        private string _testsitename;
        private string _testsitesource;
        private AzureCli _azure;

        public AntaresStartup()
        {
            _loggerFactory = LoggerHelper.GetLoggerFactory();
            _rand = new Random((int)DateTime.Now.Ticks);
            _username = GenerateRandomePassword();
            _password = GenerateRandomePassword();
        }

        public void Dispose()
        {
            if (!string.IsNullOrEmpty(_testsitename) && _azure != null)
            {
                _azure.DeleteWebSite(_testsitename);
            }
        }

        [Theory(Skip = "Reqire azure subscription on test machine")]
        [InlineData("StarterMvc", "clr")]
        [InlineData("StarterMvc", "coreclr")]
        public void PublishAndRun(string sampleName, string framework)
        {
            _log = _loggerFactory.CreateLogger<AntaresStartup>(nameof(PublishAndRun), sampleName, framework);
            using (_log.BeginScope("root"))
            {
                DeployTestSite(sampleName);

                var client = new HttpClient();
                var url = $"http://{_testsitename}.azurewebsites.net";

                var sw = new Stopwatch();
                sw.Start();

                var webstask = client.GetAsync(url);
                if (webstask.Wait(TimeSpan.FromMinutes(10)))
                {
                    sw.Stop();
                    _log.LogData("Latency", sw.ElapsedMilliseconds);
                    _log.LogData("Success", true);
                }
                else
                {
                    _log.LogError("Http client timeout after 10 minute.");
                    _log.LogData("Success", false);
                }
            }
        }

        private void DeployTestSite(string sampleName)
        {
            var runner = new CommandLineRunner() { Timeout = TimeSpan.FromMinutes(5) };

            _testsitesource = Path.GetTempFileName();
            File.Delete(_testsitesource);
            Directory.CreateDirectory(_testsitesource);

            var sourcePath = PathHelper.GetTestAppFolder(sampleName);
            Assert.NotNull(sourcePath);

            runner.Execute($"git clean -xdff .", sourcePath);
            Assert.NotEqual(-1, runner.Execute($"robocopy {sourcePath} {_testsitesource} /E /S /XD node_modules")); // robcopy doesn't return 0

            _log.LogInformation($"Testsite sources are copied to {_testsitesource}.");

            CreateTestSite();

            Assert.Equal(0, runner.Execute("git add -A .", _testsitesource));
            Assert.Equal(0, runner.Execute("git commit -m \"init\"", _testsitesource));

            var giturl = $"https://{_username}:{_password}@{_testsitename}.scm.azurewebsites.net:443/{_testsitename}.git";
            Assert.Equal(0, runner.Execute($"git remote set-url azure {giturl}", _testsitesource));

            _log.LogInformation("Git repository is set up at testsite source folder");

            runner.Timeout = TimeSpan.FromMinutes(15);
            Assert.Equal(0, runner.Execute($"git push azure master", _testsitesource));
        }

        private void CreateTestSite()
        {
            _azure = AzureCli.Create(_testsitesource);
            Assert.NotNull(_azure);

            var subscription = _azure.GetDefaultAccountName();
            Assert.NotNull(subscription);

            _log.LogInformation($"Use azure subscription {subscription}");

            int retry = 0;
            while (retry < 10)
            {
                _testsitename = GenerateRandomWebsiteName();
                var siteinfo = _azure.CreateWebSiteGit(_location, _testsitename, _username);
                if (siteinfo != null)
                {
                    break;
                }
                else
                {
                    retry++;
                }
            }

            if (retry >= 10)
            {
                Assert.False(true, $"Fail to create test web site name after {retry} tries");
            }

            _azure.SetWebSiteScaleMode(_testsitename, "Basic");
            _azure.ResetCredential(_username, _password);
        }

        private string GenerateRandomePassword()
        {
            var password = new char[32];

            int i = 0;
            for (; i < 12; ++i)
            {
                password[i] = (char)_rand.Next(65, 91);
            }

            for (; i < 24; ++i)
            {
                password[i] = (char)_rand.Next(97, 122);
            }

            for (; i < 32; ++i)
            {
                password[i] = (char)_rand.Next(48, 58);
            }

            // Fisher-Yates shuffle
            char e;
            for (i = password.Length - 1; i > 0; --i)
            {
                var pos = _rand.Next(i + 1);
                e = password[pos];
                password[pos] = password[i];
                password[i] = e;
            }

            return new string(password);
        }

        private static string GenerateRandomWebsiteName()
        {
            var name = Path.GetRandomFileName();
            return name.Remove(name.IndexOf('.'), 1);
        }
    }
}
