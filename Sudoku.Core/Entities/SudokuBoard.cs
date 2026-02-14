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
    /// <summary>
    /// Class that represents a sudoku board and contains methods for interacting and extracting data from board
    /// </summary>
    public class SudokuBoard : ISudokuBoard
    {
        /// <summary>
        /// 1d array containing all set values of cells in board, 0 if empty
        /// </summary>
        private readonly int[] _puzzle;

        // arrays containing bitmask based constraints for every row column and block
        private readonly int[] _rowConstraints;
        private readonly int[] _columnConstraints;
        private readonly int[] _blockConstraints;

        /// <summary>
        /// array storing bitmask based constraints for every cell, linearly for better performance
        /// </summary>
        private readonly int[] _cellConstraints;

        /// <summary>
        /// array storing the block id of every cell, linearly for better performance.
        /// pays in memory for performance thanks to not having to calculate which block a cell is in every time
        /// </summary>
        private readonly int[] _cellToBlockMap;

        /// <summary>
        /// mask where all bits we are working with are set
        /// </summary>
        private readonly int _allOnesMask;

        /// <summary>
        /// 1d array of bitmasks of banned contraints for every cell
        /// </summary>
        private readonly int[] _banConstraints;

        /// <summary>
        /// size of the edge of a block, so in a 9x9 suduko every block is 3x3 so the block size is 3
        /// </summary>
        public int BlockSize { get; private set; }

        /// <summary>
        /// size of the edge of the suduko, so in a 9x9 suduko the edge size is 9
        /// </summary>
        public int EdgeSize { get; private set; }

        /// <summary>
        /// number of cells total in puzzle, so in a 9x9 suduko total cell count is 81
        /// </summary>
        public int TotalCells { get; }

        /// <summary>
        /// Constructor for board that takes the block size of the board and the initial state of the board
        /// </summary>
        /// <param name="blockSize"> size of the edge of a block, so in a 9x9 suduko every block is 3x3 so the block size is 3 </param>
        /// <param name="initialGrid"> 2s array where every [x,y] is the values of the cell at [row, column] </param>
        /// <exception cref="NotSupportedException"> throws an exception if the puzzle is larger than the bitmask can support </exception>
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
            _banConstraints = new int[TotalCells];

            InitializeConstraints();
            LoadInitialData(initialGrid);
        }

        /// <summary>
        /// efficient copy constructor for the board, that uses raw memory copying
        /// </summary>
        /// <param name="other"> board that is copied </param>
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

            _banConstraints = new int[TotalCells];
            Buffer.BlockCopy(other._banConstraints, 0, _banConstraints, 0, TotalCells * sizeof(int));
        }

        /// <summary>
        /// clones board into new identical board object
        /// </summary>
        /// <returns>new identical board object</returns>
        public ISudokuBoard Clone() => new SudokuBoard(this);

        /// <summary>
        /// returns value of specified cell, 0 if empty
        /// </summary>
        /// <param name="row">row of cell</param>
        /// <param name="col">column of cell</param>
        /// <returns>value of cell,  0 if empty</returns>
        public int GetCellValue(int row, int col) => _puzzle[GetIndex(row - 1, col - 1)];

        /// <summary>
        /// returns bool of if cell's value is set
        /// </summary>
        /// <param name="row">row of cell</param>
        /// <param name="col">column of cell</param>
        /// <returns>true if cell's values is set, false if cell is empty</returns>
        public bool IsSet(int row, int col) => _puzzle[GetIndex(row - 1, col - 1)] != 0;

        /// <summary>
        /// sets a cells value and updates the relevant bitmasks
        /// </summary>
        /// <param name="row">row of cell</param>
        /// <param name="col">column of cell</param>
        /// <param name="val">new value</param>
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

        /// <summary>
        /// checks if given assignment is legal according to the constraints and bitmasks
        /// </summary>
        /// <param name="row">row of cell</param>
        /// <param name="col">column of cell</param>
        /// <param name="val">new value</param>
        /// <returns>true if assignment is legal, false if it isnt</returns>
        public bool IsLegalAssignment(int row, int col, int val)
        {
            int r = row - 1;
            int c = col - 1;
            int blockIdx = _cellToBlockMap[GetIndex(r, c)];
            int valMask = 1 << (val - 1);

            return ((_rowConstraints[r] | _columnConstraints[c] | _blockConstraints[blockIdx]) & valMask) == 0;
        }

        /// <summary>
        /// finds all valid candidates for a cell
        /// </summary>
        /// <param name="row">row of cell</param>
        /// <param name="col">column of cell</param>
        /// <returns> list of all valid candidates </returns>
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

        /// <summary>
        /// removes a candidate from a cells constraints
        /// </summary>
        /// <param name="row">row of cell</param>
        /// <param name="col">column of cell</param>
        /// <param name="val">value removed</param>
        public void RemoveCellCandidate(int row, int col, int val)
        {
            _cellConstraints[GetIndex(row - 1, col - 1)] &= ~(1 << (val - 1));
        }

        /// <summary>
        /// removes a mask of candidates from a cell
        /// </summary>
        /// <param name="row">row of cell</param>
        /// <param name="col">column of cell</param>
        /// <param name="maskToRemove">mask of values to remove</param>
        /// <returns>true if any candidates were removed otherwise false</returns>
        public bool RemoveCandidates(int row, int col, int maskToRemove)
        {
            int index = GetIndex(row - 1, col - 1);

            int actuallyRemoved = maskToRemove & ~_banConstraints[index];

            if (actuallyRemoved == 0) return false;

            _banConstraints[index] |= actuallyRemoved;
            return true;
        }

        /// <summary>
        /// scans the contraints to see if puzzle is solved
        /// </summary>
        /// <returns> true if solved, false if not</returns>
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

        /// <summary>
        /// returns a list of all cells which have not been assigned a value
        /// </summary>
        /// <returns>list of cells which have not been assigned a value</returns>
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

        /// <summary>
        /// returns a list of all cells which have not been assigned a value and their count of valid candidates
        /// </summary>
        /// <returns>list of cells which have not been assigned a value and their count of valid candidates</returns>
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

        /// <summary>
        /// gets the complete candidate bitmask for a cell
        /// </summary>
        /// <param name="row">row of cell</param>
        /// <param name="col">column of cell</param>
        /// <returns>complete candidate bitmask for given cell</returns>
        public int GetCandidatesMask(int row, int col)
        {
            int r = row - 1;
            int c = col - 1;
            int index = GetIndex(r, c);
            int blockIdx = _cellToBlockMap[index];

            int usedMask = _rowConstraints[r] | _columnConstraints[c] | _blockConstraints[blockIdx];

            return (~usedMask & _allOnesMask) & ~_banConstraints[index];
        }

        /// <summary>
        /// finds and returns best empty cell in puzzle according to lowest number of possible candidates 
        /// </summary>
        /// <returns>best empty cell in puzzle and its count of valid candidates</returns>
        public (int Row, int Col, int Count) GetBestEmptyCell()
        {
            int minCount = int.MaxValue;
            int bestRow = -1;
            int bestCol = -1;

            for (int i = 0; i < TotalCells; i++)
            {
                if (_puzzle[i] == 0)
                {
                    int r = i / EdgeSize;
                    int c = i % EdgeSize;
                    int blockIdx = _cellToBlockMap[i];

                    int usedMask = _rowConstraints[r] | _columnConstraints[c] | _blockConstraints[blockIdx];
                    int freeMask = ~usedMask & _allOnesMask;

                    int count = BitOperations.PopCount((uint)freeMask);

                    if (count == 0) return (r + 1, c + 1, 0);

                    if (count == 1) return (r + 1, c + 1, 1);

                    if (count < minCount)
                    {
                        minCount = count;
                        bestRow = r + 1;
                        bestCol = c + 1;
                    }
                }
            }

            return (bestRow, bestCol, minCount);
        }

        /// <summary>
        /// initialises the cell to block map according to edge size and block size
        /// </summary>
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

        /// <summary>
        /// loads in the 2d array representing the initial state of the board, storing the cells in the relevant array initialising their constraints
        /// </summary>
        /// <param name="grid">2d int array containing the values of all cells in the board, 0 if empty</param>
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

        /// <summary>
        /// returns the 1d index of a cell based on its 2d position
        /// </summary>
        /// <param name="row">row of cell</param>
        /// <param name="col">column of cell</param>
        /// <returns></returns>
        private int GetIndex(int row, int col) => row * EdgeSize + col;

        /// <summary>
        /// builds a 1d string representation of board
        /// </summary>
        /// <returns>1d string representation of board</returns>
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
    

