using Godot;
using System.Collections.Generic;
using System.Linq;
using Archistrateia;

public partial class GameManager : Node
{
    public TurnManager TurnManager { get; private set; }
    public List<Player> Players { get; private set; } = new List<Player>();
    public Dictionary<Vector2I, HexTile> GameMap { get; private set; } = new Dictionary<Vector2I, HexTile>();
    public GodotMovementSystem MovementSystem { get; private set; }
    public MapRenderer MapRenderer { get; private set; }
    private bool _initialized = false;

    public GameManager()
    {
        // Constructor doesn't initialize - _Ready() handles it
    }

    public override void _Ready()
    {
        // Don't auto-initialize - wait for SetGameMap() and manual InitializeGame() call
    }

    public void InitializeGame()
    {
        if (_initialized)
        {
            GD.Print("GameManager already initialized, skipping");
            return;
        }
        
        _initialized = true;
        
        TurnManager = new TurnManager();
        AddChild(TurnManager);

        CreatePlayers();
        
        // Only create a new map if one hasn't been set already
        if (GameMap.Count == 0)
        {
            CreateGameMap();
        }
        else
        {
            GD.Print($"Using existing game map with {GameMap.Count} tiles");
            // Still need to add tiles to scene tree and create cities
            foreach (var tile in GameMap.Values)
            {
                AddChild(tile);
            }
            CreateCities();
        }
        
        SetupInitialUnits();
        
        MovementSystem = new GodotMovementSystem();
        AddChild(MovementSystem);
        MovementSystem.InitializeNavigation(GameMap);
        MovementValidationLogic.SetMovementSystem(MovementSystem);

        // Connect phase change signal to clear movement displays
        TurnManager.PhaseChanged += OnPhaseChanged;

        GD.Print("Archistrateia game initialized successfully!");
    }



    private void OnPhaseChanged(int oldPhase, int newPhase)
    {
        GD.Print($"🔄 GameManager.OnPhaseChanged: {(GamePhase)oldPhase} → {(GamePhase)newPhase}");
        
        // Notify MapRenderer of phase change to clear movement displays
        if (MapRenderer != null)
        {
            GD.Print($"   📡 Forwarding to MapRenderer");
            MapRenderer.OnPhaseChanged((GamePhase)newPhase);
        }
        else
        {
            GD.Print($"   ❌ MapRenderer is null - cannot forward phase change");
        }
    }

    public void SetMapRenderer(MapRenderer mapRenderer)
    {
        MapRenderer = mapRenderer;
    }

    private void CreatePlayers()
    {
        var player1 = new Player("Pharaoh", 150);
        var player2 = new Player("General", 150);

        Players.Add(player1);
        Players.Add(player2);

        GD.Print($"Created players: {player1.Name} and {player2.Name}");
    }

    private void CreateGameMap()
    {
        // Generate default map if no map is provided
        GameMap = MapGenerator.GenerateMap(MapConfiguration.MAP_WIDTH, MapConfiguration.MAP_HEIGHT, MapType.Continental);
        
        foreach (var tile in GameMap.Values)
        {
            AddChild(tile);
        }

        CreateCities();
        GD.Print($"Created game map with {GameMap.Count} tiles");
    }

    public void SetGameMap(Dictionary<Vector2I, HexTile> gameMap)
    {
        GameMap = gameMap;
        GD.Print($"Set game map from preview with {GameMap.Count} tiles");
    }

    private void CreateCities()
    {
        var city1 = new City("Memphis", 15, true);
        var city2 = new City("Thebes", 12);
        var city3 = new City("Alexandria", 10);

        city1.SetOwner(Players[0]);
        city2.SetOwner(Players[1]);

        GameMap[new Vector2I(1, 1)].SetCity(city1);
        GameMap[new Vector2I(6, 4)].SetCity(city2);
        GameMap[new Vector2I(3, 2)].SetCity(city3);

        GD.Print("Created cities: Memphis (Capital), Thebes, Alexandria");
    }

