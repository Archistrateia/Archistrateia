using NUnit.Framework;
using Godot;

namespace Archistrateia.Tests
{
    [TestFixture]
    public partial class ModernUIManagerTest : Node
    {
        [Test]
        public void ModernUIManager_Should_Be_Defined()
        {
            // Test that the ModernUIManager class exists and can be referenced
            Assert.IsNotNull(typeof(ModernUIManager), "ModernUIManager class should be defined");
            Assert.IsTrue(typeof(ModernUIManager).IsSubclassOf(typeof(Control)), "ModernUIManager should inherit from Control");
        }

        [Test]
        public void ModernUIManager_Should_Have_Required_Methods()
        {
            var type = typeof(ModernUIManager);
            
            // Test that all required methods exist
            Assert.IsNotNull(type.GetMethod("GetStartGameButton"), "GetStartGameButton method should exist");
            Assert.IsNotNull(type.GetMethod("GetNextPhaseButton"), "GetNextPhaseButton method should exist");
            Assert.IsNotNull(type.GetMethod("GetMapTypeSelector"), "GetMapTypeSelector method should exist");
            Assert.IsNotNull(type.GetMethod("GetZoomSlider"), "GetZoomSlider method should exist");
            Assert.IsNotNull(type.GetMethod("GetGameArea"), "GetGameArea method should exist");
            Assert.IsNotNull(type.GetMethod("UpdatePlayerInfo"), "UpdatePlayerInfo method should exist");
            Assert.IsNotNull(type.GetMethod("HideStartButton"), "HideStartButton method should exist");
        }

        [Test]
        public void ModernUIManager_Should_Have_Required_Properties()
        {
            var type = typeof(ModernUIManager);
            
            // Test that the class has the expected structure
            var fields = type.GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            bool hasStartButton = false;
            bool hasNextPhaseButton = false;
            bool hasMapSelector = false;
            bool hasZoomSlider = false;
            bool hasGameArea = false;
            
            foreach (var field in fields)
            {
                if (field.Name == "_startGameButton") hasStartButton = true;
                if (field.Name == "_nextPhaseButton") hasNextPhaseButton = true;
                if (field.Name == "_mapTypeSelector") hasMapSelector = true;
                if (field.Name == "_zoomSlider") hasZoomSlider = true;
                if (field.Name == "_gameArea") hasGameArea = true;
            }
            
            Assert.IsTrue(hasStartButton, "ModernUIManager should have _startGameButton field");
            Assert.IsTrue(hasNextPhaseButton, "ModernUIManager should have _nextPhaseButton field");
            Assert.IsTrue(hasMapSelector, "ModernUIManager should have _mapTypeSelector field");
            Assert.IsTrue(hasZoomSlider, "ModernUIManager should have _zoomSlider field");
            Assert.IsTrue(hasGameArea, "ModernUIManager should have _gameArea field");
        }

        [Test]
        public void ModernUIManager_Should_Have_Correct_Method_Signatures()
        {
            var type = typeof(ModernUIManager);
            
            // Test method signatures
            var updatePlayerInfoMethod = type.GetMethod("UpdatePlayerInfo");
            Assert.IsNotNull(updatePlayerInfoMethod, "UpdatePlayerInfo method should exist");
            
            var parameters = updatePlayerInfoMethod.GetParameters();
            Assert.AreEqual(3, parameters.Length, "UpdatePlayerInfo should have 3 parameters");
            Assert.AreEqual(typeof(string), parameters[0].ParameterType, "First parameter should be string (playerName)");
            Assert.AreEqual(typeof(string), parameters[1].ParameterType, "Second parameter should be string (phase)");
            Assert.AreEqual(typeof(int), parameters[2].ParameterType, "Third parameter should be int (turn)");
        }
    }
}
