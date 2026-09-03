import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, catchError, throwError, timeout, tap } from 'rxjs';
import { GameStateResponse, Scoreboard, GameMode } from '../models/game.models';

@Injectable({
  providedIn: 'root'
})
export class GameApiService {
  private http = inject(HttpClient);

  // Common .NET Web API local ports (5050 primary)
  private candidatePorts = ['5050', '62415', '5245', '5000', '7215', '5001'];
  private activeBaseUrl: string | null = null;

  getActiveBaseUrl(): string {
    return this.activeBaseUrl || `http://localhost:${this.candidatePorts[0]}/api`;
  }

  setBaseUrl(url: string): void {
    let cleaned = url.trim().replace(/\/+$/, '');
    if (!cleaned.endsWith('/api')) {
      cleaned = `${cleaned}/api`;
    }
    this.activeBaseUrl = cleaned;
  }

  private requestWithFallback<T>(makeReq: (url: string) => Observable<T>): Observable<T> {
    if (this.activeBaseUrl) {
      return makeReq(this.activeBaseUrl).pipe(
        catchError(() => {
          this.activeBaseUrl = null;
          return this.tryNextPort(makeReq, 0);
        })
      );
    }
    return this.tryNextPort(makeReq, 0);
  }

  private tryNextPort<T>(makeReq: (url: string) => Observable<T>, index: number): Observable<T> {
    if (index >= this.candidatePorts.length) {
      return throwError(() => new Error('Could not connect to .NET Web API. Please verify backend is running on port 5050.'));
    }

    const targetUrl = `http://localhost:${this.candidatePorts[index]}/api`;
    return makeReq(targetUrl).pipe(
      timeout(3000),
      tap(() => {
        this.activeBaseUrl = targetUrl;
      }),
      catchError(() => {
        return this.tryNextPort(makeReq, index + 1);
      })
    );
  }

  createGame(mode: GameMode = 'TwoPlayer'): Observable<GameStateResponse> {
    return this.requestWithFallback(url => this.http.post<GameStateResponse>(`${url}/games`, { mode }));
  }

  getGame(id: string): Observable<GameStateResponse> {
    return this.requestWithFallback(url => this.http.get<GameStateResponse>(`${url}/games/${id}`));
  }

  submitMove(id: string, player: string, row: number, column: number): Observable<GameStateResponse> {
    return this.requestWithFallback(url => this.http.post<GameStateResponse>(`${url}/games/${id}/moves`, {
      player,
      row,
      column
    }));
  }

  undoMove(id: string): Observable<GameStateResponse> {
    return this.requestWithFallback(url => this.http.post<GameStateResponse>(`${url}/games/${id}/undo`, {}));
  }

  resetGame(id: string): Observable<GameStateResponse> {
    return this.requestWithFallback(url => this.http.post<GameStateResponse>(`${url}/games/${id}/reset`, {}));
  }

  getScoreboard(): Observable<Scoreboard> {
    return this.requestWithFallback(url => this.http.get<Scoreboard>(`${url}/scoreboard`));
  }

  resetScoreboard(): Observable<Scoreboard> {
    return this.requestWithFallback(url => this.http.post<Scoreboard>(`${url}/scoreboard/reset`, {}));
  }
}
