using Godot;

public partial class GameExample : Node
{
    public override void _Ready()
    {
        DemonstrateGameSystem();
    }

    private void DemonstrateGameSystem()
    {
        GD.Print("=== Archistrateia Game System Demonstration ===\n");

        var turnManager = new TurnManager();
        AddChild(turnManager);

        var player1 = new Player("Pharaoh", 200);
        var player2 = new Player("General", 150);

        var city1 = new City("Memphis", 15, true);
        var city2 = new City("Thebes", 12);

        city1.SetOwner(player1);
        city2.SetOwner(player2);

        var nakhtu = new Nakhtu();
        var medjay = new Medjay();
        var archer = new Archer();
        var charioteer = new Charioteer();

        player1.AddUnit(nakhtu);
        player1.AddUnit(medjay);
        player2.AddUnit(archer);
        player2.AddUnit(charioteer);

        var desertTile = new HexTile(new Vector2I(0, 0), TerrainType.Desert);
        var hillTile = new HexTile(new Vector2I(1, 0), TerrainType.Hill);
        var riverTile = new HexTile(new Vector2I(0, 1), TerrainType.River);

        desertTile.SetCity(city1);
        hillTile.SetCity(city2);

        desertTile.MoveUnitTo(nakhtu);
        hillTile.MoveUnitTo(medjay);
        riverTile.MoveUnitTo(archer);

        GD.Print("Initial Setup:");
        GD.Print($"Player 1 ({player1.Name}): {player1.Gold} gold, {player1.Units.Count} units");
        GD.Print($"Player 2 ({player2.Name}): {player2.Gold} gold, {player2.Units.Count} units");
        GD.Print($"Turn: {turnManager.CurrentTurn}, Phase: {turnManager.CurrentPhase}\n");

        GD.Print("Earn Phase:");
        player1.EarnFromCities();
        player2.EarnFromCities();

        GD.Print("Purchase Phase:");
        var newNakhtu = new Nakhtu();
        if (player1.PurchaseUnit(newNakhtu))
        {
            GD.Print($"{player1.Name} purchased a new {newNakhtu.Name}");
        }

        GD.Print("Move Phase:");
        if (riverTile.MoveUnitTo(archer))
        {
            GD.Print($"{archer.Name} moved to river tile");
        }

        GD.Print("Combat Phase:");
        GD.Print($"{nakhtu.Name} combat power: {nakhtu.GetCombatPower()}");
        GD.Print($"{medjay.Name} combat power: {medjay.GetCombatPower()}");

        GD.Print("\nAdvancing to next turn...");
        turnManager.AdvancePhase();

        GD.Print($"New Turn: {turnManager.CurrentTurn}, Phase: {turnManager.CurrentPhase}");

        GD.Print("\nMap Status:");
        GD.Print($"Desert Tile: {desertTile.GetDescription()}");
        GD.Print($"Hill Tile: {hillTile.GetDescription()}");
        GD.Print($"River Tile: {riverTile.GetDescription()}");

        GD.Print("\n=== Demonstration Complete ===");
    }
}