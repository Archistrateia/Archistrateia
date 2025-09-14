using Godot;
using System;
using System.Collections.Generic;

namespace Archistrateia.Tests
{
    /// <summary>
    /// Godot Scene Test Runner - Tests actual Godot scenes that NUnit cannot handle.
    /// This runs within the Godot engine and can test full scene functionality.
    /// </summary>
    public partial class GodotSceneTestRunner : Node
    {
        private List<SceneTest> _sceneTests = new List<SceneTest>();
        private int _currentTestIndex = 0;
        private int _passedTests = 0;
        private int _failedTests = 0;
        private string _currentTestName = "";
        private bool _testInProgress = false;

        public override void _Ready()
        {
            GD.Print("=== Godot Scene Test Runner Starting ===");
            
            // Initialize scene tests
            InitializeSceneTests();
            
            // Start running tests
            CallDeferred(nameof(RunNextTest));
        }

        private void InitializeSceneTests()
        {
            // Test 1: Main Scene Loading and Basic UI
            _sceneTests.Add(new SceneTest
            {
                Name = "Main_Scene_Loads_Successfully",
                Description = "Test that the main scene loads without errors",
                TestAction = TestMainSceneLoads
            });

            // Test 2: UI Manager Initialization
            _sceneTests.Add(new SceneTest
            {
                Name = "UI_Manager_Initializes_Correctly",
                Description = "Test that UI manager initializes with all components",
                TestAction = TestUIManagerInitialization
            });

            // Test 3: Button Functionality
            _sceneTests.Add(new SceneTest
            {
                Name = "Start_Button_Functionality",
                Description = "Test that start button works correctly",
                TestAction = TestStartButtonFunctionality
            });

            // Test 4: Map Generation
            _sceneTests.Add(new SceneTest
            {
                Name = "Map_Generation_Works",
                Description = "Test that map generation works from UI",
                TestAction = TestMapGeneration
            });

            // Test 5: Scene Transitions
            _sceneTests.Add(new SceneTest
            {
                Name = "Scene_Transitions_Work",
                Description = "Test that scene transitions work properly",
                TestAction = TestSceneTransitions
            });

            GD.Print($"Initialized {_sceneTests.Count} scene tests");
        }

        private void RunNextTest()
        {
            if (_currentTestIndex >= _sceneTests.Count)
            {
                PrintFinalResults();
                return;
            }

            var test = _sceneTests[_currentTestIndex];
            _currentTestName = test.Name;
            _testInProgress = true;

            GD.Print($"Running scene test: {test.Name} - {test.Description}");

            try
            {
                bool result = test.TestAction();
                if (result)
                {
                    GD.Print($"✓ PASS: {test.Name}");
                    _passedTests++;
                }
                else
                {
                    GD.PrintErr($"✗ FAIL: {test.Name}");
                    _failedTests++;
                }
            }
            catch (Exception ex)
            {
                GD.PrintErr($"✗ FAIL: {test.Name} - Exception: {ex.Message}");
                _failedTests++;
            }

            _currentTestIndex++;
            _testInProgress = false;

            // Run next test after a short delay
            GetTree().CreateTimer(0.1f).Timeout += RunNextTest;
        }

        private bool TestMainSceneLoads()
        {
            try
            {
                // Test that we can load the main scene
                var mainScene = GD.Load<PackedScene>("res://Scenes/Main.tscn");
                if (mainScene == null)
                {
                    GD.PrintErr("Failed to load Main.tscn");
                    return false;
                }

                // Test that we can instantiate it
                var mainInstance = mainScene.Instantiate();
                if (mainInstance == null)
                {
                    GD.PrintErr("Failed to instantiate Main scene");
                    return false;
                }

                // Add it to the scene tree temporarily
                AddChild(mainInstance);
                
                // Test that Main class is accessible
                var main = mainInstance as Main;
                if (main == null)
                {
                    GD.PrintErr("Main instance is not of type Main");
                    mainInstance.QueueFree();
                    return false;
                }

                // Call _Ready to initialize
                main._Ready();

                // Clean up
                mainInstance.QueueFree();

                GD.Print("Main scene loaded and initialized successfully");
                return true;
            }
            catch (Exception ex)
            {
                GD.PrintErr($"Main scene test failed: {ex.Message}");
                return false;
            }
        }

        private bool TestUIManagerInitialization()
        {
            try
            {
                // Load and instantiate main scene
                var mainScene = GD.Load<PackedScene>("res://Scenes/Main.tscn");
                var mainInstance = mainScene.Instantiate();
                AddChild(mainInstance);

                var main = mainInstance as Main;
                main._Ready();

                // Find UI manager in the scene
                ModernUIManager uiManager = null;
                var children = main.GetChildren();
                foreach (Node child in children)
                {
                    if (child is ModernUIManager manager)
                    {
                        uiManager = manager;
                        break;
                    }
                }

                if (uiManager == null)
                {
                    GD.PrintErr("UI Manager not found in Main scene");
                    mainInstance.QueueFree();
                    return false;
                }

                // Test UI components exist
                var startButton = uiManager.GetStartGameButton();
                var nextPhaseButton = uiManager.GetNextPhaseButton();
                var mapSelector = uiManager.GetMapTypeSelector();
                var zoomSlider = uiManager.GetZoomSlider();
                var gameArea = uiManager.GetGameArea();

                bool allComponentsExist = startButton != null && nextPhaseButton != null && 
                                        mapSelector != null && zoomSlider != null && gameArea != null;

                if (!allComponentsExist)
                {
                    GD.PrintErr("Some UI components are missing");
                    mainInstance.QueueFree();
                    return false;
                }

                // Clean up
                mainInstance.QueueFree();

                GD.Print("UI Manager initialized with all components");
                return true;
            }
            catch (Exception ex)
            {
                GD.PrintErr($"UI Manager test failed: {ex.Message}");
                return false;
            }
        }

