using Godot;

public class MoveResult
{
    public bool Success { get; set; }
    public Vector2I NewPosition { get; set; }
    public string ErrorMessage { get; set; }

    public static MoveResult CreateSuccess(Vector2I newPosition)
    {
        return new MoveResult 
        { 
            Success = true, 
            NewPosition = newPosition 
        };
    }

    public static MoveResult CreateFailure(string errorMessage)
    {
        return new MoveResult 
        { 
            Success = false, 
            ErrorMessage = errorMessage 
        };
    }
}

public class TileClickResult
{
    public bool IsMovementAttempt { get; set; }
    public Vector2I DestinationPosition { get; set; }
    public string ErrorMessage { get; set; }

    public static TileClickResult CreateMovementAttempt(Vector2I destination)
    {
        return new TileClickResult 
        { 
            IsMovementAttempt = true, 
            DestinationPosition = destination 
        };
    }

    public static TileClickResult CreateError(string errorMessage)
    {
        return new TileClickResult 
        { 
            IsMovementAttempt = false, 
            ErrorMessage = errorMessage 
        };
    }
} 