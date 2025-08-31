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
    [Signal]
    public delegate void PhaseChangedEventHandler(int oldPhase, int newPhase);
    
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
        var oldPhase = _currentPhase;
        
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
        
        // Emit signal for phase change
        EmitSignal(SignalName.PhaseChanged, (int)oldPhase, (int)_currentPhase);
    }

    public void SetPhase(GamePhase phase)
    {
        var oldPhase = _currentPhase;
        _currentPhase = phase;
        GD.Print($"Phase set to: {_currentPhase}");
        
        // Emit signal for phase change
        EmitSignal(SignalName.PhaseChanged, (int)oldPhase, (int)_currentPhase);
    }

    public string GetCurrentPhaseName()
    {
        return _currentPhase.ToString();
    }
}
