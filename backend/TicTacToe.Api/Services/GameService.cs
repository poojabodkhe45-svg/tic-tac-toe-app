using System.Collections.Concurrent;
using TicTacToe.Api.Models;

namespace TicTacToe.Api.Services;

public class GameService : IGameService
{
    private readonly ConcurrentDictionary<Guid, GameSession> _sessions = new();
    private readonly Scoreboard _scoreboard = new();
    private readonly object _scoreLock = new();

    public GameSession CreateGame(GameMode mode)
    {
        var session = new GameSession
        {
            Id = Guid.NewGuid(),
            Mode = mode,
            Status = GameStatus.InProgress,
            CurrentTurn = "X",
            Board = GameSession.CreateEmptyBoard(),
            MoveHistory = new List<MoveRecord>(),
            CreatedAt = DateTime.UtcNow
        };

        _sessions[session.Id] = session;
        return session;
    }

    public GameSession? GetGame(Guid id)
    {
        _sessions.TryGetValue(id, out var session);
        return session;
    }

    public (bool Success, string? Error, GameSession? Session) MakeMove(Guid id, string player, int row, int col)
    {
        if (!_sessions.TryGetValue(id, out var session))
        {
            return (false, "Game session not found.", null);
        }

        lock (session)
        {
            // Validation
            var validationError = ValidateMove(session, player, row, col);
            if (validationError != null)
            {
                return (false, validationError, session);
            }

            // Execute human player move
            ApplyMoveToSession(session, player, row, col);

            // Check if game ended after human move
            EvaluateGameStatus(session);

            // If in Computer Mode and game is still in progress, trigger computer move
            if (session.Mode == GameMode.AgainstComputer && session.Status == GameStatus.InProgress && session.CurrentTurn == "O")
            {
                var compMove = CalculateComputerMove(session.Board);
                ApplyMoveToSession(session, "O", compMove.Row, compMove.Column);
                EvaluateGameStatus(session);
            }

            return (true, null, session);
        }
    }

    public (bool Success, string? Error, GameSession? Session) UndoMove(Guid id)
    {
        if (!_sessions.TryGetValue(id, out var session))
        {
            return (false, "Game session not found.", null);
        }

        lock (session)
        {
            if (session.Status != GameStatus.InProgress)
            {
                return (false, "Cannot undo moves on a completed game.", session);
            }

            if (session.MoveHistory.Count == 0)
            {
                return (false, "No moves to undo.", session);
            }

            if (session.Mode == GameMode.TwoPlayer)
            {
                // Remove 1 move
                var lastMove = session.MoveHistory[^1];
                session.MoveHistory.RemoveAt(session.MoveHistory.Count - 1);
                session.Board[lastMove.Position.Row][lastMove.Position.Column] = "";
                session.CurrentTurn = lastMove.Player;
            }
            else // AgainstComputer
            {
                // Remove last 2 moves if available (Computer O + Human X), otherwise 1 if only 1 move exists
                int countToRemove = Math.Min(2, session.MoveHistory.Count);
                for (int i = 0; i < countToRemove; i++)
                {
                    var moveToRemove = session.MoveHistory[^1];
                    session.MoveHistory.RemoveAt(session.MoveHistory.Count - 1);
                    session.Board[moveToRemove.Position.Row][moveToRemove.Position.Column] = "";
                }
                session.CurrentTurn = "X";
            }

            session.Status = GameStatus.InProgress;
            session.Winner = null;
            session.WinningCells = null;

            return (true, null, session);
        }
    }

    public (bool Success, string? Error, GameSession? Session) ResetGame(Guid id)
    {
        if (!_sessions.TryGetValue(id, out var session))
        {
            return (false, "Game session not found.", null);
        }

        lock (session)
        {
            session.Board = GameSession.CreateEmptyBoard();
            session.MoveHistory.Clear();
            session.Status = GameStatus.InProgress;
            session.CurrentTurn = "X";
            session.Winner = null;
            session.WinningCells = null;
            session.ScoreboardUpdated = false;

            return (true, null, session);
        }
    }

    public Scoreboard GetScoreboard()
    {
        lock (_scoreLock)
        {
            return new Scoreboard
            {
                XWins = _scoreboard.XWins,
                OWins = _scoreboard.OWins,
                Draws = _scoreboard.Draws
            };
        }
    }

    public Scoreboard ResetScoreboard()
    {
        lock (_scoreLock)
        {
            _scoreboard.XWins = 0;
            _scoreboard.OWins = 0;
            _scoreboard.Draws = 0;

            return GetScoreboard();
        }
    }

