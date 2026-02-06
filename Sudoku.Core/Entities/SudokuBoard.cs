using Sudoku.Core.Interfaces;
using Sudoku.Core.Extensions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sudoku.Core.Entities
{
    public class SudokuBoard : ISudokuBoard
    {
        private readonly int[,] _puzzle;
        private readonly BitArray[,] _cellConstraints;
        private readonly BitArray[] _rowConstraints;
        private readonly BitArray[] _columnConstraints;
        private readonly BitArray[] _blockConstraints;

        private List<(int Row, int Col)> _nonAssignedCells;

        public int BlockSize { get; private set; }
        public int EdgeSize { get; private set; }

        public SudokuBoard(int blockSize, int[,] initialGrid)
        {
            BlockSize = blockSize;
            EdgeSize = BlockSize * BlockSize;

            _puzzle = new int[EdgeSize, EdgeSize];
            _cellConstraints = new BitArray[EdgeSize, EdgeSize];
            _rowConstraints = new BitArray[EdgeSize];
            _columnConstraints = new BitArray[EdgeSize];
            _blockConstraints = new BitArray[EdgeSize];
            _nonAssignedCells = [];

            InitializeConstraints();
            LoadInitialData(initialGrid);
        }

        public ISudokuBoard Clone()
        {
            var clone = new SudokuBoard(this.BlockSize, this._puzzle)
            {
                _nonAssignedCells = [.. this._nonAssignedCells]
            };

            for (int i = 0; i < EdgeSize; i++)
            {
                clone._rowConstraints[i] = (BitArray)this._rowConstraints[i].Clone();
                clone._columnConstraints[i] = (BitArray)this._columnConstraints[i].Clone();
                clone._blockConstraints[i] = (BitArray)this._blockConstraints[i].Clone();
                for (int j = 0; j < EdgeSize; j++)
                    clone._cellConstraints[i, j] = (BitArray)this._cellConstraints[i, j].Clone();
            }
            return clone;
        }

        public int GetCellValue(int row, int col) => _puzzle[row - 1, col - 1];
        public bool IsSet(int row, int col) => _puzzle[row - 1, col - 1] != 0;

        public void SetCellValue(int row, int col, int val)
        {
            int r = row - 1; int c = col - 1; int v = val - 1; // from 1 based to 0 based indexing
            _puzzle[r, c] = val;
            _cellConstraints[r, c].SetAll(false);
            _cellConstraints[r, c].Set(val - 1, true);

            _rowConstraints[r].Set(v, true);
            _columnConstraints[c].Set(v, true);
            _blockConstraints[GetBlockIndex(r, c)].Set(v, true);

            _nonAssignedCells.RemoveAll(x => x.Row == row && x.Col == col);
        }

        public bool IsLegalAssignment(int row, int col, int val)
        {
            return !_rowConstraints[row - 1][val - 1] &&
                   !_columnConstraints[col - 1][val - 1] &&
                   !_blockConstraints[GetBlockIndex(row - 1, col - 1)][val - 1];
        }

        public List<int> GetCellCandidates(int row, int col)
        {
            var list = new List<int>();
            for (int k = 0; k < EdgeSize; k++)
                if (_cellConstraints[row-1, col-1][k]) list.Add(k + 1);
            return list;
        }

        public void RemoveCellCandidate(int row, int col, int val)
        {
            _cellConstraints[row - 1, col - 1].Set(val - 1, false);
        }

        public bool IsSolved()
        {
            for (int i = 0; i < EdgeSize; i++)
            {
                if (!_rowConstraints[i].AreAllSet() ||
                !_columnConstraints[i].AreAllSet() ||
                !_blockConstraints[i].AreAllSet())
                    return false;
            }
            return true;
        }

        public List<(int Row, int Col)> GetNonAssignedCells()
        {
            return [.. _nonAssignedCells];
        }

        public List<(int Row, int Col, int Count)>? GetNonAssignedCellsWithCount()
        {
            var list = new List<(int, int, int)>();
            foreach (var p in _nonAssignedCells)
            {
                int count = _cellConstraints[p.Row - 1, p.Col - 1].GetCount();
                if (count == 0) return null;
                list.Add((p.Row, p.Col, count));
            }
            return list;
        }

        private void InitializeConstraints()
        {
            for (int i = 0; i < EdgeSize; i++)
            {
                _rowConstraints[i] = new BitArray(EdgeSize, false);
                _columnConstraints[i] = new BitArray(EdgeSize, false);
                _blockConstraints[i] = new BitArray(EdgeSize, false);
                for (int j = 0; j < EdgeSize; j++)
                {
                    _cellConstraints[i, j] = new BitArray(EdgeSize, false);
                }
            }
        }

        private void LoadInitialData(int[,] grid)
        {
            for (int row = 0; row < EdgeSize; row++)
            {
                for (int col = 0; col < EdgeSize; col++)
                {
                    int val = grid[row, col];
                    _puzzle[row, col] = val;

                    if (val == 0)
                    {
                        for (int k = 0; k < EdgeSize; k++) _cellConstraints[row, col].Set(k, true);
                        _nonAssignedCells.Add((row + 1, col + 1));
                    }
                    else
                    {
                        _cellConstraints[row, col].Set(val - 1, true);
                        _rowConstraints[row].Set(val - 1, true);
                        _columnConstraints[col].Set(val - 1, true);
                        _blockConstraints[GetBlockIndex(row, col)].Set(val - 1, true);
                    }
                }
            }
        }

        private int GetBlockIndex(int row, int col)
        {
            return (row / BlockSize) * BlockSize + (col / BlockSize);
        }

    }
}
    

