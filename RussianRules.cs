using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using XCheckers.GameLogic;
using XCheckers.InternalLogic;

namespace XCheckers.ServicesLogic
{
    public sealed class RussianRules : IGameRulesProvider
    {
        #region Moves Logic

        public bool HasPossibleMoves(Cell cell, Cell[,] board)
        {
            var _tempCells = new List<Cell>();
            var _tempMoves = new List<Move>();
            var cellState = cell.state;
            cell.state = CellState.Empty;

            if (IsKing(cellState))
            {
                CalcKingEatingMoves(cell, cellState, _tempMoves, _tempCells, board, LockDir.None);

                if (_tempMoves.Count == 0)
                {
                    cell.state = cellState;
                    CalcKingSimpleMoves(cell, cellState, _tempMoves, _tempCells, board);
                }
            }
            else
            {
                CalcCheckerEatingMoves(cell, cellState, _tempMoves, _tempCells, board, LockDir.None);

                if (_tempMoves.Count == 0)
                {
                    cell.state = cellState;
                    if (IsWhite(cell))
                    {
                        CalcWhiteSimpleMoves(cell, _tempMoves, board);
                    }
                    else
                    {
                        CalcBlackSimpleMoves(cell, _tempMoves, board);
                    }
                }
            }

            cell.state = cellState;

            return _tempMoves.Count > 0;
        }

        public List<Move> GetPossibleMoves(Cell cell, Cell[,] board)
        {
            var _tempMoves = new List<Move>();
            var _tempCells = new List<Cell>();
            var movesToRemove = new List<Move>();
            var cellState = cell.state;
            cell.state = CellState.Empty;

            if (IsKing(cellState))
            {
                CalcKingEatingMoves(cell, cellState, _tempMoves, _tempCells, board, LockDir.None);

                if (_tempMoves.Count == 0)
                {
                    cell.state = cellState;
                    CalcKingSimpleMoves(cell, cell.state, _tempMoves, _tempCells, board);
                }
                else
                {
                    cell.state = cellState;
                    foreach (var m in _tempMoves)
                    {
                        var lockDir = LockDir.None;

                        var lastCell = new Vector2(m.cells[m.cells.Length - 1].xIndex, m.cells[m.cells.Length - 1].yIndex);
                        var prevCell = new Vector2(m.cells[m.cells.Length - 2].xIndex, m.cells[m.cells.Length - 2].yIndex);

                        if (prevCell.x == lastCell.x - 1 && prevCell.y == lastCell.y - 1)
                        {
                            lockDir = LockDir.DownLeft;
                        }
                        if (prevCell.x == lastCell.x + 1 && prevCell.y == lastCell.y + 1)
                        {
                            lockDir = LockDir.UpRight;
                        }
                        if (prevCell.x == lastCell.x - 1 && prevCell.y == lastCell.y + 1)
                        {
                            lockDir = LockDir.UpLeft;
                        }
                        if (prevCell.x == lastCell.x + 1 && prevCell.y == lastCell.y - 1)
                        {
                            lockDir = LockDir.DownRight;
                        }

                        var newBoard = CopyBoard(board);
                        ApplyMoveToBoard(m, newBoard);

                        if (HasEatingMoves(newBoard[m.cells[m.cells.Length - 1].xIndex, m.cells[m.cells.Length - 1].yIndex], newBoard,  m.finalCellState))
                        {
                            movesToRemove.Add(m);
                        }
                    }
                }
            }
            else
            {
                CalcCheckerEatingMoves(cell, cellState, _tempMoves, _tempCells, board, LockDir.None);

                if (_tempMoves.Count == 0)
                {
                    cell.state = cellState;
                    if (IsWhite(cell))
                    {
                        CalcWhiteSimpleMoves(cell, _tempMoves, board);
                    }
                    else
                    {
                        CalcBlackSimpleMoves(cell, _tempMoves, board);
                    }
                }

            }

            cell.state = cellState;
            Debug.Log(_tempMoves.Except(movesToRemove).Count());
            return _tempMoves.Except(movesToRemove).ToList();
        }

        public bool HasEatingMoves(Cell cell, Cell[,] board, CellState finalCellState, LockDir lockDir = LockDir.None, Cell[] prevCells = null)
        {
            var _tempCells = new List<Cell>();
            if (prevCells != null)
            {
                _tempCells.AddRange(prevCells);
            }
            var _tempMoves = new List<Move>();

            if (IsKing(finalCellState))
            {
                CalcKingEatingMoves(cell, finalCellState, _tempMoves, _tempCells, board, lockDir);
            }
            else
            {
                CalcCheckerEatingMoves(cell, finalCellState, _tempMoves, _tempCells, board, lockDir);
            }

            if (_tempMoves.Count > 0)
            {
                return true;
            }

            return false;
        }

        public (bool, List<Move>) GetMoves(PlayerColor player, Cell[,] board)
        {
            var supposedMoves = new List<Move>();
            var supposedEatingMoves = new List<Move>();

            if (player == PlayerColor.White)
            {
                foreach (var cell in board)
                {
                    if (IsWhite(cell))
                        supposedMoves.AddRange(GetPossibleMoves(cell, board));
                }
            }
            else
            {
                foreach (var cell in board)
                {
                    if (IsBlack(cell))
                        supposedMoves.AddRange(GetPossibleMoves(cell, board));
                }
            }

            if (supposedMoves.Count > 0)
            {
                foreach (var move in supposedMoves)
                {
                    foreach (var cell in move.cells)
                    {
                        if (player == PlayerColor.Black)
                        {
                            if (board[cell.xIndex, cell.yIndex].state == CellState.WhiteChecker || board[cell.xIndex, cell.yIndex].state == CellState.WhiteKing)
                            {
                                supposedEatingMoves.Add(move);
                                break;
                            }
                        }

                        if (player == PlayerColor.White)
                        {
                            if (board[cell.xIndex, cell.yIndex].state == CellState.BlackChecker || board[cell.xIndex, cell.yIndex].state == CellState.BlackKing)
                            {
                                supposedEatingMoves.Add(move);
                                break;
                            }
                        }
                    }
                }
            }

            if (supposedEatingMoves.Count > 0)
            {
                return (true, supposedEatingMoves);
            }
            else
            {
                return (false, supposedMoves);
            }
        }

