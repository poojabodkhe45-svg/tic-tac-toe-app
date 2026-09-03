using TicTacToe.Api.Models;

namespace TicTacToe.Api.Services;

public interface IGameService
{
    GameSession CreateGame(GameMode mode);
    GameSession? GetGame(Guid id);
    (bool Success, string? Error, GameSession? Session) MakeMove(Guid id, string player, int row, int col);
    (bool Success, string? Error, GameSession? Session) UndoMove(Guid id);
    (bool Success, string? Error, GameSession? Session) ResetGame(Guid id);
    Scoreboard GetScoreboard();
    Scoreboard ResetScoreboard();
    CellPosition CalculateComputerMove(string[][] board);
}
