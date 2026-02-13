using Sudoku.Core.Extensions;
using Sudoku.Core.Interfaces;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Sudoku.Core.Entities
{
    public class SudokuBoard : ISudokuBoard
    {
        private readonly int[] _puzzle;

        private readonly int[] _rowConstraints;
        private readonly int[] _columnConstraints;
        private readonly int[] _blockConstraints;

        private readonly int[] _cellConstraints;

        private readonly int[] _cellToBlockMap;

        private readonly int _allOnesMask;

        public int BlockSize { get; private set; }
        public int EdgeSize { get; private set; }
        public int TotalCells { get; }

        public SudokuBoard(int blockSize, int[,] initialGrid)
        {
            BlockSize = blockSize;
            EdgeSize = BlockSize * BlockSize;
            TotalCells = EdgeSize * EdgeSize;

            if (EdgeSize > 32)
                throw new NotSupportedException("int based bitmask supports only up to 32x32 puzzles");

            _allOnesMask = (int)((1L << EdgeSize) - 1);

            _puzzle = new int[TotalCells];
            _cellConstraints = new int[TotalCells];
            _rowConstraints = new int[EdgeSize];
            _columnConstraints = new int[EdgeSize];
            _blockConstraints = new int[EdgeSize];
            _cellToBlockMap = new int[TotalCells];

            InitializeConstraints();
            LoadInitialData(initialGrid);
        }

        private SudokuBoard(SudokuBoard other)
        {
            BlockSize = other.BlockSize;
            EdgeSize = other.EdgeSize;
            TotalCells = other.TotalCells;
            _allOnesMask = other._allOnesMask;

            // Raw memory copy
            _puzzle = new int[TotalCells];
            Buffer.BlockCopy(other._puzzle, 0, _puzzle, 0, TotalCells * sizeof(int));

            _cellConstraints = new int[TotalCells];
            Buffer.BlockCopy(other._cellConstraints, 0, _cellConstraints, 0, TotalCells * sizeof(int));

            _rowConstraints = new int[EdgeSize];
            Buffer.BlockCopy(other._rowConstraints, 0, _rowConstraints, 0, EdgeSize * sizeof(int));

            _columnConstraints = new int[EdgeSize];
            Buffer.BlockCopy(other._columnConstraints, 0, _columnConstraints, 0, EdgeSize * sizeof(int));

            _blockConstraints = new int[EdgeSize];
            Buffer.BlockCopy(other._blockConstraints, 0, _blockConstraints, 0, EdgeSize * sizeof(int));

            _cellToBlockMap = new int[TotalCells];
            Buffer.BlockCopy(other._cellToBlockMap, 0, _cellToBlockMap, 0, TotalCells * sizeof(int));
        }

        public ISudokuBoard Clone() => new SudokuBoard(this);
        public int GetCellValue(int row, int col) => _puzzle[GetIndex(row - 1, col - 1)];
        public bool IsSet(int row, int col) => _puzzle[GetIndex(row - 1, col - 1)] != 0;

        public void SetCellValue(int row, int col, int val)
        {
            int r = row - 1;
            int c = col - 1;
            int index = GetIndex(r, c);
            int blockIdx = _cellToBlockMap[index];
            int valMask = 1 << (val - 1);

            _puzzle[index] = val;

            _cellConstraints[index] = valMask;

            _rowConstraints[r] |= valMask;
            _columnConstraints[c] |= valMask;
            _blockConstraints[blockIdx] |= valMask;


        }

        public bool IsLegalAssignment(int row, int col, int val)
        {
            int r = row - 1;
            int c = col - 1;
            int blockIdx = _cellToBlockMap[GetIndex(r, c)];
            int valMask = 1 << (val - 1);

            return ((_rowConstraints[r] | _columnConstraints[c] | _blockConstraints[blockIdx]) & valMask) == 0;
        }

        public List<int> GetCellCandidates(int row, int col)
        {
            var candidates = new List<int>(EdgeSize);
            int r = row - 1;
            int c = col - 1;
            int blockIdx = _cellToBlockMap[GetIndex(r, c)];

            int usedMask = _rowConstraints[r] | _columnConstraints[c] | _blockConstraints[blockIdx];

            int freeMask = ~usedMask & _allOnesMask;

            for (int val = 1; val <= EdgeSize; val++)
            {
                if ((freeMask & (1 << (val - 1))) != 0)
                {
                    candidates.Add(val);
                }
            }
            return candidates;
        }

        public void RemoveCellCandidate(int row, int col, int val)
        {
            _cellConstraints[GetIndex(row - 1, col - 1)] &= ~(1 << (val - 1));
        }

        public bool IsSolved()
        {
            for (int i = 0; i < EdgeSize; i++)
            {
                if (_rowConstraints[i] != _allOnesMask ||
                    _columnConstraints[i] != _allOnesMask ||
                    _blockConstraints[i] != _allOnesMask)
                {
                    return false;
                }
            }
            return true;
        }

        public List<(int Row, int Col)> GetNonAssignedCells()
        {
            var list = new List<(int, int)>(TotalCells);
            for (int i = 0; i < TotalCells; i++)
            {
                if (_puzzle[i] == 0)
                {
                    int r = i / EdgeSize;
                    int c = i % EdgeSize;
                    list.Add((r + 1, c + 1));
                }
            }
            return list;
        }

        public List<(int Row, int Col, int Count)>? GetNonAssignedCellsWithCount()
        {
            var list = new List<(int, int, int)>(TotalCells);

            for (int i = 0; i < TotalCells; i++)
            {
                if (_puzzle[i] == 0)
                {
                    int candidatesMask = _cellConstraints[i];

                    int count = BitOperations.PopCount((uint)candidatesMask);

                    if (count == 0) return null;  // Logic violation (empty cell with no candidates)

                    int r = i / EdgeSize;
                    int c = i % EdgeSize;
                    list.Add((r + 1, c + 1, count));
                }
            }
            return list;
        }

        private void InitializeConstraints()
        {
            for (int r = 0; r < EdgeSize; r++)
            {
                for (int c = 0; c < EdgeSize; c++)
                {
                    _cellToBlockMap[r * EdgeSize + c] = (r / BlockSize) * BlockSize + (c / BlockSize);
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
                    int index = GetIndex(row, col);

                    if (val == 0)
                    {
                        _puzzle[index] = 0;
                        _cellConstraints[index] = _allOnesMask; // All candidates valid initially
                    }
                    else
                    {
                        SetCellValue(row + 1, col + 1, val);
                    }
                }
            }
        }

        private int GetIndex(int row, int col) => row * EdgeSize + col;

        public override string ToString()
        {
            var sb = new System.Text.StringBuilder(TotalCells);

            for (int i = 0; i < TotalCells; i++)
            {
                sb.Append(_puzzle[i]);
            }

            return sb.ToString();
        }
    }
}
    

