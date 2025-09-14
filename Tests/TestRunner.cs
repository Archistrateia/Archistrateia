using Godot;
using System;
using System.Reflection;
using System.Linq;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;
using NUnit.Framework.Internal.Commands;

namespace Archistrateia.Tests
{
    public partial class TestRunner : Node
    {
        public override void _Ready()
        {
            GD.Print("=== NUnit Test Runner Starting ===");
            RunNUnitTests();
        }

        private void RunNUnitTests()
        {
            try
            {
                // Get all test classes from the current assembly
                var assembly = Assembly.GetExecutingAssembly();
                var testClasses = assembly.GetTypes()
                    .Where(t => t.GetCustomAttribute<TestFixtureAttribute>() != null &&
                               !t.Name.Contains("UI")) // Exclude UI tests - they run in Phase 3
                    .ToList();

                GD.Print($"Found {testClasses.Count} test classes");

                int totalTests = 0;
                int passedTests = 0;
                int failedTests = 0;

                foreach (var testClass in testClasses)
                {
                    GD.Print($"Running tests in {testClass.Name}...");

                    var testMethods = testClass.GetMethods()
                        .Where(m => m.GetCustomAttribute<TestAttribute>() != null)
                        .ToList();

                    foreach (var testMethod in testMethods)
                    {
                        totalTests++;
                        GD.Print($"  Running {testMethod.Name}...");

                        try
                        {
                            // Create instance of test class
                            var testInstance = Activator.CreateInstance(testClass);

                            // Run the test method
                            testMethod.Invoke(testInstance, null);

                            GD.Print($"    ✓ PASS: {testMethod.Name}");
                            passedTests++;
                        }
                        catch (Exception ex)
                        {
                            GD.PrintErr($"    ✗ FAIL: {testMethod.Name} - {ex.InnerException?.Message ?? ex.Message}");
                            failedTests++;
                        }
                    }
                }

                GD.Print("=== NUnit Test Results ===");
                GD.Print($"Total Tests: {totalTests}");
                GD.Print($"Passed: {passedTests}");
                GD.Print($"Failed: {failedTests}");
                GD.Print($"Success Rate: {(passedTests * 100.0 / totalTests):F1}%");

                if (failedTests == 0)
                {
                    GD.Print("🎉 ALL NUnit TESTS PASSED! 🎉");
                }
                else
                {
                    GD.PrintErr("❌ SOME NUnit TESTS FAILED! ❌");
                }

                GD.Print("=== NUnit Test Runner Completed ===");
            }
            catch (Exception ex)
            {
                GD.PrintErr($"Error running NUnit tests: {ex.Message}");
            }
        }
    }
}