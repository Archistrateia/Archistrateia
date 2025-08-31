using NUnit.Framework;
using Godot;
using Archistrateia;

[TestFixture]
public partial class VisualDisplayTest : Node
{
    [Test]
    public void Should_Format_Movement_Points_Display_Text()
    {
        var unit = new Charioteer();
        
        var displayText = MovementDisplayLogic.GetMovementDisplayText(unit);
        
        var expectedText = $"MP: {unit.CurrentMovementPoints}";
        Assert.AreEqual(expectedText, displayText);
    }
    
    [Test]
    public void Should_Update_Display_Text_After_Movement()
    {
        var unit = new Nakhtu();
        
        var initialText = MovementDisplayLogic.GetMovementDisplayText(unit);
        var expectedInitialText = $"MP: {unit.CurrentMovementPoints}";
        Assert.AreEqual(expectedInitialText, initialText);
        
        var originalMP = unit.CurrentMovementPoints;
        unit.CurrentMovementPoints = originalMP - 1;
        var updatedText = MovementDisplayLogic.GetMovementDisplayText(unit);
        var expectedUpdatedText = $"MP: {unit.CurrentMovementPoints}";
        Assert.AreEqual(expectedUpdatedText, updatedText);
        
        unit.CurrentMovementPoints = 0;
        var finalText = MovementDisplayLogic.GetMovementDisplayText(unit);
        Assert.AreEqual("MP: 0", finalText);
    }
    
    [Test]
    public void Should_Determine_When_To_Show_Movement_Display()
    {
        var unit = new Charioteer();
        
        bool shouldShow = MovementDisplayLogic.ShouldShowMovementDisplay(unit, isSelected: true);
        Assert.IsTrue(shouldShow);
        
        bool shouldNotShow = MovementDisplayLogic.ShouldShowMovementDisplay(unit, isSelected: false);
        Assert.IsFalse(shouldNotShow);
        
        unit.CurrentMovementPoints = 0;
        bool shouldShowEmpty = MovementDisplayLogic.ShouldShowMovementDisplay(unit, isSelected: true);
        Assert.IsTrue(shouldShowEmpty);
    }

    [Test]
    public void Should_Create_UnitRendererLogic_Directly()
    {
        var unitRenderer = new UnitRendererLogic();
        Assert.IsNotNull(unitRenderer, "Should be able to create UnitRendererLogic directly");
    }

    [Test]
    public void Should_Calculate_Unit_Display_Data()
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
    public void Should_Track_Selection_State()
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
    public void Should_Update_Position()
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
    public void Should_Calculate_Vertices_For_Unit_Shape()
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

    [Test]
    public void Should_Immediately_Clear_Visual_Overlays()
    {
        var visualTile = new VisualHexTile();
        visualTile.Initialize(new Vector2I(0, 0), TerrainType.Shoreline, Colors.Blue, Vector2.Zero);
        
        GD.Print("=== TESTING IMMEDIATE OVERLAY REMOVAL ===");
        
        visualTile.SetGrayed(true);
        Assert.IsTrue(visualTile.IsGrayed(), "Tile should be grayed");
        
        visualTile.SetGrayed(false);
        Assert.IsFalse(visualTile.IsGrayed(), "Tile should not be grayed");
        
        visualTile.SetBrightened(true);
        Assert.IsTrue(visualTile.IsBrightened(), "Tile should be brightened");
        
        visualTile.SetBrightened(false);
        Assert.IsFalse(visualTile.IsBrightened(), "Tile should not be brightened");
        
        GD.Print("✅ Visual overlay states are correctly managed!");
        
        visualTile.QueueFree();
    }
    
    [Test]
    public void Should_Handle_Multiple_Overlay_State_Changes()
    {
        var visualTile = new VisualHexTile();
        visualTile.Initialize(new Vector2I(1, 1), TerrainType.Desert, Colors.Brown, Vector2.Zero);
        
        GD.Print("=== TESTING MULTIPLE OVERLAY STATE CHANGES ===");
        
        visualTile.SetGrayed(true);
        Assert.IsTrue(visualTile.IsGrayed(), "Should be grayed");
        
        visualTile.SetBrightened(true);
        Assert.IsTrue(visualTile.IsBrightened(), "Should be brightened");
        
        visualTile.SetGrayed(false);
        visualTile.SetBrightened(false);
        
        Assert.IsFalse(visualTile.IsGrayed(), "Should not be grayed");
        Assert.IsFalse(visualTile.IsBrightened(), "Should not be brightened");
        
        GD.Print("✅ Multiple overlay state changes handled correctly!");
        
        visualTile.QueueFree();
    }
} 