        public void ApplyMoveToBoard(Move move, Cell[,] board)
        {
            for (int i = 0; i < move.cells.Length; i++)
            {
                if (i == move.cells.Length - 1)
                {
                    board[move.cells[i].xIndex, move.cells[i].yIndex].state = move.finalCellState;
                }
                else
                {
                    board[move.cells[i].xIndex, move.cells[i].yIndex].state = CellState.Empty;
                }
            }
        }

        private bool CalcKingEatingMoves(Cell cell, CellState finalCellState, List<Move> possibleMoves, List<Cell> previousCells, Cell[,] board, LockDir lockDir)
        {
            var hasMoreMoves = false;

            var checkedCells = new List<Cell>();
            var eatingMoveCells = new List<Cell>();

            if (lockDir != LockDir.UpRight)
            {
                var targetCell = RightUpCell(cell, board);
                if (RightUpCell(cell, board) != null)
                {
                    while (true)
                    {
                        if (targetCell != null)
                        {
                            if (!IsEmpty(targetCell) && previousCells.Contains(targetCell))
                            {
                                checkedCells.Clear();
                                break;
                            }
                        }

                        if (targetCell == null)
                            break;

                        if (!IsEmpty(targetCell) && SameColor(targetCell, finalCellState))
                        {
                            break;
                        }

                        var nextToTargetCell = RightUpCell(targetCell, board);

                        if (nextToTargetCell != null)
                        {
                            if (!IsEmpty(targetCell) && !IsEmpty(nextToTargetCell))
                            {
                                break;
                            }
                        }

                        checkedCells.Add(targetCell);
                        targetCell = nextToTargetCell;
                    }

                    hasMoreMoves = FindKingEatingMovesInCheckedCells(cell, finalCellState, possibleMoves, checkedCells, eatingMoveCells, previousCells, board, LockDir.DownLeft);
                }
            }


            checkedCells.Clear();
            eatingMoveCells.Clear();

            if (lockDir != LockDir.UpLeft)
            {
                var targetCell = LeftUpCell(cell, board);
                if (LeftUpCell(cell, board) != null)
                {
                    while (true)
                    {
                        if (targetCell != null)
                        {
                            if (!IsEmpty(targetCell) && previousCells.Contains(targetCell))
                            {
                                checkedCells.Clear();
                                break;
                            }
                        }

                        if (targetCell == null)
                            break;

                        if (!IsEmpty(targetCell) && SameColor(targetCell, finalCellState))
                        {
                            break;
                        }

                        var nextToTargetCell = LeftUpCell(targetCell, board);

                        if (nextToTargetCell != null)
                        {
                            if (!IsEmpty(targetCell) && !IsEmpty(nextToTargetCell))
                            {
                                break;
                            }
                        }

                        checkedCells.Add(targetCell);
                        targetCell = nextToTargetCell;
                    }

                    hasMoreMoves = FindKingEatingMovesInCheckedCells(cell, finalCellState, possibleMoves, checkedCells, eatingMoveCells, previousCells, board, LockDir.DownRight);
                }
            }

            checkedCells.Clear();
            eatingMoveCells.Clear();

            if (lockDir != LockDir.DownRight)
            {
                var targetCell = RightDownCell(cell, board);
                if (RightDownCell(cell, board) != null)
                {
                    while (true)
                    {
                        if (targetCell != null)
                        {
                            if (!IsEmpty(targetCell) && previousCells.Contains(targetCell))
                            {
                                checkedCells.Clear();
                                break;
                            }
                        }

                        if (targetCell == null)
                            break;

                        if (!IsEmpty(targetCell) && SameColor(targetCell, finalCellState))
                        {
                            break;
                        }

                        var nextToTargetCell = RightDownCell(targetCell, board);

                        if (nextToTargetCell != null)
                        {
                            if (!IsEmpty(targetCell) && !IsEmpty(nextToTargetCell))
                            {
                                break;
                            }
                        }

                        checkedCells.Add(targetCell);
                        targetCell = nextToTargetCell;
                    }

                    hasMoreMoves = FindKingEatingMovesInCheckedCells(cell, finalCellState, possibleMoves, checkedCells, eatingMoveCells, previousCells, board, LockDir.UpLeft);
                }
            }

            checkedCells.Clear();
            eatingMoveCells.Clear();

            if (lockDir != LockDir.DownLeft)
            {
                var targetCell = LeftDownCell(cell, board);
                if (LeftDownCell(cell, board) != null)

                {
                    while (true)
                    {
                        if (targetCell != null)
                        {
                            if (!IsEmpty(targetCell) && previousCells.Contains(targetCell))
                            {
                                checkedCells.Clear();
                                break;
                            }
                        }

                        if (targetCell == null)
                            break;

                        if (!IsEmpty(targetCell) && SameColor(targetCell, finalCellState))
                        {
                            break;
                        }

                        var nextToTargetCell = LeftDownCell(targetCell, board);

                        if (nextToTargetCell != null)
                        {
                            if (!IsEmpty(targetCell) && !IsEmpty(nextToTargetCell))
                            {
                                break;
                            }
                        }

                        checkedCells.Add(targetCell);
                        targetCell = nextToTargetCell;
                    }

                    hasMoreMoves = FindKingEatingMovesInCheckedCells(cell, finalCellState, possibleMoves, checkedCells, eatingMoveCells, previousCells, board, LockDir.UpRight);
                }
            }

            checkedCells.Clear();
            eatingMoveCells.Clear();

            if (hasMoreMoves == false && previousCells.Count > 0)
                possibleMoves.Add(new Move(previousCells, finalCellState, true));

            return hasMoreMoves;
        }

