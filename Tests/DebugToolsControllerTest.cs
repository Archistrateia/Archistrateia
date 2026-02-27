using Godot;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using Archistrateia;

[TestFixture]
public class DebugToolsControllerTest
{
    [Test]
    public void ToggleDebugAdjacentMode_Should_Enable_Mode_And_Update_Button_Text()
    {
        var tiles = new List<FakeDebugHexTile>();
        var controller = CreateController(tiles);
        var button = new Button();

        controller.ToggleDebugAdjacentMode(button);

        Assert.IsTrue(controller.IsDebugAdjacentModeEnabled());
        Assert.AreEqual("Debug Adjacent: ON", button.Text);
    }

    [Test]
    public void ToggleDebugAdjacentMode_Should_Clear_Highlights_When_Disabling()
    {
        var tiles = new List<FakeDebugHexTile>
        {
            new(new Vector2I(0, 0)) { Highlighted = true },
            new(new Vector2I(1, 0)) { Highlighted = true }
        };
        var controller = CreateController(tiles);
        var button = new Button();
        controller.ToggleDebugAdjacentMode(button);

        controller.ToggleDebugAdjacentMode(button);

        Assert.IsFalse(controller.IsDebugAdjacentModeEnabled());
        Assert.AreEqual("Debug Adjacent", button.Text);
        Assert.IsTrue(tiles.All(tile => !tile.Highlighted));
    }

    [Test]
    public void HandleDebugAdjacentHover_Should_Highlight_Hovered_And_Adjacent_Tiles()
    {
        var center = new FakeDebugHexTile(new Vector2I(1, 1));
        var adjacent = new FakeDebugHexTile(new Vector2I(1, 2));
        var unrelated = new FakeDebugHexTile(new Vector2I(4, 4));
        var tiles = new List<FakeDebugHexTile> { center, adjacent, unrelated };
        var controller = CreateController(
            tiles,
            hitTest: (tile, _) => ReferenceEquals(tile, center));
        controller.ToggleDebugAdjacentMode(new Button());

        controller.HandleDebugAdjacentHover(Vector2.Zero);

        Assert.IsTrue(center.Highlighted);
        Assert.AreEqual(new Color(1.0f, 1.0f, 0.0f, 0.5f), center.HighlightColor);
        Assert.IsTrue(adjacent.Highlighted);
        Assert.AreEqual(new Color(0.0f, 1.0f, 0.0f, 0.5f), adjacent.HighlightColor);
        Assert.IsFalse(unrelated.Highlighted);
    }

    [Test]
    public void HandleDebugAdjacentHover_Should_Do_Nothing_When_Mode_Is_Disabled()
    {
        var hovered = new FakeDebugHexTile(new Vector2I(1, 1));
        var controller = CreateController(
            new List<FakeDebugHexTile> { hovered },
            hitTest: (tile, _) => true);

        controller.HandleDebugAdjacentHover(Vector2.Zero);

        Assert.IsFalse(hovered.Highlighted);
    }

    private static DebugToolsController CreateController(
        List<FakeDebugHexTile> tiles,
        System.Func<IDebugHexTile, Vector2, bool> hitTest = null)
    {
        return new DebugToolsController(
            () => tiles.Cast<IDebugHexTile>(),
            hitTest,
            _ => { });
    }

    private sealed class FakeDebugHexTile : IDebugHexTile
    {
        public Vector2I GridPosition { get; }
        public bool Highlighted { get; set; }
        public Color HighlightColor { get; private set; }

        public FakeDebugHexTile(Vector2I gridPosition)
        {
            GridPosition = gridPosition;
        }

        public Vector2 ToLocal(Vector2 globalPosition)
        {
            return globalPosition;
        }

        public bool ContainsLocalPoint(Vector2 localPoint)
        {
            return true;
        }

        public void SetHighlight(bool highlight, Color highlightColor = default)
        {
            Highlighted = highlight;
            HighlightColor = highlightColor;
        }
    }
}
