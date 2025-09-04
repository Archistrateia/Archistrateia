using Godot;

public partial class InformationPanel : Panel
{
    private Label _terrainLabel;
    private Label _movementCostLabel;
    private Label _unitLabel;
    private VBoxContainer _container;

    public override void _Ready()
    {
        Name = "InformationPanel";
        
        // Create container
        _container = new VBoxContainer();
        AddChild(_container);
        
        // Create labels
        _terrainLabel = new Label();
        _terrainLabel.Text = "Terrain: ";
        _container.AddChild(_terrainLabel);
        
        _movementCostLabel = new Label();
        _movementCostLabel.Text = "Movement Cost: ";
        _container.AddChild(_movementCostLabel);
        
        _unitLabel = new Label();
        _unitLabel.Text = "Unit: ";
        _container.AddChild(_unitLabel);
        
        // Style the panel
        var style = new StyleBoxFlat();
        style.BgColor = new Color(0, 0, 0, 0.8f);
        style.BorderColor = new Color(1, 1, 1, 0.5f);
        style.BorderWidthTop = 2;
        style.BorderWidthBottom = 2;
        style.BorderWidthLeft = 2;
        style.BorderWidthRight = 2;
        AddThemeStyleboxOverride("panel", style);
        
        // Set initial size and position
        Size = new Vector2(200, 80);
        SetVisible(false);
    }

    public void ShowTerrainInfo(TerrainType terrainType, int movementCost, Vector2 position)
    {
        _terrainLabel.Text = $"Terrain: {terrainType}";
        _movementCostLabel.Text = $"Movement Cost: {movementCost}";
        _unitLabel.Text = "Unit: None";
        
        UpdatePosition(position);
        SetVisible(true);
    }

    public void ShowUnitInfo(Unit unit, TerrainType terrainType, int movementCost, Vector2 position)
    {
        _terrainLabel.Text = $"Terrain: {terrainType}";
        _movementCostLabel.Text = $"Movement Cost: {movementCost}";
        _unitLabel.Text = $"Unit: {unit.Name}";
        
        UpdatePosition(position);
        SetVisible(true);
    }

    public new void Hide()
    {
        SetVisible(false);
    }

    public void UpdatePosition(Vector2 mousePosition)
    {
        var viewportSize = GetViewport().GetVisibleRect().Size;
        Vector2 panelPos = mousePosition + new Vector2(10, 10);
        
        // Keep panel within viewport bounds
        if (panelPos.X + Size.X > viewportSize.X)
            panelPos.X = mousePosition.X - Size.X - 10;
        if (panelPos.Y + Size.Y > viewportSize.Y)
            panelPos.Y = mousePosition.Y - Size.Y - 10;
            
        Position = panelPos;
    }
}