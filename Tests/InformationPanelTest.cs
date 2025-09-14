using NUnit.Framework;
using Godot;

namespace Archistrateia.Tests
{
    [TestFixture]
    public partial class InformationPanelTest : Node
    {
        private InformationPanel _infoPanel;

        [SetUp]
        public void Setup()
        {
            _infoPanel = new InformationPanel();
            AddChild(_infoPanel);
            
            // Wait for the panel to be ready
            _infoPanel._Ready();
            
            // Verify panel is initialized
            Assert.IsNotNull(_infoPanel, "Information panel should be initialized");
        }

        [Test]
        public void InformationPanel_Should_Be_Hidden_Initially()
        {
            Assert.IsFalse(_infoPanel.Visible, "Information panel should be hidden initially");
        }

        [Test]
        public void InformationPanel_Should_Show_Terrain_Info()
        {
            var terrainType = TerrainType.Desert;
            var movementCost = 2;
            var position = new Vector2(100, 100);
            
            _infoPanel.ShowTerrainInfo(terrainType, movementCost, position);
            
            Assert.IsTrue(_infoPanel.Visible, "Panel should be visible after showing terrain info");
            Assert.AreEqual(new Vector2(200, 80), _infoPanel.Size, "Panel should have correct size");
        }

        [Test]
        public void InformationPanel_Should_Update_Position_Correctly()
        {
            var mousePosition = new Vector2(50, 50);
            var expectedPosition = mousePosition + new Vector2(10, 10);
            
            _infoPanel.ShowTerrainInfo(TerrainType.Hill, 1, mousePosition);
            
            Assert.AreEqual(expectedPosition, _infoPanel.Position, "Panel should position correctly relative to mouse");
        }

        [Test]
        public void InformationPanel_Should_Handle_Viewport_Bounds()
        {
            var viewportSize = GetViewport().GetVisibleRect().Size;
            var mousePosition = new Vector2(viewportSize.X - 50, viewportSize.Y - 50);
            
            _infoPanel.ShowTerrainInfo(TerrainType.River, 3, mousePosition);
            
            Assert.IsTrue(_infoPanel.Position.X < viewportSize.X, "Panel should stay within viewport bounds");
            Assert.IsTrue(_infoPanel.Position.Y < viewportSize.Y, "Panel should stay within viewport bounds");
        }

        [Test]
        public void InformationPanel_Should_Show_Unit_Info()
        {
            var unit = new Nakhtu();
            var terrainType = TerrainType.Grassland;
            var movementCost = 1;
            var position = new Vector2(200, 200);
            
            _infoPanel.ShowUnitInfo(unit, terrainType, movementCost, position);
            
            Assert.IsTrue(_infoPanel.Visible, "Panel should be visible after showing unit info");
        }

        [Test]
        public void InformationPanel_Should_Hide_When_Called()
        {
            _infoPanel.ShowTerrainInfo(TerrainType.Mountain, 4, new Vector2(100, 100));
            
            Assert.IsTrue(_infoPanel.Visible, "Panel should be visible before hiding");
            
            _infoPanel.Hide();
            
            Assert.IsFalse(_infoPanel.Visible, "Panel should be hidden after Hide() call");
        }

        [Test]
        public void InformationPanel_Should_Be_Panel_Node()
        {
            Assert.IsTrue(_infoPanel is Panel, "InformationPanel should inherit from Panel");
            Assert.AreEqual("InformationPanel", _infoPanel.Name, "Panel should have correct name");
        }

        [Test]
        public void InformationPanel_Should_Handle_UpdatePosition()
        {
            var testPosition = new Vector2(300, 400);
            
            Assert.DoesNotThrow(() => {
                _infoPanel.UpdatePosition(testPosition);
            }, "UpdatePosition should not throw exceptions");
        }
    }
}
