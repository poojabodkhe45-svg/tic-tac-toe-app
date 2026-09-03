import { Component, OnInit, signal, computed, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { GameApiService } from './services/game-api.service';
import { GameStateResponse, GameMode } from './models/game.models';

const createDefaultState = (mode: GameMode = 'TwoPlayer'): GameStateResponse => ({
  gameId: '',
  board: [
    ['', '', ''],
    ['', '', ''],
    ['', '', '']
  ],
  currentPlayer: 'X',
  gameMode: mode,
  gameStatus: 'InProgress',
  winner: null,
  winningCells: null,
  moveHistory: [],
  scoreboard: { xWins: 0, oWins: 0, draws: 0 }
});

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App implements OnInit {
  private api = inject(GameApiService);

  gameState = signal<GameStateResponse>(createDefaultState());
  selectedMode = signal<GameMode>('TwoPlayer');
  errorMessage = signal<string | null>(null);
  loading = signal<boolean>(false);
  apiUrl: string = 'http://localhost:62415';
  showSettings = signal<boolean>(false);

  // Computed signals
  isGameFinished = computed(() => {
    const state = this.gameState();
    return state.gameStatus === 'Won' || state.gameStatus === 'Draw';
  });

  canUndo = computed(() => {
    const state = this.gameState();
    if (this.isGameFinished()) return false; // Option A: Disable Undo after game completion
    if (state.moveHistory.length === 0) return false;
    if (state.gameMode === 'AgainstComputer' && state.moveHistory.length < 1) return false;
    return true;
  });

  ngOnInit(): void {
    this.startNewGame(this.selectedMode());
  }

  getBoardRow(idx: number): number {
    return Math.floor(idx / 3);
  }

  getBoardCol(idx: number): number {
    return idx % 3;
  }

  startNewGame(mode: GameMode = this.selectedMode()): void {
    this.loading.set(true);
    this.errorMessage.set(null);
    this.selectedMode.set(mode);

    this.api.createGame(mode).subscribe({
      next: (response) => {
        this.gameState.set(response);
        this.loading.set(false);
      },
      error: (err) => {
        this.gameState.set(createDefaultState(mode));
        this.errorMessage.set(
          'Connecting to backend... If port is different, click ⚙️ Settings and enter your port (e.g. http://localhost:62415).'
        );
        this.loading.set(false);
      }
    });
  }

  applyCustomApiUrl(): void {
    if (this.apiUrl) {
      this.api.setBaseUrl(this.apiUrl);
      this.startNewGame(this.selectedMode());
    }
  }

  switchMode(mode: GameMode): void {
    if (this.selectedMode() === mode && this.gameState().gameId !== '') return;
    this.startNewGame(mode);
  }

  onCellClick(row: number, col: number): void {
    const state = this.gameState();
    if (this.loading() || this.isGameFinished()) return;

    if (state.board[row][col] !== '') return;

    if (!state.gameId) {
      this.startNewGame(this.selectedMode());
      return;
    }

    this.loading.set(true);
    this.errorMessage.set(null);

    const playerToMove = state.currentPlayer;

    this.api.submitMove(state.gameId, playerToMove, row, col).subscribe({
      next: (updatedState) => {
        this.gameState.set(updatedState);
        this.loading.set(false);
      },
      error: (err) => {
        this.errorMessage.set(err.error?.error || err.message || 'Invalid move request.');
        this.loading.set(false);
      }
    });
  }

  onUndo(): void {
    const state = this.gameState();
    if (!state.gameId || !this.canUndo() || this.loading()) return;

    this.loading.set(true);
    this.errorMessage.set(null);

    this.api.undoMove(state.gameId).subscribe({
      next: (updatedState) => {
        this.gameState.set(updatedState);
        this.loading.set(false);
      },
      error: (err) => {
        this.errorMessage.set(err.error?.error || err.message || 'Could not undo move.');
        this.loading.set(false);
      }
    });
  }

  onResetGame(): void {
    const state = this.gameState();
    if (!state.gameId) {
      this.startNewGame(this.selectedMode());
      return;
    }

    this.loading.set(true);
    this.errorMessage.set(null);

    this.api.resetGame(state.gameId).subscribe({
      next: (updatedState) => {
        this.gameState.set(updatedState);
        this.loading.set(false);
      },
      error: (err) => {
        this.errorMessage.set(err.error?.error || err.message || 'Could not reset game.');
        this.loading.set(false);
      }
    });
  }

  onResetScoreboard(): void {
    this.loading.set(true);
    this.errorMessage.set(null);

    this.api.resetScoreboard().subscribe({
      next: (scoreboard) => {
        this.gameState.set({
          ...this.gameState(),
          scoreboard
        });
        this.loading.set(false);
      },
      error: (err) => {
        this.errorMessage.set(err.error?.error || err.message || 'Could not reset scoreboard.');
        this.loading.set(false);
      }
    });
  }

  isWinningCell(row: number, col: number): boolean {
    const winningCells = this.gameState().winningCells;
    if (!winningCells) return false;
    return winningCells.some(cell => cell.row === row && cell.column === col);
  }
}
