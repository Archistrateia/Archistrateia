using NUnit.Framework;
using Godot;

[TestFixture]
public partial class UnitRendererLogicTest : Node
{
    [Test]
    public void Should_Create_UnitRendererLogic_Directly()
    {
        var unitRenderer = new UnitRendererLogic();
        Assert.IsNotNull(unitRenderer, "Should be able to create UnitRendererLogic directly");
    }

    [Test]
    public void UnitRendererLogic_Should_Calculate_Unit_Display_Data()
    {
        var unitRenderer = new UnitRendererLogic();
        Assert.IsNotNull(unitRenderer, "UnitRenderer should not be null");
        
        var unit = new Nakhtu();
        Assert.IsNotNull(unit, "Unit should not be null");
        
        var position = new Vector2(100, 100);
        var color = new Color(1.0f, 0.0f, 0.0f);

        var displayData = unitRenderer.CreateUnitDisplayData(unit, position, color);

        Assert.IsNotNull(displayData);
        Assert.AreEqual(unit, displayData.LogicalUnit);
        Assert.AreEqual(position, displayData.Position);
        Assert.AreEqual(color, displayData.Color);
        Assert.AreEqual("N", displayData.DisplayText);
    }

    [Test]
    public void UnitRendererLogic_Should_Track_Selection_State()
    {
        var unitRenderer = new UnitRendererLogic();
        var unit = new Nakhtu();
        var position = new Vector2(100, 100);
        var color = new Color(1.0f, 0.0f, 0.0f);

        var displayData = unitRenderer.CreateUnitDisplayData(unit, position, color);
        
        Assert.IsFalse(displayData.IsSelected);
        
        unitRenderer.SetSelected(displayData, true);
        Assert.IsTrue(displayData.IsSelected);
        
        unitRenderer.SetSelected(displayData, false);
        Assert.IsFalse(displayData.IsSelected);
    }

    [Test]
    public void UnitRendererLogic_Should_Update_Position()
    {
        var unitRenderer = new UnitRendererLogic();
        var unit = new Nakhtu();
        var initialPosition = new Vector2(100, 100);
        var newPosition = new Vector2(200, 200);
        var color = new Color(1.0f, 0.0f, 0.0f);

        var displayData = unitRenderer.CreateUnitDisplayData(unit, initialPosition, color);
        unitRenderer.UpdatePosition(displayData, newPosition);

        Assert.AreEqual(newPosition, displayData.Position);
    }

    [Test]
    public void UnitRendererLogic_Should_Calculate_Vertices_For_Unit_Shape()
    {
        var unitRenderer = new UnitRendererLogic();
        var vertices = unitRenderer.CalculateUnitVertices(15.0f);
        
        Assert.IsNotNull(vertices);
        Assert.AreEqual(8, vertices.Length);
        
        for (int i = 0; i < vertices.Length; i++)
        {
            var distance = vertices[i].Length();
            Assert.AreEqual(15.0f, distance, 0.01f, $"Vertex {i} should be at radius 15");
        }
    }
} 