    private void SetupInitialUnits()
    {
        var player1 = Players[0];
        var player2 = Players[1];

        var nakhtu1 = new Nakhtu();
        var medjay1 = new Medjay();
        var archer1 = new Archer();

        var nakhtu2 = new Nakhtu();
        var charioteer2 = new Charioteer();

        player1.AddUnit(nakhtu1);
        player1.AddUnit(medjay1);
        player1.AddUnit(archer1);

        player2.AddUnit(nakhtu2);
        player2.AddUnit(charioteer2);

        // Find suitable starting positions dynamically
        var suitablePositions = FindSuitableStartingPositions();
        
        if (suitablePositions.Count >= 5)
        {
            GameMap[suitablePositions[0]].MoveUnitTo(nakhtu1);
            GameMap[suitablePositions[1]].MoveUnitTo(medjay1);
            GameMap[suitablePositions[2]].MoveUnitTo(archer1);
            GameMap[suitablePositions[3]].MoveUnitTo(nakhtu2);
            GameMap[suitablePositions[4]].MoveUnitTo(charioteer2);
            
            GD.Print($"Initial units deployed at positions: {string.Join(", ", suitablePositions.Take(5))}");
        }
        else
        {
            GD.PrintErr($"Not enough suitable positions found for units! Found {suitablePositions.Count}, need 5");
        }
    }

    private List<Vector2I> FindSuitableStartingPositions()
    {
        var suitablePositions = new List<Vector2I>();
        
        // Look for grassland, desert, or hill tiles (avoid water, rivers, mountains)
        var preferredTerrains = new[] { TerrainType.Grassland, TerrainType.Desert, TerrainType.Hill, TerrainType.Shoreline };
        
        foreach (var kvp in GameMap)
        {
            var position = kvp.Key;
            var tile = kvp.Value;
            
            if (preferredTerrains.Contains(tile.TerrainType) && !tile.IsOccupied() && tile.City == null)
            {
                suitablePositions.Add(position);
            }
        }
        
        // Sort by distance from center to spread units out
        var center = new Vector2I(MapConfiguration.MAP_WIDTH / 2, MapConfiguration.MAP_HEIGHT / 2);
        suitablePositions.Sort((a, b) => 
        {
            var distA = (a - center).Length();
            var distB = (b - center).Length();
            return distA.CompareTo(distB);
        });
        
        GD.Print($"Found {suitablePositions.Count} suitable starting positions");
        return suitablePositions;
    }

    public void StartNewTurn()
    {
        foreach (var player in Players)
        {
            player.ResetUnitMovement();
        }

        TurnManager.AdvancePhase();
    }

    public void ProcessEarnPhase()
    {
        foreach (var player in Players)
        {
            player.EarnFromCities();
        }
        GD.Print("Earn phase completed");
    }

    public bool MoveUnit(Unit unit, Vector2I targetPosition)
    {
        if (!GameMap.ContainsKey(targetPosition))
        {
            return false;
        }

        var targetTile = GameMap[targetPosition];
        if (targetTile.MoveUnitTo(unit))
        {
            var currentTile = FindUnitTile(unit);
            if (currentTile != null && currentTile != targetTile)
            {
                currentTile.RemoveUnit();
            }
            return true;
        }

        return false;
    }

    private HexTile FindUnitTile(Unit unit)
    {
        foreach (var tile in GameMap.Values)
        {
            if (tile.OccupyingUnit == unit)
            {
                return tile;
            }
        }
        return null;
    }

    public void PrintGameState()
    {
        GD.Print($"=== Turn {TurnManager.CurrentTurn} - {TurnManager.CurrentPhase} ===");

        foreach (var player in Players)
        {
            GD.Print($"Player: {player.Name}, Gold: {player.Gold}, Units: {player.Units.Count}, Cities: {player.Cities.Count}");
        }

        GD.Print("Map Status:");
        foreach (var tile in GameMap.Values)
        {
            if (tile.IsOccupied() || tile.IsCity())
            {
                GD.Print($"  {tile.GetDescription()}");
            }
        }
    }
}