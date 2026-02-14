using Sudoku.Core.Interfaces;
using Sudoku.Core.Strategies;
using System;
using System.Collections.Generic;
using System.Numerics;
using static Sudoku.Core.Interfaces.ISudokuStrategy;

namespace Sudoku.Core.Entities
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

        /// <summary>
        /// Method that solves a given sudoku board using strategies specified in given strategies <c>string</c>
        /// </summary>
        /// <param name="sudoku"> <see cref="ISudokuBoard"/> object containing a valid sudoku board </param>
        /// <param name="strategies"> a <c>string</c> contining the strategies to be used when solving the suduko, 
        /// currently supported are candidate reduction ('r') and unique candidate ('u') both would be used if strategies was "ru"
        /// </param>
        /// <returns> the solved version of the board or <c>null</c> if the board cannot be solved </returns>
        public ISudokuBoard? Solve(ISudokuBoard sudoku, string strategies = "")
        {
            var strategyList = StrategyFactory.GetStrategies(strategies);

            return RecursiveSolve(sudoku, strategyList);
        }

        /// <summary>
        /// The recursive part of the sudoku solution, attempts to use given strategies and only then solves using backtracking on the best empty cell
        /// </summary>
        /// <param name="sudoku"> <see cref="ISudokuBoard"/> object containing a valid sudoku board </param>
        /// <returns> the solved version of the board or <c>null</c> if the board cannot be solved </returns>
        public ISudokuBoard? RecursiveSolve(ISudokuBoard sudoku, List<ISudokuStrategy> strategies)
        {
            SearchCounter++;

            if (SearchCounter > 20000000) return null; // exit at overly large number of searches in case of infinite loop

            if (sudoku.IsSolved())
                return sudoku;

            if (!ApplyStrategies(sudoku, strategies)) return null; // board must be be invalid


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

                var result = RecursiveSolve(nextState, strategies);
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
        private bool ApplyStrategies(ISudokuBoard sudoku, List<ISudokuStrategy> strategies)
        {
            if (strategies.Count == 0) return true;
            bool changed = true;
            while (changed)
            {
                changed = false;

                foreach (var strategy in strategies)
                {
                    var result = strategy.Apply(sudoku);

                    if (result == StrategyStatus.Failed)
                    {
                        return false;
                    }

                    if (result == StrategyStatus.Changed)
                    {
                        changed = true;
                        break; 
                    }
                }
            }
            return true;
        }
    }
}