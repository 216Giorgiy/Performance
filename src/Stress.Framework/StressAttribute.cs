// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using Microsoft.AspNetCore.Server.Testing;
using Xunit;
using Xunit.Sdk;

namespace Stress.Framework
{
    [XunitTestCaseDiscoverer("Stress.Framework.StressTestCaseDiscoverer", "Stress.Framework")]
    public class StressAttribute : FactAttribute
    {
        public StressAttribute(string testApplicationName, params ServerType[] servers)
        {
            TestApplicationName = testApplicationName;
            Servers = servers.Any() ? servers : new ServerType[] { ServerType.IISExpress, ServerType.Kestrel };
        }

        public long Iterations { get; set; } = StressConfig.Instance.Iterations;

        public int Clients { get; set; } = StressConfig.Instance.Clients;

        public string WarmupMethodName { get; set; }

        public string TestApplicationName { get; }

        public ServerType[] Servers { get; set; }
    }
}
