using Microsoft.AspNetCore.Mvc;
using TicTacToe.Api.Models;
using TicTacToe.Api.Services;

namespace TicTacToe.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GamesController : ControllerBase
{
    private readonly IGameService _gameService;

    public GamesController(IGameService gameService)
    {
        _gameService = gameService;
    }

    [HttpPost]
    public ActionResult<GameStateResponse> CreateGame([FromBody] CreateGameRequest? request)
    {
        var mode = GameMode.TwoPlayer;
        if (request != null && Enum.TryParse<GameMode>(request.Mode, true, out var parsedMode))
        {
            mode = parsedMode;
        }

        var session = _gameService.CreateGame(mode);
        var scoreboard = _gameService.GetScoreboard();

        return CreatedAtAction(nameof(GetGame), new { id = session.Id }, MapToResponse(session, scoreboard));
    }

    [HttpGet("{id:guid}")]
    public ActionResult<GameStateResponse> GetGame(Guid id)
    {
        var session = _gameService.GetGame(id);
        if (session == null)
        {
            return NotFound(new { error = $"Game session '{id}' not found." });
        }

        var scoreboard = _gameService.GetScoreboard();
        return Ok(MapToResponse(session, scoreboard));
    }

    [HttpPost("{id:guid}/moves")]
    public ActionResult<GameStateResponse> SubmitMove(Guid id, [FromBody] MakeMoveRequest request)
    {
        if (request == null)
        {
            return BadRequest(new { error = "Move request payload is required." });
        }

        var (success, error, session) = _gameService.MakeMove(id, request.Player, request.Row, request.Column);
        if (!success)
        {
            if (session == null)
            {
                return NotFound(new { error });
            }
            return BadRequest(new { error });
        }

        var scoreboard = _gameService.GetScoreboard();
        return Ok(MapToResponse(session!, scoreboard));
    }

    [HttpPost("{id:guid}/undo")]
    public ActionResult<GameStateResponse> UndoMove(Guid id)
    {
        var (success, error, session) = _gameService.UndoMove(id);
        if (!success)
        {
            if (session == null)
            {
                return NotFound(new { error });
            }
            return BadRequest(new { error });
        }

        var scoreboard = _gameService.GetScoreboard();
        return Ok(MapToResponse(session!, scoreboard));
    }

    [HttpPost("{id:guid}/reset")]
    public ActionResult<GameStateResponse> ResetGame(Guid id)
    {
        var (success, error, session) = _gameService.ResetGame(id);
        if (!success)
        {
            return NotFound(new { error });
        }

        var scoreboard = _gameService.GetScoreboard();
        return Ok(MapToResponse(session!, scoreboard));
    }

    private static GameStateResponse MapToResponse(GameSession session, Scoreboard scoreboard)
    {
        return new GameStateResponse
        {
            GameId = session.Id,
            Board = session.Board,
            CurrentPlayer = session.CurrentTurn,
            GameMode = session.Mode.ToString(),
            GameStatus = session.Status.ToString(),
            Winner = session.Winner,
            WinningCells = session.WinningCells,
            MoveHistory = session.MoveHistory,
            Scoreboard = scoreboard
        };
    }
}
