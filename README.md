# 🎮 Tic Tac Toe - Full-Stack Application

A modern, browser-based Tic Tac Toe application featuring an **Angular frontend** and a **.NET Web API backend** running locally with REST API communication, move history tracking, mode-aware undo capabilities, session scoreboard, and an intelligent computer opponent mode.

---

## 🚀 Quick Start Guide

### Prerequisites
- **.NET SDK** (Version 8.0, 9.0, or 10.0+)
- **Node.js** (v18.x, v20.x, or v22.x+)
- **npm** (v9.x or v10.x+)

---

### 1. Running the Backend (.NET Web API)

```bash
# Navigate to the backend directory
cd backend

# Build and run the API server
dotnet run --project TicTacToe.Api
```

The REST API server will start listening locally at:
- **HTTP**: `http://localhost:5000`

> 💡 *The backend uses CORS policy allowing requests from `http://localhost:4200`.*

---

### 2. Running the Frontend (Angular SPA)

```bash
# Navigate to the frontend application directory
cd frontend/tic-tac-toe-app

# Install dependencies (if not already installed)
npm install

# Start the Angular development server
npm start
```

Open your browser and navigate to:
- 🌐 **`http://localhost:4200`**

---

### 3. Running Unit Tests

#### Backend Unit Tests (.NET xUnit)
```bash
# Run all backend unit tests covering core game rules, AI priorities, undo, and scoreboard
cd backend
dotnet test
```
*Expected Result: All 19 tests pass.*

#### Frontend Unit Tests (Angular / Vitest)
```bash
# Run frontend component tests
cd frontend/tic-tac-toe-app
npm test
```
*Expected Result: All component tests pass.*

---

## 🛠️ Technology Stack

| Layer | Technology |
| :--- | :--- |
| **Frontend** | Angular 21, TypeScript, RxJS, HTML5, CSS Grid / Glassmorphism |
| **Backend** | .NET 10 Web API, C# |
| **API Style** | RESTful JSON API |
| **State / Storage** | Thread-Safe In-Memory Store (`ConcurrentDictionary<Guid, GameSession>`) |
| **Testing** | xUnit (Backend), Vitest / Jasmine (Frontend) |

---

## ✨ Features Implemented

1. **3 × 3 Interactive Game Board**
   - Clickable empty cells.
   - Distinctive styling for Player X (Cyan) and Player O (Rose/Pink).
   - Cell locking for occupied cells and finished games.
   - Glowing accent animation for winning cell combinations.

2. **Player Turns & Game Modes**
   - **Two Player Mode**: Alternating human turns (`X` and `O`).
   - **Play Against Computer Mode**: Human plays as `X`, Computer plays automatically as `O`.
   - Real-time turn indicator banner.

3. **5-Tier Computer AI Opponent**
   - The AI follows the strict priority hierarchy:
     1. **Winning Move**: Plays winning cell if `O` can complete 3-in-a-row/col/diag.
     2. **Block Opponent**: Blocks `X` if `X` can win on the next turn.
     3. **Center Cell**: Occupies center cell `(1,1)` if available.
     4. **Corner Cell**: Occupies a corner cell `(0,0)`, `(0,2)`, `(2,0)`, or `(2,2)` if available.
     5. **Any Cell**: Occupies any remaining empty cell.

4. **Win & Draw Detection**
   - Automatic detection for complete rows, columns, or diagonals.
   - Winner celebration card with highlighted winning line.
   - Draw notification when 9 moves are completed without a winner.

5. **Move History**
   - Structured live table recording Move #, Player symbol, and Position (e.g. `Row 1, Column 1`).
   - Updates after every valid move.

6. **Mode-Aware Undo Move**
   - **Two Player Mode**: Reverts the single latest move, returning turn to that player.
   - **Computer Mode**: Reverts both the computer's move and the human player's previous move together, returning turn to `X`.
   - Disabled when no moves exist or when game is finished (Option A policy).

7. **Session Scoreboard**
   - Server-managed tracking of `X Wins`, `O Wins`, and `Draws`.
   - Guaranteed single score increment per completed match.
   - Independent **Reset Scoreboard** option.
   - **Reset Game** clears board and history while preserving the scoreboard.

---

## 📡 REST API Documentation

