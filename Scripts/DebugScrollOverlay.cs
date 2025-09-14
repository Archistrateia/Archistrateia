using Godot;
using System.Linq;

namespace Archistrateia.Debug
{
    /// <summary>
    /// Debug overlay that visualizes the scroll areas where mouse hover triggers map scrolling.
    /// Shows translucent red rectangles at the edges of the game area where scrolling is active.
    /// Press F3 to toggle visibility.
    /// </summary>
    public partial class DebugScrollOverlay : Control
{
    private bool _isVisible = false;
    private Color _scrollAreaColor = new Color(1.0f, 0.0f, 0.0f, 0.3f); // Semi-transparent red
    private Color _uiExclusionColor = new Color(0.0f, 1.0f, 0.0f, 0.3f); // Semi-transparent green
    
    public new bool IsVisible
    {
        get => _isVisible;
        set
        {
            _isVisible = value;
            base.Visible = value;
        }
    }
    
    public override void _Ready()
    {
        Name = "DebugScrollOverlay";
        MouseFilter = Control.MouseFilterEnum.Ignore; // Allow mouse events to pass through
        Visible = false;
    }
    
    public void UpdateScrollAreas(Vector2 gameAreaSize, float edgeScrollThreshold, bool isScrollingNeeded, Vector2 areaPosition = default)
    {
        if (!_isVisible) return;
        
        // Position the debug overlay at the game grid area
        Position = areaPosition;
        
        // Clear existing children
        foreach (Node child in GetChildren())
        {
            child.QueueFree();
        }
        
        if (!isScrollingNeeded)
        {
            return; // No scroll areas to show if scrolling isn't needed
        }
        
        // Create scroll area rectangles (positioned relative to the overlay)
        CreateScrollAreaRectangles(gameAreaSize, edgeScrollThreshold, Vector2.Zero);
    }
    
    private void CreateScrollAreaRectangles(Vector2 gameAreaSize, float edgeScrollThreshold, Vector2 areaPosition = default)
    {
        // Left edge scroll area
        var leftRect = new ColorRect();
        leftRect.Color = _scrollAreaColor;
        leftRect.Position = areaPosition;
        leftRect.Size = new Vector2(edgeScrollThreshold, gameAreaSize.Y);
        AddChild(leftRect);
        
        // Right edge scroll area
        var rightRect = new ColorRect();
        rightRect.Color = _scrollAreaColor;
        rightRect.Position = areaPosition + new Vector2(gameAreaSize.X - edgeScrollThreshold, 0);
        rightRect.Size = new Vector2(edgeScrollThreshold, gameAreaSize.Y);
        AddChild(rightRect);
        
        // Top edge scroll area
        var topRect = new ColorRect();
        topRect.Color = _scrollAreaColor;
        topRect.Position = areaPosition;
        topRect.Size = new Vector2(gameAreaSize.X, edgeScrollThreshold);
        AddChild(topRect);
        
        // Bottom edge scroll area
        var bottomRect = new ColorRect();
        bottomRect.Color = _scrollAreaColor;
        bottomRect.Position = areaPosition + new Vector2(0, gameAreaSize.Y - edgeScrollThreshold);
        bottomRect.Size = new Vector2(gameAreaSize.X, edgeScrollThreshold);
        AddChild(bottomRect);
    }
    
    public void UpdateUIExclusions(Vector2 mousePosition, bool isOverUIControls)
    {
        if (!_isVisible) return;
        
        // Remove any existing UI exclusion indicators
        var existingExclusions = GetChildren().OfType<ColorRect>().Where(cr => cr.Color == _uiExclusionColor).ToList();
        foreach (var exclusion in existingExclusions)
        {
            exclusion.QueueFree();
        }
        
        if (isOverUIControls)
        {
            // Create a small indicator where the mouse is over UI
            var uiIndicator = new ColorRect();
            uiIndicator.Color = _uiExclusionColor;
            uiIndicator.Position = mousePosition - Vector2.One * 10; // 20x20 indicator centered on mouse
            uiIndicator.Size = Vector2.One * 20;
            AddChild(uiIndicator);
        }
    }
    
    public void ToggleVisibility()
    {
        IsVisible = !IsVisible;
        GD.Print($"🔍 Debug scroll overlay {(IsVisible ? "enabled" : "disabled")}");
    }
    }
}
