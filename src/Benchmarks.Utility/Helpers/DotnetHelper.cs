﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;

namespace Benchmarks.Utility.Helpers
{
    public class DotnetHelper
    {
        private readonly string _executablePath = Path.Combine("cli", "bin", "dotnet.exe");
        private readonly string _defaultDotnetHome = Path.Combine(Environment.GetEnvironmentVariable("LocalAppData"), "Microsoft", "dotnet");

        public ProcessStartInfo BuildStartInfo(
            string appbasePath,
            string argument)
        {
            var dnxPath = GetDotnetExecutable();
            var psi = new ProcessStartInfo(dnxPath, argument)
            {
                WorkingDirectory = appbasePath,
                UseShellExecute = false
            };

            return psi;
        }

        public bool Restore(string workingDir, bool quiet = false)
        {
            var psi = new ProcessStartInfo(GetDotnetExecutable())
            {
                Arguments = "restore" + (quiet ? " --quiet" : ""),
                WorkingDirectory = workingDir,
                UseShellExecute = false
            };

            var proc = Process.Start(psi);

            var exited = proc.WaitForExit(300 * 1000);

            return exited && proc.ExitCode == 0;
        }

        public bool Publish(string workingDir, string outputDir)
        {
            var psi = new ProcessStartInfo(GetDotnetExecutable())
            {
                Arguments = $"publish --output {outputDir}",
                WorkingDirectory = workingDir,
                UseShellExecute = false
            };

            var proc = Process.Start(psi);

            var exited = proc.WaitForExit(300 * 1000);

            return exited && proc.ExitCode == 0;
        }

        public string GetDotnetPath()
        {
            var envDotnetHome = Environment.GetEnvironmentVariable("DOTNET_INSTALL_DIR");
            var dotnetHome = envDotnetHome != null ? Environment.ExpandEnvironmentVariables(envDotnetHome) : _defaultDotnetHome;

            if (Directory.Exists(dotnetHome))
            {
                return dotnetHome;
            }
            else
            {
                return null;
            }
        }

        public string GetDotnetExecutable()
        {
            var dnxPath = GetDotnetPath();
            if (dnxPath != null)
            {
                return Path.Combine(dnxPath, _executablePath);
            }
            else
            {
                return null;
            }
        }
    }
}
