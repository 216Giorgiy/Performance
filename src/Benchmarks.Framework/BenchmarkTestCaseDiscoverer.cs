﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Xunit.Abstractions;
using Xunit.Sdk;
using XunitDiagnosticMessage = Xunit.DiagnosticMessage;

namespace Benchmarks.Framework
{
    public class BenchmarkTestCaseDiscoverer : IXunitTestCaseDiscoverer
    {
        private readonly IMessageSink _diagnosticMessageSink;

        public BenchmarkTestCaseDiscoverer(IMessageSink diagnosticMessageSink)
        {
            _diagnosticMessageSink = diagnosticMessageSink;
        }

        public virtual IEnumerable<IXunitTestCase> Discover(
            ITestFrameworkDiscoveryOptions discoveryOptions,
            ITestMethod testMethod,
            IAttributeInfo factAttribute)
        {
            var skipReason = EvaluateSkipConditions(testMethod);

            if(skipReason != null)
            {
                _diagnosticMessageSink.OnMessage(
                        new XunitDiagnosticMessage($"Skipping { testMethod.Method.Name }{ Environment.NewLine }Reason: { skipReason }"));
                return new List<IXunitTestCase>();
            }

            var variations = testMethod.Method
                .GetCustomAttributes(typeof(BenchmarkVariationAttribute))
                .Select(a => new
                {
                    Name = a.GetNamedArgument<string>(nameof(BenchmarkVariationAttribute.VariationName)),
                    TestMethodArguments = a.GetNamedArgument<object[]>(nameof(BenchmarkVariationAttribute.Data)),
                    Framework = a.GetNamedArgument<string>(nameof(BenchmarkVariationAttribute.Framework))
                })
                .ToList();

            if (!variations.Any())
            {
                variations.Add(new
                {
                    Name = "Default",
                    TestMethodArguments = new object[0],
                    Framework = (string)null
                });
            }

            var tests = new List<IXunitTestCase>();
            foreach (var variation in variations)
            {
                if (BenchmarkConfig.Instance.RunIterations)
                {
                    tests.Add(new BenchmarkTestCase(
                        factAttribute.GetNamedArgument<int>(nameof(BenchmarkAttribute.Iterations)),
                        factAttribute.GetNamedArgument<int>(nameof(BenchmarkAttribute.WarmupIterations)),
                        variation.Framework,
                        variation.Name,
                        _diagnosticMessageSink,
                        testMethod,
                        variation.TestMethodArguments));
                }
                else
                {
                    tests.Add(new NonCollectingBenchmarkTestCase(
                        variation.Name,
                        _diagnosticMessageSink,
                        testMethod,
                        variation.TestMethodArguments));
                }
            }

            return tests;
        }

        private string EvaluateSkipConditions(ITestMethod testMethod)
        {
            return
                testMethod.Method
                .GetCustomAttributes(typeof(ITestCondition))
                .OfType<ReflectionAttributeInfo>()
                .Select(attributeInfo => attributeInfo.Attribute)
                .OfType<ITestCondition>()
                .Where(condition => !condition.IsMet)
                .Select(condition => condition.SkipReason)
                .FirstOrDefault();
        }
    }
}
