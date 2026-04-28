using System;
using Tetris.Core.Configs;
using Tetris.Core.Scoring;
using Tetris.Core.Tetrominoes;
using UnityEngine;

namespace Tetris.Core.Events
{
    public sealed class GameEvents
    {
        public event Action? GameStarted;
        public event Action<ScoreState>? GameOver;
        public event Action? GamePaused;
        public event Action? GameResumed;

        public void InvokeGamesStarted() => GameStarted?.Invoke();
        public void InvokeGameOver(ScoreState finalScore) => GameOver?.Invoke(finalScore);
        public void InvokeGamePaused() => GamePaused?.Invoke();
        public void InvokeGameResumed() => GameResumed?.Invoke();

        public event Action<Piece>? PieceSpawned;
        public event Action<Piece, Vector3Int>? PieceMoved;
        public event Action<Piece>? PieceRotated;
        public event Action<Piece>? PieceFell;
        public event Action<Piece, int> PieceHardDropped;
        public event Action<Piece, TetrominoType>? PieceLocked;
        public event Action<Piece>? GhostPieceChanged;

        public void InvokePieceSwaped(Piece piece) => PieceSpawned?.Invoke(piece);
        public void InvokePieceMoved(Piece piece, Vector3Int direction) => PieceMoved?.Invoke(piece, direction);
        public void InvokePieceRotated(Piece piece) => PieceRotated?.Invoke(piece);
        public void InvokePieceFell(Piece piece) => PieceFell?.Invoke(piece);
        public void InvokePiceHardDropped(Piece piece, int cellDropped) => PieceHardDropped?.Invoke(piece, cellDropped);
        public void InvokePieceLocked(Piece piece, TetrominoType type) => PieceLocked?.Invoke(piece, type);
        public void InvokedGhostPieceChanged(Piece piece) => GhostPieceChanged.Invoke(piece);

        public event Action<int[], TetrominoType>? LayerClearing;
        public event Action<ScoreState, int>? LayerCleared;

        public void InvokedLayerClearing(int[] clearingYs, TetrominoType type) => LayerClearing?.Invoke(clearingYs, type);
        public void InvokedLayersCleared(ScoreState score, int clearingYs) => LayerCleared?.Invoke(score, clearingYs);

        public event Action<ScoreState>? ScoreChnaged;
        public event Action<int> LevelChanged;

        public void InvokeScoreChanged(ScoreState score) => ScoreChnaged?.Invoke(score);
        public void InvokeLevelChanged(int newLevel) => LevelChanged?.Invoke(newLevel);

        public event Action<TetrominoShapeSo>? NextPieceChanged;

        public void InvokedNextPieceChanged(TetrominoShapeSo nextShape) => NextPieceChanged?.Invoke(nextShape);
    }
}