    public CellPosition CalculateComputerMove(string[][] board)
    {
        // 1. Win if O can win
        var winMove = FindWinningMove(board, "O");
        if (winMove != null) return winMove;

        // 2. Block X if X can win next
        var blockMove = FindWinningMove(board, "X");
        if (blockMove != null) return blockMove;

        // 3. Take center if available (1,1)
        if (string.IsNullOrEmpty(board[1][1]))
        {
            return new CellPosition(1, 1);
        }

        // 4. Take a corner if available: (0,0), (0,2), (2,0), (2,2)
        var corners = new (int r, int c)[] { (0, 0), (0, 2), (2, 0), (2, 2) };
        foreach (var (r, c) in corners)
        {
            if (string.IsNullOrEmpty(board[r][c]))
            {
                return new CellPosition(r, c);
            }
        }

        // 5. Take any available cell
        for (int r = 0; r < 3; r++)
        {
            for (int c = 0; c < 3; c++)
            {
                if (string.IsNullOrEmpty(board[r][c]))
                {
                    return new CellPosition(r, c);
                }
            }
        }

        throw new InvalidOperationException("No available cells for computer move.");
    }

    private static string? ValidateMove(GameSession session, string player, int row, int col)
    {
        if (session.Status != GameStatus.InProgress)
        {
            return "Cannot make a move in a completed game.";
        }

        if (row < 0 || row > 2 || col < 0 || col > 2)
        {
            return "Move is outside the board boundaries (must be 0..2).";
        }

        if (!string.IsNullOrEmpty(session.Board[row][col]))
        {
            return $"Cell position ({row}, {col}) is already occupied.";
        }

        if (!string.Equals(player, session.CurrentTurn, StringComparison.OrdinalIgnoreCase))
        {
            return $"It is currently Player {session.CurrentTurn}'s turn, not Player {player}.";
        }

        return null;
    }

    private static void ApplyMoveToSession(GameSession session, string player, int row, int col)
    {
        session.Board[row][col] = player.ToUpper();
        session.MoveHistory.Add(new MoveRecord
        {
            MoveNumber = session.MoveHistory.Count + 1,
            Player = player.ToUpper(),
            Position = new CellPosition(row, col),
            Timestamp = DateTime.UtcNow
        });

        session.CurrentTurn = player.ToUpper() == "X" ? "O" : "X";
    }

    private void EvaluateGameStatus(GameSession session)
    {
        var winResult = CheckWin(session.Board);
        if (winResult.IsWin)
        {
            session.Status = GameStatus.Won;
            session.Winner = winResult.Winner;
            session.WinningCells = winResult.WinningCells;
            UpdateScoreboardOnce(session);
            return;
        }

        if (session.MoveHistory.Count == 9 || IsBoardFull(session.Board))
        {
            session.Status = GameStatus.Draw;
            session.Winner = null;
            session.WinningCells = null;
            UpdateScoreboardOnce(session);
        }
    }

    private void UpdateScoreboardOnce(GameSession session)
    {
        if (session.ScoreboardUpdated) return;

        lock (_scoreLock)
        {
            if (session.ScoreboardUpdated) return;

            if (session.Status == GameStatus.Won)
            {
                if (session.Winner == "X") _scoreboard.XWins++;
                else if (session.Winner == "O") _scoreboard.OWins++;
            }
            else if (session.Status == GameStatus.Draw)
            {
                _scoreboard.Draws++;
            }

            session.ScoreboardUpdated = true;
        }
    }

    private static (bool IsWin, string? Winner, List<WinningCell>? WinningCells) CheckWin(string[][] board)
    {
        var lines = new (int r, int c)[][]
        {
            // Rows
            new[] { (0,0), (0,1), (0,2) },
            new[] { (1,0), (1,1), (1,2) },
            new[] { (2,0), (2,1), (2,2) },
            // Columns
            new[] { (0,0), (1,0), (2,0) },
            new[] { (0,1), (1,1), (2,1) },
            new[] { (0,2), (1,2), (2,2) },
            // Diagonals
            new[] { (0,0), (1,1), (2,2) },
            new[] { (0,2), (1,1), (2,0) }
        };

        foreach (var line in lines)
        {
            string first = board[line[0].r][line[0].c];
            if (!string.IsNullOrEmpty(first) &&
                first == board[line[1].r][line[1].c] &&
                first == board[line[2].r][line[2].c])
            {
                var winningCells = line.Select(p => new WinningCell(p.r, p.c)).ToList();
                return (true, first, winningCells);
            }
        }

        return (false, null, null);
    }

    private static bool IsBoardFull(string[][] board)
    {
        for (int r = 0; r < 3; r++)
        {
            for (int c = 0; c < 3; c++)
            {
                if (string.IsNullOrEmpty(board[r][c])) return false;
            }
        }
        return true;
    }

    private CellPosition? FindWinningMove(string[][] board, string symbol)
    {
        for (int r = 0; r < 3; r++)
        {
            for (int c = 0; c < 3; c++)
            {
                if (string.IsNullOrEmpty(board[r][c]))
                {
                    // Try move
                    board[r][c] = symbol;
                    var win = CheckWin(board);
                    board[r][c] = ""; // Revert

                    if (win.IsWin && win.Winner == symbol)
                    {
                        return new CellPosition(r, c);
                    }
                }
            }
        }
        return null;
    }
}
