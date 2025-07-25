using Godot;

public enum GamePhase
{
    Earn,
    Purchase,
    Move,
    Combat
}

public partial class TurnManager : Node
{
    private GamePhase _currentPhase = GamePhase.Earn;
    private int _currentTurn = 1;

    public GamePhase CurrentPhase => _currentPhase;
    public int CurrentTurn => _currentTurn;

    public override void _Ready()
    {
        GD.Print($"Turn {_currentTurn} - {_currentPhase} phase started");
    }

    public void AdvancePhase()
    {
        switch (_currentPhase)
        {
            case GamePhase.Earn:
                _currentPhase = GamePhase.Purchase;
                break;
            case GamePhase.Purchase:
                _currentPhase = GamePhase.Move;
                break;
            case GamePhase.Move:
                _currentPhase = GamePhase.Combat;
                break;
            case GamePhase.Combat:
                _currentPhase = GamePhase.Earn;
                _currentTurn++;
                break;
        }

        GD.Print($"Turn {_currentTurn} - {_currentPhase} phase started");
    }

    public void SetPhase(GamePhase phase)
    {
        _currentPhase = phase;
        GD.Print($"Phase set to: {_currentPhase}");
    }

    public string GetCurrentPhaseName()
    {
        return _currentPhase.ToString();
    }
}
