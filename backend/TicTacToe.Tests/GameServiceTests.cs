using TicTacToe.Api.Models;
using TicTacToe.Api.Services;
using Xunit;

namespace TicTacToe.Tests;

public class GameServiceTests
{
    private readonly GameService _service;

    public GameServiceTests()
    {
        _service = new GameService();
    }

    [Fact]
    public void CreateGame_ShouldInitializeDefaultTwoPlayerGame()
    {
        var session = _service.CreateGame(GameMode.TwoPlayer);

        Assert.NotNull(session);
        Assert.NotEqual(Guid.Empty, session.Id);
        Assert.Equal(GameMode.TwoPlayer, session.Mode);
        Assert.Equal(GameStatus.InProgress, session.Status);
        Assert.Equal("X", session.CurrentTurn);
        Assert.Empty(session.MoveHistory);
        Assert.All(session.Board, row => Assert.All(row, cell => Assert.Equal("", cell)));
    }

    [Fact]
    public void MakeMove_ValidMove_ShouldUpdateBoardAndSwitchTurn()
    {
        var session = _service.CreateGame(GameMode.TwoPlayer);

        var (success, error, updated) = _service.MakeMove(session.Id, "X", 0, 0);

        Assert.True(success);
        Assert.Null(error);
        Assert.NotNull(updated);
        Assert.Equal("X", updated!.Board[0][0]);
        Assert.Equal("O", updated.CurrentTurn);
        Assert.Single(updated.MoveHistory);
        Assert.Equal(1, updated.MoveHistory[0].MoveNumber);
        Assert.Equal("X", updated.MoveHistory[0].Player);
        Assert.Equal(0, updated.MoveHistory[0].Position.Row);
        Assert.Equal(0, updated.MoveHistory[0].Position.Column);
    }

