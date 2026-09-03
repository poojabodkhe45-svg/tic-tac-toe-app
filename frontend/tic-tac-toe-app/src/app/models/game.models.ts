export type GameMode = 'TwoPlayer' | 'AgainstComputer';
export type GameStatus = 'InProgress' | 'Won' | 'Draw';

export interface WinningCell {
  row: number;
  column: number;
}

export interface CellPosition {
  row: number;
  column: number;
}

export interface MoveRecord {
  moveNumber: number;
  player: string;
  position: CellPosition;
  timestamp: string;
}

export interface Scoreboard {
  xWins: number;
  oWins: number;
  draws: number;
}

export interface GameStateResponse {
  gameId: string;
  board: string[][];
  currentPlayer: string;
  gameMode: string;
  gameStatus: string;
  winner?: string | null;
  winningCells?: WinningCell[] | null;
  moveHistory: MoveRecord[];
  scoreboard: Scoreboard;
}

export interface CreateGameRequest {
  mode: GameMode;
}

export interface MakeMoveRequest {
  player: string;
  row: number;
  column: number;
}
