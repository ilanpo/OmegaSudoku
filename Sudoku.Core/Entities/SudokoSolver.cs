using System;
using System.Collections.Generic;
using Sudoku.Core.Interfaces;

namespace Sudoku.Solvers
{
    public class SudokuSolver : ISudokuSolver
    {
        public long SearchCounter { get; private set; }

        public ISudokuBoard? Solve(ISudokuBoard sudoku, string? strategies = "")
        {
            SearchCounter++;

            if (SearchCounter > 20000000) return null; // exit at overly large number of searches in case of infinite loop

            if (sudoku.IsSolved())
                return sudoku;

            if (!string.IsNullOrEmpty(strategies))
            {
                if (strategies.Contains('r') && CandidateReduction(sudoku) && sudoku.IsSolved()) return sudoku;
                if (strategies.Contains('u') && UniqueCandidate(sudoku) && sudoku.IsSolved()) return sudoku;
                if (strategies.Contains('h') && HiddenPair(sudoku) && sudoku.IsSolved()) return sudoku;
                if (strategies.Contains('n') && NakedPair(sudoku) && sudoku.IsSolved()) return sudoku;
            }


            var nonAssignedCells = sudoku.GetNonAssignedCellsWithCount();

            if (nonAssignedCells == null) return null;

            if (nonAssignedCells.Count == 0) return sudoku;


            // order the non assigned cells according to number of candidates, and then by their row number
            nonAssignedCells.Sort((a, b) => a.Count != b.Count ? a.Count - b.Count : a.Row - b.Row);

            var (row, col, count) = nonAssignedCells[0];
            var candidates = sudoku.GetCellCandidates(row, col);

            foreach (int val in candidates)
            {
                ISudokuBoard nextState = sudoku.Clone();
                nextState.SetCellValue(row, col, val);

                ISudokuBoard? result = Solve(nextState, strategies);

                if (result != null) return result;
            }

            return null;
        }

        public void ResetCounter()
        {
            SearchCounter = 0;
        }

        private bool CandidateReduction(ISudokuBoard sudoku)
        {
            bool nonStable = true;

            while (nonStable)
            {
                nonStable = false;

                List<(int row, int col)> nonAssignedCells = sudoku.GetNonAssignedCells();

                if (nonAssignedCells == null) return false;
                if (nonAssignedCells.Count == 0) return true;

                foreach ((int row, int col) in nonAssignedCells)
                {
                    List<int> candidates = sudoku.GetCellCandidates(row, col);

                    if (candidates.Count == 0)
                    {
                        // dead end because cell is empty but has no legal moves left
                        return false;
                    }
                    else if (candidates.Count == 1)
                    {
                        sudoku.SetCellValue(row, col, candidates[0]);
                        nonStable = true; // board changed so we must restart the loop to propagate constraints
                    }
                }
            }

            return true;
        }
        private bool UniqueCandidate(ISudokuBoard sudoku)
        {
            bool nonStable = true;

            while (nonStable)
            {
                nonStable = false;
                var nonAssignedCells = sudoku.GetNonAssignedCells();

                if (nonAssignedCells.Count == 0) return true;

                foreach (var (row, col) in nonAssignedCells)
                {
                    var candidates = sudoku.GetCellCandidates(row, col);

                    if (candidates.Count == 0) return false; // board invalid

                    bool valueSet = false;

                    foreach (int val in candidates)
                    {
                        // check if val is unique in row, column, or block
                        if (IsUniqueInUnit(sudoku, row, col, val, "Row") ||
                            IsUniqueInUnit(sudoku, row, col, val, "Col") ||
                            IsUniqueInUnit(sudoku, row, col, val, "block"))
                        {
                            sudoku.SetCellValue(row, col, val);
                            nonStable = true;
                            valueSet = true;
                            break;
                        }
                    }

                    // if board was modified break to work with newer board
                    if (valueSet) break;
                }

                // validate with candidate reduction
                if (nonStable)
                {
                    if (!CandidateReduction(sudoku)) return false;
                }
            }

            return true;
        }

        private List<(int row, int col)> MakeUnit(ISudokuBoard sudoku, int startRow, int startCol, int val, string unitType)
        {
            var unitCells = new List<(int row, int col)>();

            if (unitType == "row")
            {
                for (int col = 0; col < sudoku.EdgeSize; col++) unitCells.Add((startRow, col));
            }
            else if (unitType == "col")
            {
                for (int row = 0; row < sudoku.EdgeSize; row++) unitCells.Add((row, startCol));
            }
            else // block
            {
                int startBlockRow = (startRow / sudoku.BlockSize) * sudoku.BlockSize;
                int startBlockCol = (startCol / sudoku.BlockSize) * sudoku.BlockSize;
                for (int r = 0; r < sudoku.BlockSize; r++)
                    for (int c = 0; c < sudoku.BlockSize; c++)
                        unitCells.Add((startBlockRow + r, startBlockCol + c));
            }

            return unitCells;
        }

        private bool IsUniqueInUnit(ISudokuBoard sudoku, int startRow, int startCol, int val, string unitType)
        {
            var unitCells = MakeUnit(sudoku, startRow, startCol, val, unitType);

            foreach (var (row, col) in unitCells)
            {
                if (row == startRow && col == startCol) continue;

                if (sudoku.IsSet(row, col)) continue;

                var neighborsCandidates = sudoku.GetCellCandidates(row, col);
                if (neighborsCandidates.Contains(val))
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