    [Fact]
    public void MakeMove_InvalidMove_OccupiedCell_ShouldFailAndNotSwitchTurn()
    {
        var session = _service.CreateGame(GameMode.TwoPlayer);
        _service.MakeMove(session.Id, "X", 0, 0);

        var (success, error, updated) = _service.MakeMove(session.Id, "O", 0, 0);

        Assert.False(success);
        Assert.Contains("occupied", error, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("O", updated!.CurrentTurn);
    }

    [Fact]
    public void MakeMove_InvalidMove_OutOfBounds_ShouldFail()
    {
        var session = _service.CreateGame(GameMode.TwoPlayer);

        var (success, error, _) = _service.MakeMove(session.Id, "X", 3, 0);

        Assert.False(success);
        Assert.Contains("outside", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void MakeMove_InvalidMove_WrongPlayer_ShouldFail()
    {
        var session = _service.CreateGame(GameMode.TwoPlayer);

        var (success, error, _) = _service.MakeMove(session.Id, "O", 0, 0);

        Assert.False(success);
        Assert.Contains("currently Player X's turn", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RowWin_ShouldDetectWinnerHighlightCellsAndUpdateScoreboard()
    {
        var session = _service.CreateGame(GameMode.TwoPlayer);

        // X (0,0), O (1,0), X (0,1), O (1,1), X (0,2) -> X wins row 0
        _service.MakeMove(session.Id, "X", 0, 0);
        _service.MakeMove(session.Id, "O", 1, 0);
        _service.MakeMove(session.Id, "X", 0, 1);
        _service.MakeMove(session.Id, "O", 1, 1);
        var (_, _, updated) = _service.MakeMove(session.Id, "X", 0, 2);

        Assert.Equal(GameStatus.Won, updated!.Status);
        Assert.Equal("X", updated.Winner);
        Assert.NotNull(updated.WinningCells);
        Assert.Equal(3, updated.WinningCells.Count);
        Assert.Contains(updated.WinningCells, c => c.Row == 0 && c.Column == 0);
        Assert.Contains(updated.WinningCells, c => c.Row == 0 && c.Column == 1);
        Assert.Contains(updated.WinningCells, c => c.Row == 0 && c.Column == 2);

        var scoreboard = _service.GetScoreboard();
        Assert.Equal(1, scoreboard.XWins);
        Assert.Equal(0, scoreboard.OWins);
        Assert.Equal(0, scoreboard.Draws);
    }

    [Fact]
    public void ColumnWin_ShouldDetectWinner()
    {
        var session = _service.CreateGame(GameMode.TwoPlayer);

        // X (0,0), O (0,1), X (1,0), O (1,1), X (2,0) -> X wins col 0
        _service.MakeMove(session.Id, "X", 0, 0);
        _service.MakeMove(session.Id, "O", 0, 1);
        _service.MakeMove(session.Id, "X", 1, 0);
        _service.MakeMove(session.Id, "O", 1, 1);
        var (_, _, updated) = _service.MakeMove(session.Id, "X", 2, 0);

        Assert.Equal(GameStatus.Won, updated!.Status);
        Assert.Equal("X", updated.Winner);
        Assert.Equal(3, updated.WinningCells!.Count);
    }

    [Fact]
    public void DiagonalWin_ShouldDetectWinner()
    {
        var session = _service.CreateGame(GameMode.TwoPlayer);

        // X (0,0), O (0,1), X (1,1), O (0,2), X (2,2) -> X wins main diagonal
        _service.MakeMove(session.Id, "X", 0, 0);
        _service.MakeMove(session.Id, "O", 0, 1);
        _service.MakeMove(session.Id, "X", 1, 1);
        _service.MakeMove(session.Id, "O", 0, 2);
        var (_, _, updated) = _service.MakeMove(session.Id, "X", 2, 2);

        Assert.Equal(GameStatus.Won, updated!.Status);
        Assert.Equal("X", updated.Winner);
    }

    [Fact]
    public void DrawDetection_ShouldMarkDrawWhenBoardFull()
    {
        var session = _service.CreateGame(GameMode.TwoPlayer);

        // X O X
        // X O O
        // O X X
        _service.MakeMove(session.Id, "X", 0, 0); // X
        _service.MakeMove(session.Id, "O", 0, 1); // O
        _service.MakeMove(session.Id, "X", 0, 2); // X
        _service.MakeMove(session.Id, "O", 1, 1); // O
        _service.MakeMove(session.Id, "X", 1, 0); // X
        _service.MakeMove(session.Id, "O", 1, 2); // O
        _service.MakeMove(session.Id, "X", 2, 1); // X
        _service.MakeMove(session.Id, "O", 2, 0); // O
        var (_, _, updated) = _service.MakeMove(session.Id, "X", 2, 2); // X

        Assert.Equal(GameStatus.Draw, updated!.Status);
        Assert.Null(updated.Winner);
        Assert.Null(updated.WinningCells);

        var scoreboard = _service.GetScoreboard();
        Assert.Equal(1, scoreboard.Draws);
    }

    [Fact]
    public void MoveAfterGameCompletion_ShouldBeRejected()
    {
        var session = _service.CreateGame(GameMode.TwoPlayer);

        // Quick X win
        _service.MakeMove(session.Id, "X", 0, 0);
        _service.MakeMove(session.Id, "O", 1, 0);
        _service.MakeMove(session.Id, "X", 0, 1);
        _service.MakeMove(session.Id, "O", 1, 1);
        _service.MakeMove(session.Id, "X", 0, 2); // X wins

        var (success, error, _) = _service.MakeMove(session.Id, "O", 2, 2);

        Assert.False(success);
        Assert.Contains("completed game", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ResetGame_ShouldClearBoardAndHistory_KeepScoreboard()
    {
        var session = _service.CreateGame(GameMode.TwoPlayer);
        _service.MakeMove(session.Id, "X", 0, 0);
        _service.MakeMove(session.Id, "O", 1, 1);

        var (success, _, reset) = _service.ResetGame(session.Id);

        Assert.True(success);
        Assert.Empty(reset!.MoveHistory);
        Assert.Equal("X", reset.CurrentTurn);
        Assert.Equal(GameStatus.InProgress, reset.Status);
        Assert.All(reset.Board, row => Assert.All(row, cell => Assert.Equal("", cell)));
    }

    [Fact]
    public void UndoInTwoPlayerMode_ShouldRemoveSingleLatestMove()
    {
        var session = _service.CreateGame(GameMode.TwoPlayer);
        _service.MakeMove(session.Id, "X", 0, 0);
        _service.MakeMove(session.Id, "O", 1, 1);

        var (success, _, undone) = _service.UndoMove(session.Id);

        Assert.True(success);
        Assert.Single(undone!.MoveHistory);
        Assert.Equal("", undone.Board[1][1]);
        Assert.Equal("X", undone.Board[0][0]);
        Assert.Equal("O", undone.CurrentTurn);
    }

    [Fact]
    public void UndoInComputerMode_ShouldRemoveBothComputerAndHumanMoves()
    {
        var session = _service.CreateGame(GameMode.AgainstComputer);

        // Human X moves at (0,0) -> Computer O automatically responds
        var (_, _, afterHuman) = _service.MakeMove(session.Id, "X", 0, 0);
        Assert.Equal(2, afterHuman!.MoveHistory.Count);

        var (success, _, undone) = _service.UndoMove(session.Id);

        Assert.True(success);
        Assert.Empty(undone!.MoveHistory);
        Assert.Equal("", undone.Board[0][0]);
        Assert.Equal("X", undone.CurrentTurn);
    }

    [Fact]
    public void ComputerAI_Priority1_ShouldTakeWinningMove()
    {
        // Board state where O can win at (0,2): O at (0,0), (0,1)
        var board = GameSession.CreateEmptyBoard();
        board[0][0] = "O";
        board[0][1] = "O";
        board[1][0] = "X";
        board[1][1] = "X";

        var move = _service.CalculateComputerMove(board);

        Assert.Equal(0, move.Row);
        Assert.Equal(2, move.Column);
    }

    [Fact]
    public void ComputerAI_Priority2_ShouldBlockOpponentWinningMove()
    {
        // Board state where X can win at (2,0): X at (0,0), (1,0)
        var board = GameSession.CreateEmptyBoard();
        board[0][0] = "X";
        board[1][0] = "X";
        board[0][1] = "O";

        var move = _service.CalculateComputerMove(board);

        Assert.Equal(2, move.Row);
        Assert.Equal(0, move.Column);
    }

    [Fact]
    public void ComputerAI_Priority3_ShouldTakeCenterIfAvailable()
    {
        // Board state where center (1,1) is open
        var board = GameSession.CreateEmptyBoard();
        board[0][0] = "X";

        var move = _service.CalculateComputerMove(board);

        Assert.Equal(1, move.Row);
        Assert.Equal(1, move.Column);
    }

    [Fact]
    public void ComputerAI_Priority4_ShouldTakeCornerIfCenterOccupied()
    {
        // Board state where center is occupied by X
        var board = GameSession.CreateEmptyBoard();
        board[1][1] = "X";

        var move = _service.CalculateComputerMove(board);

        // First available corner is (0,0)
        Assert.Equal(0, move.Row);
        Assert.Equal(0, move.Column);
    }

    [Fact]
    public void ScoreboardReset_ShouldClearAllStats()
    {
        var session = _service.CreateGame(GameMode.TwoPlayer);
        // Win a game for X
        _service.MakeMove(session.Id, "X", 0, 0);
        _service.MakeMove(session.Id, "O", 1, 0);
        _service.MakeMove(session.Id, "X", 0, 1);
        _service.MakeMove(session.Id, "O", 1, 1);
        _service.MakeMove(session.Id, "X", 0, 2);

        var scoreboardBefore = _service.GetScoreboard();
        Assert.Equal(1, scoreboardBefore.XWins);

        var scoreboardAfter = _service.ResetScoreboard();
        Assert.Equal(0, scoreboardAfter.XWins);
        Assert.Equal(0, scoreboardAfter.OWins);
        Assert.Equal(0, scoreboardAfter.Draws);
    }
}