        private bool FindKingEatingMovesInCheckedCells(Cell cell, CellState finalCellState, List<Move> possibleMoves, List<Cell> checkedCells, List<Cell> eatingMoveCells, List<Cell> previousCells, Cell[,] board, LockDir lockDir)
        {
            if (checkedCells.Count > 0)
            {
                //if (checkedCells[0] != cell)
                checkedCells.Insert(0, cell);

                for (int i = 0; i < checkedCells.Count; i++)
                {
                    if (!IsEmpty(checkedCells[i]) && !SameColor(checkedCells[i], finalCellState))
                    {
                        /*                        if (previousCells.Contains(checkedCells[i]))
                                                {
                                                    break;
                                                }*/

/*                        foreach (var c in previousCells)
                        {
                            if (c.xIndex == checkedCells[i].xIndex && c.yIndex == checkedCells[i].yIndex)
                            {
                                break;
                            }
                        }*/

                        var index = i;
                        index++;
                        if (index < checkedCells.Count)
                        {
                            if (IsEmpty(checkedCells[index]))
                            {
                                //eatingMoveCells.Clear();
                                for (int y = index; y < checkedCells.Count; y++)
                                {
                                    if (IsEmpty(checkedCells[y]))
                                    {
                                        eatingMoveCells.Add(checkedCells[y]);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            if (eatingMoveCells.Count > 0)
            {
                var hasEatMoves = false;

                for (int i = 0; i < eatingMoveCells.Count; i++)
                {
                    var moveCells = new List<Cell>();

                    for (int y = 0; y < checkedCells.Count; y++)
                    {
                        moveCells.Add(checkedCells[y]);
                        if (checkedCells[y] == eatingMoveCells[i])
                            break;
                    }

                    var nB = CopyBoard(board);
                    foreach (var c in previousCells)
                    {
                        nB[c.xIndex, c.yIndex].state = CellState.Empty;
                    }
                    nB[eatingMoveCells[i].xIndex, eatingMoveCells[i].yIndex].state = finalCellState;

                    if (HasEatingMoves(nB[eatingMoveCells[i].xIndex, eatingMoveCells[i].yIndex], nB, finalCellState, lockDir))
                    {
                        AddMovesAndContinue(moveCells, possibleMoves, previousCells, finalCellState, board, lockDir);
                        hasEatMoves = true;
                    }
                }

                if (hasEatMoves)
                    return hasEatMoves;
            }


            if (eatingMoveCells.Count > 0)
            {
                for (int i = 0; i < eatingMoveCells.Count; i++)
                {
                    var moveCells = new List<Cell>();

                    for (int y = 0; y < checkedCells.Count; y++)
                    {
                        moveCells.Add(checkedCells[y]);
                        if (checkedCells[y] == eatingMoveCells[i])
                            break;
                    }

                    AddMovesAndContinue(moveCells, possibleMoves, previousCells, finalCellState, board, lockDir);
                }

                return true;
            }

            return false;

        }

        private void CalcKingSimpleMoves(Cell cell, CellState finalCellState, List<Move> possibleMoves, List<Cell> previousCells, Cell[,] board)
        {
            var checkedCells = new List<Cell>();

            var targetCell = RightUpCell(cell, board);
            if (RightUpCell(cell, board) != null)
            {
                while (true)
                {
                    if (targetCell != null)
                    {
                        if (!IsEmpty(targetCell) && previousCells.Contains(targetCell))
                        {
                            checkedCells.Clear();
                            break;
                        }
                    }

                    if (targetCell == null)
                        break;

                    if ((!IsEmpty(targetCell) /*&& SameColor(finalCellState, targetCell)*/))
                    {
                        break;
                    }

                    var nextToTargetCell = RightUpCell(targetCell, board);

                    if (nextToTargetCell != null)
                    {
                        if (!IsEmpty(targetCell) && !IsEmpty(nextToTargetCell))
                        {
                            break;
                        }
                    }

                    checkedCells.Add(targetCell);
                    targetCell = nextToTargetCell;
                }
            }
            AddKingSimpleMoves(cell, checkedCells, possibleMoves, finalCellState);

            targetCell = LeftUpCell(cell, board);
            if (LeftUpCell(cell, board) != null)
            {
                while (true)
                {
                    if (targetCell != null)
                    {
                        if (!IsEmpty(targetCell) && previousCells.Contains(targetCell))
                        {
                            checkedCells.Clear();
                            break;
                        }
                    }

                    if (targetCell == null)
                        break;

                    if ((!IsEmpty(targetCell)/* && SameColor(finalCellState, targetCell)*/))
                    {
                        break;
                    }

                    var nextToTargetCell = LeftUpCell(targetCell, board);

                    if (nextToTargetCell != null)
                    {
                        if (!IsEmpty(targetCell) && !IsEmpty(nextToTargetCell))
                        {
                            break;
                        }
                    }

                    checkedCells.Add(targetCell);
                    targetCell = nextToTargetCell;
                }
            }
            AddKingSimpleMoves(cell, checkedCells, possibleMoves, finalCellState);

            targetCell = RightDownCell(cell, board);
            if (RightDownCell(cell, board) != null)
            {
                while (true)
                {
                    if (targetCell != null)
                    {
                        if (!IsEmpty(targetCell) && previousCells.Contains(targetCell))
                        {
                            checkedCells.Clear();
                            break;
                        }
                    }

                    if (targetCell == null)
                        break;

                    if ((!IsEmpty(targetCell) /*&& SameColor(finalCellState, targetCell)*/))
                    {
                        break;
                    }

                    var nextToTargetCell = RightDownCell(targetCell, board);

                    if (nextToTargetCell != null)
                    {
                        if (!IsEmpty(targetCell) && !IsEmpty(nextToTargetCell))
                        {
                            break;
                        }
                    }

                    checkedCells.Add(targetCell);
                    targetCell = nextToTargetCell;
                }
            }
            AddKingSimpleMoves(cell, checkedCells, possibleMoves, finalCellState);

            targetCell = LeftDownCell(cell, board);
            if (LeftDownCell(cell, board) != null)
            {
                while (true)
                {
                    if (targetCell != null)
                    {
                        if (!IsEmpty(targetCell) && previousCells.Contains(targetCell))
                        {
                            checkedCells.Clear();
                            break;
                        }
                    }

                    if (targetCell == null)
                        break;

                    if ((!IsEmpty(targetCell)/* && SameColor(finalCellState, targetCell)*/))
                    {
                        break;
                    }

                    var nextToTargetCell = LeftDownCell(targetCell, board);

                    if (nextToTargetCell != null)
                    {
                        if (!IsEmpty(targetCell) && !IsEmpty(nextToTargetCell))
                        {
                            break;
                        }
                    }

                    checkedCells.Add(targetCell);
                    targetCell = nextToTargetCell;
                }
            }
            AddKingSimpleMoves(cell, checkedCells, possibleMoves, finalCellState);
        }

        private void AddKingSimpleMoves(Cell cell, List<Cell> checkedCells, List<Move> possibleMoves, CellState finalCellState)
        {
            if (checkedCells.Count > 0)
            {
                checkedCells.Insert(0, cell);

                for (int i = 1; i < checkedCells.Count; i++)
                {
                    possibleMoves.Add(new Move(new List<Cell>(checkedCells.GetRange(0, i + 1)), finalCellState, false));
                }

                checkedCells.Clear();
            }
        }

        private void CalcCheckerEatingMoves(Cell cell, CellState finalCellState, List<Move> possibleMoves, List<Cell> previousCells, Cell[,] board, LockDir lockDir)
        {
            var hasMoreMoves = false;

            if (lockDir != LockDir.UpRight)
            {
                if (RightUpCell(cell, board) != null)
                {
                    if (!IsEmpty(RightUpCell(cell, board)) && !SameColor(RightUpCell(cell, board), finalCellState))
                    {
                        var target = RightUpCell(RightUpCell(cell, board), board);
                        if (target != null)
                        {
                            if (IsEmpty(target) /*&& !previousCells.Contains(RightUpCell(cell, board)) && !previousCells.Contains(target)*/)
                            {
                                hasMoreMoves = true;
                                var nb = CopyBoard(board);
                                foreach(var c in previousCells)
                                {
                                    nb[c.xIndex, c.yIndex].state = CellState.Empty;
                                }

                                nb[target.xIndex, target.yIndex].state = finalCellState;

                                var cells = new List<Cell>()
                            {
                                    nb[cell.xIndex, cell.yIndex],
                                    RightUpCell(cell, nb),
                                    nb[target.xIndex, target.yIndex]
                            };

                            AddMovesAndContinue(cells, possibleMoves, previousCells, finalCellState, nb, LockDir.DownLeft);
                            }
                        }
                    }
                }
            }


            if (lockDir != LockDir.UpLeft)
            {
                if (LeftUpCell(cell, board) != null)
                {
                    if (!IsEmpty(LeftUpCell(cell, board)) && !SameColor(LeftUpCell(cell, board), finalCellState))
                    {
                        var target = LeftUpCell(LeftUpCell(cell, board), board);
                        if (target != null)
                        {
                            if (IsEmpty(target) /*&& !previousCells.Contains(LeftUpCell(cell, board)) && !previousCells.Contains(target)*/)
                            {
                                hasMoreMoves = true;
                                var nb = CopyBoard(board);
                                foreach (var c in previousCells)
                                {
                                    nb[c.xIndex, c.yIndex].state = CellState.Empty;
                                }

                                nb[target.xIndex, target.yIndex].state = finalCellState;
                                var cells = new List<Cell>()
                            {
                                    nb[cell.xIndex, cell.yIndex],
                                    LeftUpCell(cell, nb),
                                    nb[target.xIndex, target.yIndex]
                            };
                                AddMovesAndContinue(cells, possibleMoves, previousCells, finalCellState, nb, LockDir.DownRight);
                            }
                        }
                    }
                }
            }

            if (lockDir != LockDir.DownRight)
            {
                if (RightDownCell(cell, board) != null)
                {
                    if (!IsEmpty(RightDownCell(cell, board)) && !SameColor(RightDownCell(cell, board), finalCellState))
                    {
                        var target = RightDownCell(RightDownCell(cell, board), board);
                        if (target != null)
                        {
                            if (IsEmpty(target) /*&& !previousCells.Contains(RightDownCell(cell, board)) && !previousCells.Contains(target)*/)
                            {
                                hasMoreMoves = true;
                                var nb = CopyBoard(board);
                                foreach (var c in previousCells)
                                {
                                    nb[c.xIndex, c.yIndex].state = CellState.Empty;
                                }

                                nb[target.xIndex, target.yIndex].state = finalCellState;
                                var cells = new List<Cell>()
                            {
                                    nb[cell.xIndex, cell.yIndex],
                                    RightDownCell(cell, nb),
                                    nb[target.xIndex, target.yIndex]
                            };
                                AddMovesAndContinue(cells, possibleMoves, previousCells, finalCellState, nb, LockDir.UpLeft);
                            }
                        }
                    }
                }
            }

            if (lockDir != LockDir.DownLeft)
            {
                if (LeftDownCell(cell, board) != null)
                {
                    if (!IsEmpty(LeftDownCell(cell, board)) && !SameColor(LeftDownCell(cell, board), finalCellState))
                    {
                        var target = LeftDownCell(LeftDownCell(cell, board), board);
                        if (target != null)
                        {
                            if (IsEmpty(target)/* && !previousCells.Contains(LeftDownCell(cell, board)) && !previousCells.Contains(target)*/)
                            {
                                hasMoreMoves = true;
                                var nb = CopyBoard(board);
                                foreach (var c in previousCells)
                                {
                                    nb[c.xIndex, c.yIndex].state = CellState.Empty;
                                }

                                nb[target.xIndex, target.yIndex].state = finalCellState;
                                var cells = new List<Cell>()
                            {
                                    nb[cell.xIndex, cell.yIndex],
                                    LeftDownCell(cell, nb),
                                    nb[target.xIndex,target.yIndex]
                            };
                                AddMovesAndContinue(cells, possibleMoves, previousCells, finalCellState, nb, LockDir.UpRight);
                            }
                        }
                    }
                }
            }

            if (hasMoreMoves == false && previousCells.Count > 0)
                possibleMoves.Add(new Move(previousCells, finalCellState, true));
        }

        private void CalcWhiteSimpleMoves(Cell cell, List<Move> possibleMoves, Cell[,] board)
        {
            if (RightUpCell(cell, board) != null)
            {
                if (IsEmpty(RightUpCell(cell, board)))
                {
                    var moveCells = new List<Cell>
                    {
                        cell,
                        RightUpCell(cell, board)
                    };

                    if (RightUpCell(cell, board).yIndex == board.GetLength(1) - 1)
                    {
                        //CalcKingEatingMoves(RightUpCell(cell, board), CellState.WhiteKing, possibleMoves, moveCells, board, LockDir.DownLeft);
                        possibleMoves.Add(new Move(moveCells, CellState.WhiteKing, false));
                    }
                    else
                    {
                        possibleMoves.Add(new Move(moveCells, cell.state, false));
                    }
                }
            }

            if (LeftUpCell(cell, board) != null)
            {
                if (IsEmpty(LeftUpCell(cell, board)))
                {
                    var moveCells = new List<Cell>
                    {
                        cell,
                        LeftUpCell(cell, board)
                    };

                    if (LeftUpCell(cell, board).yIndex == board.GetLength(1) - 1)
                    {
                        //CalcKingEatingMoves(LeftUpCell(cell, board), CellState.WhiteKing, possibleMoves, moveCells, board, LockDir.DownRight);
                        possibleMoves.Add(new Move(moveCells, CellState.WhiteKing, false));
                    }
                    else
                    {
                        possibleMoves.Add(new Move(moveCells, cell.state, false));
                    }
                }
            }
        }

        private void CalcBlackSimpleMoves(Cell cell, List<Move> possibleMoves, Cell[,] board)
        {
            if (RightDownCell(cell, board) != null)
            {
                if (IsEmpty(RightDownCell(cell, board)))
                {
                    //_tempMoveCells.Clear();
                    var moveCells = new List<Cell>
                    {
                        cell,
                        RightDownCell(cell, board)
                    };

                    if (RightDownCell(cell, board).yIndex == 0)
                    {
                        possibleMoves.Add(new Move(moveCells, CellState.BlackKing, false));
                        //CalcKingEatingMoves(RightDownCell(cell, board), CellState.BlackKing, possibleMoves, moveCells, board, LockDir.UpLeft);
                    }
                    else
                    {
                        possibleMoves.Add(new Move(moveCells, cell.state, false));
                    }
                }
            }


            if (LeftDownCell(cell, board) != null)
            {
                if (IsEmpty(LeftDownCell(cell, board)))
                {
                    var moveCells = new List<Cell>
                    {
                        cell,
                        LeftDownCell(cell, board)
                    };

                    if (LeftDownCell(cell, board).yIndex == 0)
                    {
                        possibleMoves.Add(new Move(moveCells, CellState.BlackKing, false));
                        //CalcKingEatingMoves(LeftDownCell(cell, board), CellState.BlackKing, possibleMoves, moveCells, board, LockDir.UpRight);
                    }
                    else
                    {
                        possibleMoves.Add(new Move(moveCells, cell.state, false));
                    }
                }
            }
        }

        private void AddMovesAndContinue(List<Cell> cells, List<Move> possibleMoves, List<Cell> previousCells, CellState firstCellState, Cell[,] board, LockDir lockDir)
        {
            var moveCells = new List<Cell>();
            foreach(var c in previousCells)
            {
                moveCells.Add(board[c.xIndex, c.yIndex]);
            }
            var j = 0;
            if (moveCells.Count > 0)
            {
                if (moveCells[moveCells.Count - 1].xIndex == cells[0].xIndex && moveCells[moveCells.Count-1].yIndex == cells[0].yIndex)
                {
                    j = 1;
                }
            }

            for (int i = j; i < cells.Count; i++)
            {
                //if (!moveCells.Contains(cells[i]))
                moveCells.Add(board[cells[i].xIndex, cells[i].yIndex]);
            }

            var firstCell = moveCells[0];
            var lastCell = moveCells[moveCells.Count - 1];

            if (IsWhite(firstCellState) && lastCell.yIndex == board.GetLength(1) - 1)
            {
                firstCellState = CellState.WhiteKing;
            }

            if (IsBlack(firstCellState) && lastCell.yIndex == 0)
                firstCellState = CellState.BlackKing;

            if (IsKing(firstCellState))
            {
                CalcKingEatingMoves(lastCell, firstCellState, possibleMoves, moveCells, board, lockDir);
            }
            else
            {
                CalcCheckerEatingMoves(lastCell, firstCellState, possibleMoves, moveCells, board, lockDir);
            }
        }

        #endregion

        #region Draw Logic
        public (int, DrawType) CheckDraw(List<GameInfo> states)
        {
            if (states.Count <= 15)
                return (0, DrawType.None);

            List<(int, DrawType)> draws = new()
            {
                CheckRepeating(states),
                CheckCantWin(states),
                Check3UnitsVs1OnBigRoad(states, PlayerColor.White),
                Check3UnitsVs1OnBigRoad(states, PlayerColor.Black),
                Check3KingsOrMoreVs1CantWin(states, PlayerColor.White),
                Check3KingsOrMoreVs1CantWin(states, PlayerColor.Black),
            };

            foreach (var draw in draws)
            {
                if (draw.Item2 != DrawType.None)
                {
                    if (draw.Item1 == 0)
                    {
                        return draw;
                    }
                }
            }

            foreach (var draw in draws)
            {
                if (draw.Item2 != DrawType.None)
                {
                    if (draw.Item1 > 0)
                    {
                        return draw;
                    }
                }
            }

            return (0, DrawType.None);
        }

        private (int, DrawType) CheckRepeating(List<GameInfo> states)
        {
            var lastMoveIndex = states.Count - 1;

            if (AreStatesEqual(states[lastMoveIndex], states[lastMoveIndex - 4]) && AreStatesEqual(states[lastMoveIndex - 1], states[lastMoveIndex - 5]))
            {
                if (AreStatesEqual(states[lastMoveIndex - 2], states[lastMoveIndex - 6]) && AreStatesEqual(states[lastMoveIndex - 3], states[lastMoveIndex - 7]))
                {
                    if (AreStatesEqual(states[lastMoveIndex - 4], states[lastMoveIndex - 8]) && AreStatesEqual(states[lastMoveIndex - 5], states[lastMoveIndex - 9]))
                    {
                        return (0, DrawType.RepeatingMoves);
                    }
                    else
                    {
                        return (1, DrawType.RepeatingMoves);
                    }
                }
                else
                {
                    return (2, DrawType.RepeatingMoves);
                }
            }
            else
            {
                return (0, DrawType.None);
            }
        }

        private bool AreStatesEqual(GameInfo state1, GameInfo state2)
        {
            if (state1.lastMove.finalCellState != state2.lastMove.finalCellState)
            {
                return false;
            }

            if (state1.lastMove.cells.Length != state2.lastMove.cells.Length)
            {
                return false;
            }

            for (int i = 0; i < state1.lastMove.cells.Length; i++)
            {
                if (state1.lastMove.cells[i].state != state2.lastMove.cells[i].state)
                {
                    return false;
                }

                if (state1.lastMove.cells[i].xIndex != state2.lastMove.cells[i].xIndex)
                {
                    return false;
                }

                if (state1.lastMove.cells[i].yIndex != state2.lastMove.cells[i].yIndex)
                {
                    return false;
                }
            }

            return true;
        }
        private (int, DrawType) CheckCantWin(List<GameInfo> states)
        {
            var movesTillDraw = 15;

            for (int i = states.Count - 1; i >= states.Count - 15; i--)
            {
                if (IsKing(states[i].lastMove.finalCellState) && !states[i].lastMove.hasEating)
                {
                    movesTillDraw--;
                }
                else
                {
                    break;
                }
            }

            if (movesTillDraw <= 3)
            {
                return (movesTillDraw, DrawType.CantWin);
            }
            else
            {
                return (0, DrawType.None);
            }
        }

        private (int, DrawType) Check3UnitsVs1OnBigRoad(List<GameInfo> states, PlayerColor color)
        {
            var movesTillDraw = 5;
            var opponentColor = color == PlayerColor.White ? PlayerColor.Black : PlayerColor.White;

            for (int i = states.Count - 1; i >= states.Count - 5; i--)
            {
                if (IsSoloKingOnBigRoad(color, states[i].board) && Has3Units(opponentColor, states[i].board))
                {
                    movesTillDraw--;
                }
                else
                {
                    break;
                }
            }

            if (movesTillDraw <= 3)
            {
                return (movesTillDraw, DrawType.Units3Vs1KingOnBigRoad);
            }
            else
            {
                return (0, DrawType.None);
            }
        }

        private (int, DrawType) Check3KingsOrMoreVs1CantWin(List<GameInfo> states, PlayerColor color)
        {
            var movesTillDraw = 15;

            var opponentColor = color == PlayerColor.White ? PlayerColor.Black : PlayerColor.White;

            for (int i = states.Count - 1; i >= states.Count - 15; i--)
            {
                if (IsSoloKing(color, states[i].board) && Has3OrMoreKings(opponentColor, states[i].board))
                {
                    movesTillDraw--;
                }
                else
                {
                    break;
                }
            }

            if (movesTillDraw <= 3)
            {
                return (movesTillDraw, DrawType.Kings3orMoreVs1King);
            }
            else
            {
                return (0, DrawType.None);
            }
        }


        private bool IsSoloKingOnBigRoad(PlayerColor color, CellStruct[,] board)
        {
            var playerCells = new List<CellStruct>();

            foreach (var cell in board)
            {
                if (color == PlayerColor.White && (cell.state == CellState.WhiteChecker || cell.state == CellState.WhiteKing))
                {
                    playerCells.Add(cell);
                }

                if (color == PlayerColor.Black && (cell.state == CellState.BlackChecker || cell.state == CellState.BlackKing))
                {
                    playerCells.Add(cell);
                }
            }

            if (playerCells.Count > 1)
                return false;

            if (!(playerCells[0].state == CellState.WhiteKing || playerCells[0].state == CellState.BlackKing))
                return false;

            if (!IsBigRoadCell(playerCells[0]))
                return false;

            return true;
        }

        private bool IsBigRoadCell(CellStruct cell)
        {
            if (cell.xIndex == cell.yIndex)
            {
                return true;
            }

            return false;
        }

        private bool Has3Units(PlayerColor color, CellStruct[,] board)
        {
            var playerCells = new List<CellStruct>();

            foreach (var cell in board)
            {
                if (color == PlayerColor.White && (cell.state == CellState.WhiteChecker || cell.state == CellState.WhiteKing))
                {
                    playerCells.Add(cell);
                }

                if (color == PlayerColor.Black && (cell.state == CellState.BlackKing || cell.state == CellState.BlackChecker))
                {
                    playerCells.Add(cell);
                }
            }

            if (playerCells.Count == 3)
            {

                return true;
            }

            return false;
        }
        private bool Has3OrMoreKings(PlayerColor color, CellStruct[,] board)
        {
            var playerCells = new List<CellStruct>();

            foreach (var cell in board)
            {
                if (color == PlayerColor.White && (cell.state == CellState.WhiteChecker || cell.state == CellState.WhiteKing))
                {
                    playerCells.Add(cell);
                }

                if (color == PlayerColor.Black && (cell.state == CellState.BlackChecker || cell.state == CellState.BlackKing))
                {
                    playerCells.Add(cell);
                }
            }

            if (playerCells.Count >= 3)
            {
                var kingsCount = 0;
                foreach (var cell in playerCells)
                {
                    if (cell.state == CellState.WhiteKing || cell.state == CellState.BlackKing)
                        kingsCount++;
                }

                if (kingsCount >= 3)
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsSoloKing(PlayerColor color, CellStruct[,] board)
        {
            var playerCells = new List<CellStruct>();

            foreach (var cell in board)
            {
                if (color == PlayerColor.White && (cell.state == CellState.WhiteChecker || cell.state == CellState.WhiteKing))
                {
                    playerCells.Add(cell);
                }

                if (color == PlayerColor.Black && (cell.state == CellState.BlackChecker || cell.state == CellState.BlackKing))
                {
                    playerCells.Add(cell);
                }
            }

            if (playerCells.Count == 1 && (playerCells[0].state == CellState.WhiteKing || playerCells[0].state == CellState.BlackKing))
                return true;

            return false;
        }

        #endregion

        #region Board Logic
        public Cell[,] InitBoard(int xSize, int ySize)
        {
            var currentBoard = new Cell[xSize, ySize];

            for (int y = 0; y < ySize; y++)
            {
                for (int x = 0; x < xSize; x++)
                {
                    var cell = new Cell();
                    cell.xIndex = x;
                    cell.yIndex = y;

                    currentBoard[x, y] = cell;

                    if ((x + y) % 2 == 0)
                    {
                        if (y <= 2)
                        {
                            cell.state = CellState.WhiteChecker;
                        }
                        else if (y >= 5)
                        {
                            cell.state = CellState.BlackChecker;
                        }
                    }
                }
            }


            //---

            /*            for (int y = 0; y < ySize; y++)
                        {
                            for (int x = 0; x < xSize; x++)
                            {
                                currentBoard[x, y].state = CellState.Empty;
                            }
                        }
                        currentBoard[6, 4].state = CellState.WhiteChecker;


                        currentBoard[5, 3].state = CellState.BlackChecker;
                        currentBoard[5, 5].state = CellState.BlackChecker;
                        currentBoard[3, 5].state = CellState.BlackChecker;
                        currentBoard[3, 3].state = CellState.BlackChecker;*/

            //---
            var pu = Services.Container.GetService<IPuzzlesProvider>();
            pu.InitPuzzles();

            pu.SetPuzzle(PlayerPrefs.GetInt(Constants.PuzzleKey, 0));
            var gamePos = pu.GetPuzzleStartPos(pu.CurrentPuzzleIndex);

            foreach (var pos in gamePos)
            {
                var xIndex = pos.Key[0];
                var x = GetPuzzleIndex(xIndex);
                var yIndex = pos.Key[1];
                int y = yIndex - '1';
                var state = GetPuzzleState(pos.Value);
                currentBoard[x, y].state = state;
            }



            return currentBoard;
        }


        private CellState GetPuzzleState(string value)
        {
            switch (value)
            {
                case "white":
                    return CellState.WhiteChecker;
                case "black":
                    return CellState.BlackChecker;
                case "white-king":
                    return CellState.WhiteKing;
                case "black-king":
                    return CellState.BlackKing;
                default:
                    return CellState.Empty;
            }
        }

        private int GetPuzzleIndex(char xIndex)
        {
            switch (xIndex)
            {
                case 'a':
                    return 0;
                case 'b':
                    return 1;
                case 'c':
                    return 2;
                case 'd':
                    return 3;
                case 'e':
                    return 4;
                case 'f':
                    return 5;
                case 'g':
                    return 6;
                case 'h':
                    return 7;
                default:
                    return 10;
            }
        }


        public Cell[,] CopyBoard(Cell[,] board)
        {
            var xSize = board.GetLength(0);
            var ySize = board.GetLength(1);

            var newBoard = new Cell[xSize, ySize];

            for (int y = 0; y < ySize; y++)
            {
                for (int x = 0; x < xSize; x++)
                {
                    var cell = new Cell
                    {
                        xIndex = x,
                        yIndex = y,

                        state = board[x, y].state
                    };

                    newBoard[x, y] = cell;
                }
            }

            return newBoard;
        }

        public int CalcBoardScore(Cell[,] board, PlayerColor color, int turn)
        {

            var whiteScore = 0;
            var blackScore = 0;
            foreach (var cell in board)
            {
                if (CanBeEaten(cell, board, turn + 1))
                    continue;

                if (IsWhite(cell))
                {
                    if (IsKing(cell))
                    {
                        whiteScore += 300;
                    }
                    else
                    {
                        whiteScore += 150 + cell.yIndex;
                    }
                }

                if (IsBlack(cell))
                {
                    if (IsKing(cell))
                    {
                        blackScore += 300;
                    }
                    else
                    {
                        blackScore += 150 + (7 - cell.yIndex);
                    }
                }
            }

            if (color == PlayerColor.White)
            {
                return whiteScore - blackScore;
            }
            else
            {
                return blackScore - whiteScore;
            }
        }

        public bool CanBeEaten(Cell cell, Cell[,] board, int turn)
        {
            var turnColor = PlayerColor.Black;

            if (turn % 2 == 0)
            {
                turnColor = PlayerColor.White;
            }

            var rightDownCell = RightDownCell(cell, board);
            var rightUpCell = RightUpCell(cell, board);
            var leftUpCell = LeftUpCell(cell, board);
            var leftDownCell = LeftDownCell(cell, board);

            if (rightDownCell != null)
            {
                if (!IsEmpty(rightDownCell))
                {
                    if (!SameColor(cell, rightDownCell))
                    {
                        if (leftUpCell != null)
                        {
                            if (IsEmpty(leftUpCell))
                            {
                                if (SameColor(rightDownCell, turnColor))
                                {
                                    return true;
                                }
                            }
                        }
                    }
                }
            }

            if (leftDownCell != null)
            {
                if (!IsEmpty(leftDownCell))
                {
                    if (!SameColor(cell, leftDownCell))
                    {
                        if (rightUpCell != null)
                        {
                            if (IsEmpty(rightUpCell))
                            {
                                if (SameColor(leftDownCell, turnColor))
                                {
                                    return true;
                                }
                            }
                        }
                    }
                }
            }

            if (leftUpCell != null)
            {
                if (!IsEmpty(leftUpCell))
                {
                    if (!SameColor(cell, leftUpCell))
                    {
                        if (rightDownCell != null)
                        {
                            if (IsEmpty(rightDownCell))
                            {
                                if (SameColor(leftUpCell, turnColor))
                                {
                                    return true;
                                }
                            }
                        }
                    }
                }
            }

            if (rightUpCell != null)
            {
                if (!IsEmpty(rightUpCell))
                {
                    if (!SameColor(cell, rightUpCell))
                    {
                        if (leftDownCell != null)
                        {
                            if (IsEmpty(leftDownCell))
                            {
                                if (SameColor(rightUpCell, turnColor))
                                {
                                    return true;
                                }
                            }
                        }
                    }
                }
            }

            return false;
        }

        public List<Cell> GetCellsThatCanEat(PlayerColor color, Cell[,] board)
        {
            var cells = new List<Cell>();

            foreach (var cell in board)
            {
                if (color == PlayerColor.White && IsWhite(cell))
                {
                    if (HasEatingMoves(cell, board, cell.state))
                    {
                        cells.Add(cell);
                    }
                }

                if (color == PlayerColor.Black && IsBlack(cell))
                {
                    if (HasEatingMoves(cell, board, cell.state))
                    {
                        cells.Add(cell);
                    }
                }
            }

            return cells;
        }

        #endregion

        #region Cells Logic
        public bool IsEmpty(Cell cell)
        {
            return cell.state == CellState.Empty;
        }

        public bool IsWhite(Cell cell)
        {
            return cell.state == CellState.WhiteChecker || cell.state == CellState.WhiteKing;
        }

        public bool IsBlack(Cell cell)
        {
            return cell.state == CellState.BlackChecker || cell.state == CellState.BlackKing;
        }

        public bool IsKing(Cell cell)
        {
            return cell.state == CellState.WhiteKing || cell.state == CellState.BlackKing;
        }

        public bool IsKing(CellState state)
        {
            return state == CellState.WhiteKing || state == CellState.BlackKing;
        }

        public bool IsWhite(CellState state)
        {
            return state == CellState.WhiteKing || state == CellState.WhiteChecker;
        }

        public bool IsBlack(CellState state)
        {
            return state == CellState.BlackKing || state == CellState.BlackChecker;
        }
        #endregion

        #region Internal Logic

        private bool IsBoard(int x, int y, Cell[,] board)
        {
            if (x < board.GetLength(0) && y < board.GetLength(1) && x >= 0 && y >= 0)
                return true;

            return false;
        }

        private Cell RightUpCell(Cell cell, Cell[,] board)
        {
            if (!IsBoard(cell.xIndex + 1, cell.yIndex + 1, board))
                return null;

            return board[cell.xIndex + 1, cell.yIndex + 1];
        }

        private Cell LeftUpCell(Cell cell, Cell[,] board)
        {
            if (!IsBoard(cell.xIndex - 1, cell.yIndex + 1, board))
                return null;

            return board[cell.xIndex - 1, cell.yIndex + 1];
        }

        private Cell RightDownCell(Cell cell, Cell[,] board)
        {
            if (!IsBoard(cell.xIndex + 1, cell.yIndex - 1, board))
                return null;

            return board[cell.xIndex + 1, cell.yIndex - 1];
        }

        private Cell LeftDownCell(Cell cell, Cell[,] board)
        {
            if (!IsBoard(cell.xIndex - 1, cell.yIndex - 1, board))
                return null;

            return board[cell.xIndex - 1, cell.yIndex - 1];
        }

        private bool SameColor(Cell cell0, Cell cell1)
        {
            return (IsBlack(cell0) && IsBlack(cell1)) || (IsWhite(cell0) && IsWhite(cell1));
        }

        private bool SameColor(Cell cell0, CellState cell1)
        {
            return (IsBlack(cell0) && (cell1 == CellState.BlackChecker || cell1 == CellState.BlackKing)) || (IsWhite(cell0) && (cell1 == CellState.WhiteChecker || cell1 == CellState.WhiteKing));
        }

        private bool SameColor(Cell cell0, PlayerColor color)
        {
            return (IsBlack(cell0) && (color == PlayerColor.Black)) || (IsWhite(cell0) && color == PlayerColor.White);
        }
        #endregion
    }
}