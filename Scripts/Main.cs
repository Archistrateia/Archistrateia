using Godot;
using System.Collections.Generic;
using Archistrateia;

public partial class Main : Control
{
    [Export]
    public TurnManager TurnManager { get; set; }

    [Export]
    public Label TitleLabel { get; set; }

    [Export]
    public Button StartButton { get; set; }

    private Button _nextPhaseButton;
    private Node2D _mapContainer;
    private Dictionary<TerrainType, Color> _terrainColors;
    private const int MAP_WIDTH = 10;
    private const int MAP_HEIGHT = 10;

    public override void _Ready()
    {
        GD.Print("=== MAIN: _Ready() called ===");
        GD.Print("🚀 NEW VERSION OF MAIN.CS IS LOADED! 🚀");
        GD.Print($"Main loaded. Node path: {GetPath()}");

        // Try to find UI elements if references are null
        if (StartButton == null)
        {
            GD.Print("StartButton reference is null, trying to find it...");
            StartButton = GetNodeOrNull<Button>("UI/StartButton");
            if (StartButton != null)
            {
                GD.Print("Found StartButton in scene!");
            }
            else
            {
                GD.PrintErr("ERROR: Could not find StartButton in scene!");
            }
        }

        if (TitleLabel == null)
        {
            GD.Print("TitleLabel reference is null, trying to find it...");
            TitleLabel = GetNodeOrNull<Label>("UI/TitleLabel");
            if (TitleLabel != null)
            {
                GD.Print("Found TitleLabel in scene!");
            }
            else
            {
                GD.PrintErr("ERROR: Could not find TitleLabel in scene!");
            }
        }

        GD.Print($"StartButton reference: {(StartButton != null ? "VALID" : "NULL")}");
        GD.Print($"TitleLabel reference: {(TitleLabel != null ? "VALID" : "NULL")}");
        GD.Print($"TurnManager reference: {(TurnManager != null ? "VALID" : "NULL")}");

        InitializeTerrainColors();
        UpdateTitleLabel();

        GD.Print("=== MAIN: _Ready() completed ===");
    }

    private void InitializeTerrainColors()
    {
        GD.Print("=== MAIN: InitializeTerrainColors() called ===");
        _terrainColors = new Dictionary<TerrainType, Color>
        {
            { TerrainType.Desert, new Color(0.9f, 0.8f, 0.6f) },
            { TerrainType.Hill, new Color(0.6f, 0.5f, 0.3f) },
            { TerrainType.River, new Color(0.3f, 0.6f, 0.9f) },
            { TerrainType.Shoreline, new Color(0.8f, 0.7f, 0.5f) },
            { TerrainType.Lagoon, new Color(0.2f, 0.5f, 0.7f) }
        };
        GD.Print($"Initialized {_terrainColors.Count} terrain colors");
        GD.Print("=== MAIN: InitializeTerrainColors() completed ===");
    }

    public void OnStartButtonPressed()
    {
        GD.Print("=== MAIN: OnStartButtonPressed() called ===");
        GD.Print("Start Button Pressed. Game Starting...");

        // Try to find StartButton if reference is null
        if (StartButton == null)
        {
            GD.Print("StartButton reference is null, trying to find it...");
            StartButton = GetNodeOrNull<Button>("UI/StartButton");
            if (StartButton != null)
            {
                GD.Print("Found StartButton in scene!");
            }
            else
            {
                GD.PrintErr("ERROR: Could not find StartButton in scene!");
            }
        }

        if (StartButton != null)
        {
            GD.Print("Hiding StartButton");
            StartButton.Visible = false;
        }
        else
        {
            GD.PrintErr("ERROR: StartButton is still null!");
        }

        GD.Print("Calling GenerateMap()");
        GenerateMap();

        GD.Print("Creating Next Phase button");
        _nextPhaseButton = new Button();
        _nextPhaseButton.Text = "Next Phase";
        _nextPhaseButton.Pressed += OnNextPhaseButtonPressed;
        AddChild(_nextPhaseButton);
        GD.Print("Next Phase button added to scene");

        GD.Print("=== MAIN: OnStartButtonPressed() completed ===");
    }