Base URL: `http://localhost:5000/api`

| Method | Endpoint | Description | Sample Payload / Params |
| :--- | :--- | :--- | :--- |
| `POST` | `/api/games` | Create a new game session | `{ "mode": "TwoPlayer" \| "AgainstComputer" }` |
| `GET` | `/api/games/{id}` | Get current game state | Path parameter `id` (GUID) |
| `POST` | `/api/games/{id}/moves` | Submit a player move | `{ "player": "X", "row": 0, "column": 0 }` |
| `POST` | `/api/games/{id}/undo` | Undo last move | Path parameter `id` (GUID) |
| `POST` | `/api/games/{id}/reset` | Reset current game session | Path parameter `id` (GUID) |
| `GET` | `/api/scoreboard` | Get live scoreboard | None |
| `POST` | `/api/scoreboard/reset` | Reset scoreboard counters | None |

### Sample Game State Response JSON
```json
{
  "gameId": "3fa85f64-5717-4562-b3fc-2c963f66afe6",
  "board": [
    ["X", "O", ""],
    ["", "X", ""],
    ["", "", ""]
  ],
  "currentPlayer": "O",
  "gameMode": "TwoPlayer",
  "gameStatus": "InProgress",
  "winner": null,
  "winningCells": null,
  "moveHistory": [
    {
      "moveNumber": 1,
      "player": "X",
      "position": { "row": 0, "column": 0 },
      "timestamp": "2026-09-02T20:15:00Z"
    },
    {
      "moveNumber": 2,
      "player": "O",
      "position": { "row": 0, "column": 1 },
      "timestamp": "2026-09-02T20:15:02Z"
    }
  ],
  "scoreboard": {
    "xWins": 1,
    "oWins": 0,
    "draws": 0
  }
}
```

---

## 🧠 AI-Assisted Development & Engineering Notes

### 1. Specification Conversion Workflow
- **Input Requirement**: Functional & technical constraints provided in prompt statement.
- **System Architecture**: Converted functional requirements into a modular C# ASP.NET Web API backend (domain services + REST controllers) and Angular standalone SPA frontend.
- **Specification Document**: Created an execution plan outlining domain models, AI decision trees, REST schemas, and test matrices prior to implementation.

### 2. Prompts Used
- *Architecture Planning Prompt*: Formulated solution layout, project structure, and xUnit test coverage plan.
- *Domain Rule Generation Prompt*: Generated game evaluation logic (row/col/diag line checkers, computer priority search tree).
- *UI Blueprint Prompt*: Designed a responsive 3x3 CSS Grid interface with glassmorphic cards and glowing state indicators.

### 3. Manual Refinements & Edge Case Handling
- **Computer Mode Atomic Turn**: Refined `GameService.MakeMove` so that human player move and computer response move are processed in a single thread-safe operation, eliminating UI lag or race conditions.
- **Double Score Prevention**: Introduced `ScoreboardUpdated` boolean flag inside `GameSession` guarded by atomic lock to ensure game completion increments scoreboard exactly once.
- **Clarification 2 Policy**: Selected **Option A (Disable Undo after game completion)** to ensure clean scoreboard integrity and prevent post-match board state corruption.
- **UI Coordinate Display**: Standardized backend 0-indexed coordinates `(0, 0)` into user-friendly 1-indexed display `Row 1, Column 1` in the move history table.

---

## 🎯 Design Decisions & Clarifications

1. **State Ownership**: The backend .NET service is the sole authority for game state, move validation, win evaluation, and scoreboard counting. The Angular frontend remains thin, presenting the API response and emitting user action triggers.
2. **Clarification 2 Choice**: **Option A** was selected. Once a game reaches `Won` or `Draw` status, the Undo operation is disabled.
3. **Computer Opponent Turn Handling**: Executed server-side immediately after a human move, returning the updated state with both moves.

---

## 🔮 Known Limitations & Future Improvements

- **In-Memory Storage**: Current sessions are stored in memory (`ConcurrentDictionary`). Can be extended to EF Core + SQLite for persistence across server restarts.
- **Multi-Room Multiplayer**: WebSockets / SignalR could be added for online real-time multiplayer between remote players.
- **Difficulty Levels**: Add optional AI difficulty levels (Random, Medium, Unbeatable Minimax).
