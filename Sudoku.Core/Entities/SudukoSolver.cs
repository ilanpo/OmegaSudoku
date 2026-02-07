using System;
using System.Collections.Generic;
using Sudoku.Core.Interfaces;

namespace Sudoku.Solvers
{
    public class SudokuSolver : ISudokuSolver
    {
        public long SearchCounter { get; private set; }

        public ISudokuBoard? Solve(ISudokuBoard sudoku, string strategies = "")
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
                if (sudoku.IsLegalAssignment(row, col, val))
                {
                    ISudokuBoard nextState = sudoku.Clone();
                    nextState.SetCellValue(row, col, val);

                    ISudokuBoard? result = Solve(nextState, strategies);

                    if (result != null) return result;
                }
            }

            return null;
        }

        public void ResetCounter()
        {
            SearchCounter = 0;
        }

        private static bool CandidateReduction(ISudokuBoard sudoku) => false;
        private static bool UniqueCandidate(ISudokuBoard sudoku) => false;
        private static bool HiddenPair(ISudokuBoard sudoku) => false;
        private static bool NakedPair(ISudokuBoard sudoku) => false;
    }
}