    private void GenerateMap()
    {
        GD.Print("=== MAIN: GenerateMap() called ===");

        if (_mapContainer != null)
        {
            GD.Print("Clearing existing map container");
            _mapContainer.QueueFree();
        }

        GD.Print("Creating new map container");
        _mapContainer = new Node2D();
        _mapContainer.Name = "MapContainer";
        AddChild(_mapContainer);
        GD.Print($"MapContainer added to scene at path: {_mapContainer.GetPath()}");

        int tilesCreated = 0;
        for (int x = 0; x < MAP_WIDTH; x++)
        {
            for (int y = 0; y < MAP_HEIGHT; y++)
            {
                var terrainType = GetRandomTerrainType();
                var tile = CreateHexTile(x, y, terrainType);
                _mapContainer.AddChild(tile);
                tilesCreated++;

                if (tilesCreated % 25 == 0)
                {
                    GD.Print($"Created {tilesCreated} tiles...");
                }
            }
        }

        GD.Print($"Generated hex map with {tilesCreated} tiles");
        GD.Print($"MapContainer now has {_mapContainer.GetChildCount()} children");
        GD.Print("=== MAIN: GenerateMap() completed ===");
    }

    private TerrainType GetRandomTerrainType()
    {
        var terrainTypes = System.Enum.GetValues<TerrainType>();
        var randomType = terrainTypes[GD.RandRange(0, terrainTypes.Length - 1)];
        return randomType;
    }

    private Node2D CreateHexTile(int x, int y, TerrainType terrainType)
    {
        var tile = new Node2D();
        tile.Name = $"HexTile_{x}_{y}";

        var hexShape = new Polygon2D();
        hexShape.Polygon = HexGridCalculator.CreateHexagonVertices();
        hexShape.Color = _terrainColors[terrainType];
        hexShape.Position = HexGridCalculator.CalculateHexPositionCentered(x, y, GetViewport().GetVisibleRect().Size, MAP_WIDTH, MAP_HEIGHT);

        var outline = new Line2D();
        outline.Points = HexGridCalculator.CreateHexagonVertices();
        outline.DefaultColor = new Color(0.2f, 0.2f, 0.2f);
        outline.Width = 2.0f;
        outline.Position = HexGridCalculator.CalculateHexPositionCentered(x, y, GetViewport().GetVisibleRect().Size, MAP_WIDTH, MAP_HEIGHT);

        tile.AddChild(hexShape);
        tile.AddChild(outline);

        return tile;
    }



    private void OnNextPhaseButtonPressed()
    {
        GD.Print("=== MAIN: OnNextPhaseButtonPressed() called ===");
        GD.Print("Next Phase button pressed.");

        if (TurnManager != null)
        {
            GD.Print("Advancing phase via TurnManager");
            TurnManager.AdvancePhase();
        }
        else
        {
            GD.PrintErr("ERROR: TurnManager is null!");
        }

        UpdateTitleLabel();
        GD.Print("=== MAIN: OnNextPhaseButtonPressed() completed ===");
    }

    private void UpdateTitleLabel()
    {
        GD.Print("=== MAIN: UpdateTitleLabel() called ===");

        if (TitleLabel != null && TurnManager != null)
        {
            var newText = $"Turn {TurnManager.CurrentTurn} - {TurnManager.CurrentPhase}";
            GD.Print($"Updating title to: {newText}");
            TitleLabel.Text = newText;
        }
        else
        {
            GD.PrintErr($"ERROR: TitleLabel is {(TitleLabel == null ? "NULL" : "VALID")}, TurnManager is {(TurnManager == null ? "NULL" : "VALID")}");
        }

        GD.Print("=== MAIN: UpdateTitleLabel() completed ===");
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventKey keyEvent && keyEvent.Pressed && keyEvent.Keycode == Key.Space)
        {
            GD.Print("=== MAIN: Space key pressed ===");
            if (TurnManager != null)
            {
                TurnManager.AdvancePhase();
                UpdateTitleLabel();
            }
            else
            {
                GD.PrintErr("ERROR: TurnManager is null on space key press!");
            }
        }
    }
}
