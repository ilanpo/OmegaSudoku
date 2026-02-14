using Sudoku.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using static Sudoku.Core.Interfaces.ISudokuStrategy;

namespace Sudoku.Core.Strategies
{
    public class CandidateReductionStrategy : ISudokuStrategy
    {
        public char Key => 'r';

        /// <summary>
        /// applies the "candidate reduction" or "naked single" heuristic to the board
        /// </summary>
        /// /// <remarks>
        /// a "unique candidate" is when a cell is valid only in that cell in that row even if that cell has other candidates, 
        /// </remarks>
        /// <param name="sudoku"> <see cref="ISudokuBoard"/> object containing a valid sudoku board </param>
        /// <returns>
        /// StrategyStatus.Changed if the board changed and remains valid after applying strategy
        /// StrategyStatus.None if nothing was done
        /// StrategyStatus.Failed if a mismatch was found (like a cell with zero candidates) indicative of a dead end
        /// </returns>
        public StrategyStatus Apply(ISudokuBoard sudoku)
        {
            bool changed = false;
            int size = sudoku.EdgeSize;

            for (int r = 1; r <= size; r++)
            {
                for (int c = 1; c <= size; c++)
                {
                    if (!sudoku.IsSet(r, c))
                    {
                        int mask = sudoku.GetCandidatesMask(r, c);

                        if (mask == 0) return StrategyStatus.Failed; // dead end because cell is empty but has no legal moves left

                        if (BitOperations.PopCount((uint)mask) == 1)
                        {
                            int val = BitOperations.TrailingZeroCount(mask) + 1;
                            sudoku.SetCellValue(r, c, val);
                            changed = true; // board changed so we must restart the loop to propagate constraints
                        }
                    }
                }
            }
            return changed ? StrategyStatus.Changed : StrategyStatus.None;
        }
    }
}
