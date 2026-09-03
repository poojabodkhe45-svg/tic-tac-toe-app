namespace TicTacToe.Api.Models;

public enum GameMode
{
    TwoPlayer,
    AgainstComputer
}

public enum GameStatus
{
    InProgress,
    Won,
    Draw
}

public class CellPosition
{
    public int Row { get; set; }
    public int Column { get; set; }

    public CellPosition() { }

    public CellPosition(int row, int column)
    {
        Row = row;
        Column = column;
    }
}

public class WinningCell
{
    public int Row { get; set; }
    public int Column { get; set; }

    public WinningCell() { }

    public WinningCell(int row, int column)
    {
        Row = row;
        Column = column;
    }
}

public class MoveRecord
{
    public int MoveNumber { get; set; }
    public string Player { get; set; } = string.Empty;
    public CellPosition Position { get; set; } = new();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class Scoreboard
{
    public int XWins { get; set; }
    public int OWins { get; set; }
    public int Draws { get; set; }
}

public class GameSession
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public GameMode Mode { get; set; } = GameMode.TwoPlayer;
    public GameStatus Status { get; set; } = GameStatus.InProgress;
    public string CurrentTurn { get; set; } = "X";
    public string? Winner { get; set; }
    public List<WinningCell>? WinningCells { get; set; }
    public string[][] Board { get; set; } = CreateEmptyBoard();
    public List<MoveRecord> MoveHistory { get; set; } = new();
    public bool ScoreboardUpdated { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public static string[][] CreateEmptyBoard()
    {
        return new string[][]
        {
            new string[] { "", "", "" },
            new string[] { "", "", "" },
            new string[] { "", "", "" }
        };
    }
}

public class CreateGameRequest
{
    public string Mode { get; set; } = "TwoPlayer";
}

public class MakeMoveRequest
{
    public string Player { get; set; } = string.Empty;
    public int Row { get; set; }
    public int Column { get; set; }
}

public class GameStateResponse
{
    public Guid GameId { get; set; }
    public string[][] Board { get; set; } = Array.Empty<string[]>();
    public string CurrentPlayer { get; set; } = "X";
    public string GameMode { get; set; } = string.Empty;
    public string GameStatus { get; set; } = string.Empty;
    public string? Winner { get; set; }
    public List<WinningCell>? WinningCells { get; set; }
    public List<MoveRecord> MoveHistory { get; set; } = new();
    public Scoreboard Scoreboard { get; set; } = new();
}