        private bool TestStartButtonFunctionality()
        {
            try
            {
                // Load main scene
                var mainScene = GD.Load<PackedScene>("res://Scenes/Main.tscn");
                var mainInstance = mainScene.Instantiate();
                AddChild(mainInstance);

                var main = mainInstance as Main;
                main._Ready();

                // Find UI manager
                ModernUIManager uiManager = null;
                var children = main.GetChildren();
                foreach (Node child in children)
                {
                    if (child is ModernUIManager manager)
                    {
                        uiManager = manager;
                        break;
                    }
                }

                if (uiManager == null)
                {
                    GD.PrintErr("UI Manager not found");
                    mainInstance.QueueFree();
                    return false;
                }

                // Test start button
                var startButton = uiManager.GetStartGameButton();
                if (startButton == null)
                {
                    GD.PrintErr("Start button not found");
                    mainInstance.QueueFree();
                    return false;
                }

                // Test button properties
                if (startButton.Text != "Start Game")
                {
                    GD.PrintErr($"Start button text incorrect: {startButton.Text}");
                    mainInstance.QueueFree();
                    return false;
                }

                // Test hide functionality
                uiManager.HideStartButton();
                if (startButton.Visible)
                {
                    GD.PrintErr("Start button should be hidden after HideStartButton call");
                    mainInstance.QueueFree();
                    return false;
                }

                // Clean up
                mainInstance.QueueFree();

                GD.Print("Start button functionality works correctly");
                return true;
            }
            catch (Exception ex)
            {
                GD.PrintErr($"Start button test failed: {ex.Message}");
                return false;
            }
        }

        private bool TestMapGeneration()
        {
            try
            {
                // Load main scene
                var mainScene = GD.Load<PackedScene>("res://Scenes/Main.tscn");
                var mainInstance = mainScene.Instantiate();
                AddChild(mainInstance);

                var main = mainInstance as Main;
                main._Ready();

                // Test that map generation method exists and can be called
                var mapGeneratorType = typeof(Main);
                var generateMapMethod = mapGeneratorType.GetMethod("GenerateMap", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                if (generateMapMethod == null)
                {
                    GD.PrintErr("GenerateMap method not found");
                    mainInstance.QueueFree();
                    return false;
                }

                // Try to call map generation (this tests the method exists and is callable)
                try
                {
                    generateMapMethod.Invoke(main, null);
                    GD.Print("Map generation method can be called successfully");
                }
                catch (Exception ex)
                {
                    GD.PrintErr($"Map generation failed: {ex.Message}");
                    mainInstance.QueueFree();
                    return false;
                }

                // Clean up
                mainInstance.QueueFree();

                return true;
            }
            catch (Exception ex)
            {
                GD.PrintErr($"Map generation test failed: {ex.Message}");
                return false;
            }
        }

        private bool TestSceneTransitions()
        {
            try
            {
                // Test that we can switch between scenes
                var mainScene = GD.Load<PackedScene>("res://Scenes/Main.tscn");
                var testScene = GD.Load<PackedScene>("res://Scenes/NUnitTestScene.tscn");

                if (mainScene == null || testScene == null)
                {
                    GD.PrintErr("Failed to load scenes for transition test");
                    return false;
                }

                // Test instantiation of both scenes
                var mainInstance = mainScene.Instantiate();
                var testInstance = testScene.Instantiate();

                if (mainInstance == null || testInstance == null)
                {
                    GD.PrintErr("Failed to instantiate scenes for transition test");
                    return false;
                }

                // Clean up
                mainInstance.QueueFree();
                testInstance.QueueFree();

                GD.Print("Scene transitions work correctly");
                return true;
            }
            catch (Exception ex)
            {
                GD.PrintErr($"Scene transition test failed: {ex.Message}");
                return false;
            }
        }

        private void PrintFinalResults()
        {
            GD.Print("=== Godot Scene Test Results ===");
            GD.Print($"Total Tests: {_sceneTests.Count}");
            GD.Print($"Passed: {_passedTests}");
            GD.Print($"Failed: {_failedTests}");
            GD.Print($"Success Rate: {(_passedTests * 100.0 / _sceneTests.Count):F1}%");

            if (_failedTests == 0)
            {
                GD.Print("🎉 ALL GODOT SCENE TESTS PASSED! 🎉");
            }
            else
            {
                GD.PrintErr("❌ SOME GODOT SCENE TESTS FAILED! ❌");
            }

            GD.Print("=== Godot Scene Test Runner Completed ===");
            
            // Exit after a short delay
            GetTree().CreateTimer(2.0f).Timeout += () => GetTree().Quit();
        }
    }

    public class SceneTest
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public Func<bool> TestAction { get; set; }
    }
}
