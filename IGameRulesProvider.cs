using System.Collections.Generic;
using XCheckers.GameLogic;

namespace XCheckers.ServicesLogic
{
    public interface IGameRulesProvider : IService
    {
        void ApplyMoveToBoard(Move move, Cell[,] board);
        int CalcBoardScore(Cell[,] board, PlayerColor color, int turn);
        bool CanBeEaten(Cell cell, Cell[,] board, int turn);
        (int, DrawType) CheckDraw(List<GameInfo> states);
        Cell[,] CopyBoard(Cell[,] board);
        List<Cell> GetCellsThatCanEat(PlayerColor color, Cell[,] board);
        (bool, List<Move>) GetMoves(PlayerColor player, Cell[,] board);
        List<Move> GetPossibleMoves(Cell cell, Cell[,] board);
        bool HasEatingMoves(Cell cell, Cell[,] board, CellState finalCellState, LockDir lockDir, Cell[] Cells);
        bool HasPossibleMoves(Cell cell, Cell[,] board);
        Cell[,] InitBoard(int xSize, int ySize);

        #region Cells State Checking Logic
        bool IsBlack(Cell cell);
        bool IsEmpty(Cell cell);
        bool IsKing(Cell cell);
        bool IsKing(CellState state);
        bool IsWhite(Cell cell);
        bool IsWhite(CellState state);
        #endregion
    }
}