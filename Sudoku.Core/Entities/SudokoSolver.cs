using Sudoku.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace Sudoku.Solvers
{
    public class SudokuSolver : ISudokuSolver
    {
        public long SearchCounter { get; private set; }

        private bool _useCandidateReduction = false;
        private bool _useUniqueCandidate = false;
        private bool _useHiddenPair = false;
        private bool _useNakedPair = false;

        public ISudokuBoard? Solve(ISudokuBoard sudoku, string? strategies = "")
        {
            if (strategies.Contains('r')) _useCandidateReduction = true;
            if (strategies.Contains('u')) _useUniqueCandidate = true;
            if (strategies.Contains('h')) _useHiddenPair = true;
            if (strategies.Contains('n')) _useNakedPair = true;

            return RecursiveSolve(sudoku);
        }


        public ISudokuBoard? RecursiveSolve(ISudokuBoard sudoku)
        {
            SearchCounter++;

            if (SearchCounter > 20000000) return null; // exit at overly large number of searches in case of infinite loop

            if (sudoku.IsSolved())
                return sudoku;

            if (!ApplyStrategies(sudoku)) return null;


            var (row, col, candidateCount) = sudoku.GetBestEmptyCell();

            if (row == -1) return sudoku;

            if (candidateCount == 0) return null;

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

        public void ResetCounter()
        {
            SearchCounter = 0;
        }

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

        private void CountCandidates(int mask, int r, int c, Span<int> counts, Span<int> lastRow, Span<int> lastCol)
        {
            while (mask != 0)
            {
                int lowBit = mask & -mask;
                int val = BitOperations.TrailingZeroCount(lowBit) + 1;
                mask ^= lowBit;

                if (counts[val] != -99)
                {
                    counts[val]++;
                    lastRow[val] = r;
                    lastCol[val] = c;
                }
            }
        }

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