using Godot;
using System;
using System.Reflection;
using System.Linq;
using NUnit.Framework;

namespace Archistrateia.Tests
{
    public partial class UITestRunner : Node
    {
        public override void _Ready()
        {
            GD.Print("=== UI Test Runner Starting ===");
            RunUITests();
        }

        private void RunUITests()
        {
            try
            {
                // Get all UI test classes from the current assembly
                var assembly = Assembly.GetExecutingAssembly();
                var uiTestClasses = assembly.GetTypes()
                    .Where(t => t.GetCustomAttribute<TestFixtureAttribute>() != null && 
                               t.Name.Contains("UI"))
                    .ToList();

                GD.Print($"Found {uiTestClasses.Count} UI test classes");

                int totalTests = 0;
                int passedTests = 0;
                int failedTests = 0;

                foreach (var testClass in uiTestClasses)
                {
                    GD.Print($"Running UI tests in {testClass.Name}...");

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

                GD.Print("=== UI Test Results ===");
                GD.Print($"Total Tests: {totalTests}");
                GD.Print($"Passed: {passedTests}");
                GD.Print($"Failed: {failedTests}");
                GD.Print($"Success Rate: {(passedTests * 100.0 / totalTests):F1}%");

                if (failedTests == 0)
                {
                    GD.Print("🎉 ALL UI TESTS PASSED! 🎉");
                }
                else
                {
                    GD.PrintErr("❌ SOME UI TESTS FAILED! ❌");
                }

                GD.Print("=== UI Test Runner Completed ===");
            }
            catch (Exception ex)
            {
                GD.PrintErr($"Error running UI tests: {ex.Message}");
            }
        }
    }
}
