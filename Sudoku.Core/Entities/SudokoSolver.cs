using Sudoku.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace Sudoku.Solvers
{
    /// <summary>
    /// This class's purpose is to solve the sudoku contained in a given <see cref="ISudokuBoard"/> object
    /// </summary>
    public class SudokuSolver : ISudokuSolver
    {
        /// <summary>
        /// counts how many times the recursive solve was run, not automatically reset
        /// </summary>
        public long SearchCounter { get; private set; }

        private bool _useCandidateReduction = false;
        private bool _useUniqueCandidate = false;
        private bool _useHiddenPair = false;
        private bool _useNakedPair = false;

        /// <summary>
        /// Method that solves a given sudoku board using strategies specified in given strategies <c>string</c>
        /// </summary>
        /// <param name="sudoku"> <see cref="ISudokuBoard"/> object containing a valid sudoku board </param>
        /// <param name="strategies"> a <c>string</c> contining the strategies to be used when solving the suduko, 
        /// currently supported are candidate reduction ('r') and unique candidate ('u') both would be used if strategies was "ru"
        /// </param>
        /// <returns> the solved version of the board or <c>null</c> if the board cannot be solved </returns>
        public ISudokuBoard? Solve(ISudokuBoard sudoku, string? strategies = "")
        {
            if (strategies.Contains('r')) _useCandidateReduction = true;
            if (strategies.Contains('u')) _useUniqueCandidate = true;
            if (strategies.Contains('h')) _useHiddenPair = true;
            if (strategies.Contains('n')) _useNakedPair = true;

            return RecursiveSolve(sudoku);
        }

        /// <summary>
        /// The recursive part of the sudoku solution, attempts to use given strategies and only then solves using backtracking on the best empty cell
        /// </summary>
        /// <param name="sudoku"> <see cref="ISudokuBoard"/> object containing a valid sudoku board </param>
        /// <returns> the solved version of the board or <c>null</c> if the board cannot be solved </returns>
        public ISudokuBoard? RecursiveSolve(ISudokuBoard sudoku)
        {
            SearchCounter++;

            if (SearchCounter > 20000000) return null; // exit at overly large number of searches in case of infinite loop

            if (sudoku.IsSolved())
                return sudoku;

            if (!ApplyStrategies(sudoku)) return null; // board must be be invalid


            var (row, col, candidateCount) = sudoku.GetBestEmptyCell();

            if (row == -1) return sudoku; // board solved

            if (candidateCount == 0) return null; // board must be be invalid

            int candidatesMask = sudoku.GetCandidatesMask(row, col);


            while (candidatesMask != 0)
            {
                int lowBit = candidatesMask & -candidatesMask;

                int val = BitOperations.TrailingZeroCount(lowBit) + 1;

                candidatesMask ^= lowBit;

                ISudokuBoard nextState = sudoku.Clone();
                nextState.SetCellValue(row, col, val);

                var result = RecursiveSolve(nextState);
                if (result != null) return result;
            }

            return null;
        }

        /// <summary>
        /// resets the search counter back to 0
        /// </summary>
        public void ResetCounter()
        {
            SearchCounter = 0;
        }

        /// <summary>
        /// attempts to solve given sudoku board using active strategies, continues attempts until strategies fail to alter the board
        /// </summary>
        /// <param name="sudoku"> <see cref="ISudokuBoard"/> object containing a valid sudoku board </param>
        /// <returns>
        /// <c>true</c> if the board remains valid after applying all active strategies
        /// <c>false</c> if a mismatch was found (like a cell with zero candidates) indicative of a dead end
        /// </returns>
        private bool ApplyStrategies(ISudokuBoard sudoku)
        {
            bool changed = true;
            while (changed)
            {
                changed = false;

                if (_useCandidateReduction)
                {
                    if (!CandidateReduction(sudoku, out bool reductionChanged)) return false;
                    if (reductionChanged) changed = true;
                }

                if (_useUniqueCandidate)
                {
                    if (!UniqueCandidate(sudoku, out bool uniqueChanged)) return false;
                    if (uniqueChanged) changed = true;
                }
            }
            return true;
        }

        /// <summary>
        /// applies the "candidate reduction" or "hidden single" heuristic to the board
        /// </summary>
        /// /// <remarks>
        /// a "unique candidate" is when a cell is valid only in that cell in that row even if that cell has other candidates, 
        /// </remarks>
        /// <param name="sudoku"> <see cref="ISudokuBoard"/> object containing a valid sudoku board </param>
        /// <param name="changed"> bool value indicative of whether the board was changed by the heuristic or not</param>
        /// <returns>
        /// <c>true</c> if the board remains valid after applying strategy
        /// <c>false</c> if a mismatch was found (like a cell with zero candidates) indicative of a dead end
        /// </returns>
        private bool CandidateReduction(ISudokuBoard sudoku, out bool changed)
        {
            changed = false;

            int size = sudoku.EdgeSize;
            for (int r = 1; r <= size; r++)
            {
                for (int c = 1; c <= size; c++)
                {
                    if (!sudoku.IsSet(r, c))
                    {
                        int mask = sudoku.GetCandidatesMask(r, c);

                        if (mask == 0) return false;  // dead end because cell is empty but has no legal moves left

                        if (BitOperations.PopCount((uint)mask) == 1)
                        {
                            int val = BitOperations.TrailingZeroCount(mask) + 1;
                            sudoku.SetCellValue(r, c, val);
                            changed = true; // board changed so we must restart the loop to propagate constraints
                        }
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// applies the "unique candidate" or "naked single" heuristic to the board
        /// </summary>
        /// <remarks>
        /// a "unique candidate" is when a cell is valid only in that cell in that row even if that cell has other candidates, 
        /// </remarks>
        /// <param name="sudoku"> <see cref="ISudokuBoard"/> object containing a valid sudoku board </param>
        /// <param name="changed"> bool value indicative of whether the board was changed by the heuristic or not</param>
        /// <returns>
        /// <c>true</c> if the board remains valid after applying strategy
        /// <c>false</c> if a mismatch was found (like a cell with zero candidates) indicative of a dead end
        /// </returns>
        private bool UniqueCandidate(ISudokuBoard sudoku, out bool changed)
        {
            changed = false;
            int size = sudoku.EdgeSize;

            for (int r = 1; r <= size; r++)
            {
                if (!CheckRow(sudoku, r, out bool rowChanged)) return false;
                if (rowChanged) changed = true;
            }

            for (int c = 1; c <= size; c++)
            {
                if (!CheckCol(sudoku, c, out bool colChanged)) return false;
                if (colChanged) changed = true;
            }

            for (int b = 0; b < size; b++)
            {
                if (!CheckBlock(sudoku, b, out bool blockChanged)) return false;
                if (blockChanged) changed = true;
            }

            return true;
        }

        /// <summary>
        /// scans a specific row to identify and apply "unique candidate" candidates.
        /// </summary>
        /// <remarks>
        /// this method uses stackalloc to create temporary buffers for high performance
        /// </remarks>
        /// <param name="sudoku"> <see cref="ISudokuBoard"/> object containing a valid sudoku board </param>
        /// <param name="row">The 1-based index of the row to check.</param>
        /// <param name="changed"> bool value indicative of whether the board was changed by the heuristic or not</param>
        /// <returns>
        /// <c>true</c> if the board remains valid after applying strategy
        /// <c>false</c> if a mismatch was found (like a cell with zero candidates) indicative of a dead end
        /// </returns>
        private bool CheckRow(ISudokuBoard sudoku, int row, out bool changed)
        {
            int size = sudoku.EdgeSize;

            Span<int> counts = stackalloc int[size + 1];
            Span<int> lastRow = stackalloc int[size + 1];
            Span<int> lastCol = stackalloc int[size + 1];

            for (int c = 1; c <= size; c++)
            {
                if (sudoku.IsSet(row, c))
                {
                    counts[sudoku.GetCellValue(row, c)] = -99; // Mark as handled
                }
                else
                {
                    CountCandidates(sudoku.GetCandidatesMask(row, c), row, c, counts, lastRow, lastCol);
                }
            }

            return FindHiddenSingles(sudoku, counts, lastRow, lastCol, out changed);
        }

        /// <summary>
        /// scans a specific collumn to identify and apply "unique candidate" candidates.
        /// </summary>
        /// <remarks>
        /// this method uses stackalloc to create temporary buffers for high performance
        /// </remarks>
        /// <param name="sudoku"> <see cref="ISudokuBoard"/> object containing a valid sudoku board </param>
        /// <param name="row">The 1-based index of the row to check.</param>
        /// <param name="changed"> bool value indicative of whether the board was changed by the heuristic or not</param>
        /// <returns>
        /// <c>true</c> if the board remains valid after applying strategy
        /// <c>false</c> if a mismatch was found (like a cell with zero candidates) indicative of a dead end
        /// </returns>
        private bool CheckCol(ISudokuBoard sudoku, int col, out bool changed)
        {
            int size = sudoku.EdgeSize;
            Span<int> counts = stackalloc int[size + 1];
            Span<int> lastRow = stackalloc int[size + 1];
            Span<int> lastCol = stackalloc int[size + 1];

            for (int r = 1; r <= size; r++)
            {
                if (sudoku.IsSet(r, col))
                {
                    counts[sudoku.GetCellValue(r, col)] = -99; // Mark as handled
                }
                else
                {
                    CountCandidates(sudoku.GetCandidatesMask(r, col), r, col, counts, lastRow, lastCol);
                }
            }

            return FindHiddenSingles(sudoku, counts, lastRow, lastCol, out changed);
        }

        /// <summary>
        /// scans a specific block to identify and apply "unique candidate" candidates.
        /// </summary>
        /// <remarks>
        /// this method uses stackalloc to create temporary buffers for high performance
        /// </remarks>
        /// <param name="sudoku"> <see cref="ISudokuBoard"/> object containing a valid sudoku board </param>
        /// <param name="row">The 1-based index of the row to check.</param>
        /// <param name="changed"> bool value indicative of whether the board was changed by the heuristic or not</param>
        /// <returns>
        /// <c>true</c> if the board remains valid after applying strategy
        /// <c>false</c> if a mismatch was found (like a cell with zero candidates) indicative of a dead end
        /// </returns>
        private bool CheckBlock(ISudokuBoard sudoku, int blockIdx, out bool changed)
        {
            int size = sudoku.EdgeSize;
            int blockSize = sudoku.BlockSize;
            Span<int> counts = stackalloc int[size + 1];
            Span<int> lastRow = stackalloc int[size + 1];
            Span<int> lastCol = stackalloc int[size + 1];

            int startRow = (blockIdx / blockSize) * blockSize + 1;
            int startCol = (blockIdx % blockSize) * blockSize + 1;

            for (int r = 0; r < blockSize; r++)
            {
                for (int c = 0; c < blockSize; c++)
                {
                    int currRow = startRow + r;
                    int currCol = startCol + c;

                    if (sudoku.IsSet(currRow, currCol))
                    {
                        counts[sudoku.GetCellValue(currRow, currCol)] = -99; // Mark as handled
                    }
                    else
                    {
                        CountCandidates(sudoku.GetCandidatesMask(currRow, currCol), currRow, currCol, counts, lastRow, lastCol);
                    }
                }
            }

            return FindHiddenSingles(sudoku, counts, lastRow, lastCol, out changed);
        }

        /// <summary>
        /// updates the frequency count for a cells valid values, used to tell if any value appears only once in row collumn or block
        /// </summary>
        /// <param name="mask"> the bitmask of valid values for the cell </param>
        /// <param name="r"> row of cell to count </param>
        /// <param name="c"> collumn of cell to count </param>
        /// <param name="counts"> array tracking how often each value appears </param>
        /// <param name="lastRow"> array tracking the last row for each value </param>
        /// <param name="lastCol"> array tracking the last collumn for each value </param>
        private void CountCandidates(int mask, int r, int c, Span<int> counts, Span<int> lastRow, Span<int> lastCol)
        {
            while (mask != 0)
            {
                int lowBit = mask & -mask; // only lowest bit of mask

                int val = BitOperations.TrailingZeroCount(lowBit) + 1; // converts bit to 1 based value

                mask ^= lowBit; // removes the lowest bit from mask

                if (counts[val] != -99) // -99 marks values already handled so we skip those
                {
                    counts[val]++;
                    lastRow[val] = r;
                    lastCol[val] = c;
                }
            }
        }

        /// <summary>
        /// goes through all possible values and checks if value is a unique candidate in given cell
        /// </summary>
        /// <param name="sudoku"> <see cref="ISudokuBoard"/> object containing a valid sudoku board </param>
        /// <param name="counts"> array tracking how often each value appears </param>
        /// <param name="lastRow"> array tracking the last row for each value </param>
        /// <param name="lastCol"> array tracking the last collumn for each value </param>
        /// <param name="changed"> bool value indicative of whether the board was changed by the heuristic or not</param>
        /// <returns>
        /// <c>true</c> if the board remains valid after applying strategy
        /// <c>false</c> if a mismatch was found (like a cell with zero candidates) indicative of a dead end
        /// </returns>
        private bool FindHiddenSingles(ISudokuBoard sudoku, Span<int> counts, Span<int> lastRow, Span<int> lastCol, out bool changed)
        {
            changed = false;
            int size = sudoku.EdgeSize;

            for (int v = 1; v <= size; v++)
            {
                int count = counts[v];

                if (count == 1)
                {
                    sudoku.SetCellValue(lastRow[v], lastCol[v], v);
                    changed = true;
                }
                else if (count == 0)
                {
                    return false;
                }
            }
            return true;
        }

        private static bool HiddenPair(ISudokuBoard sudoku) => false;
        private static bool NakedPair(ISudokuBoard sudoku) => false;
    }
}