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
    public class UniqueCandidateStrategy : ISudokuStrategy
    {
        public char Key => 'u';

        /// <summary>
        /// applies the "unique candidate" or "hidden single" heuristic to the board
        /// </summary>
        /// <remarks>
        /// a "unique candidate" is when a cell is valid only in that cell in that row even if that cell has other candidates, this method uses stackalloc to create shared temporary buffers for high performance
        /// </remarks>
        /// <param name="sudoku"> <see cref="ISudokuBoard"/> object containing a valid sudoku board </param>
        /// <returns>
        /// StrategyStatus.Changed if the board changed and remains valid after applying strategy
        /// StrategyStatus.None if nothing was done
        /// StrategyStatus.Failed if a mismatch was found (like a cell with zero candidates) indicative of a dead end
        /// </returns>
        public StrategyStatus Apply(ISudokuBoard board)
        {
            bool changed = false;
            int size = board.EdgeSize;

            Span<int> counts = stackalloc int[size + 1];
            Span<int> lastRow = stackalloc int[size + 1];
            Span<int> lastCol = stackalloc int[size + 1];

            for (int i = 1; i <= size; i++)
            {
                counts.Clear();
                ScanRegion(board, i, RegionType.Row, counts, lastRow, lastCol);
                if (!ApplyLogic(board, counts, lastRow, lastCol, ref changed)) return StrategyStatus.Failed;
            }

            for (int i = 1; i <= size; i++)
            {
                counts.Clear();
                ScanRegion(board, i, RegionType.Col, counts, lastRow, lastCol);
                if (!ApplyLogic(board, counts, lastRow, lastCol, ref changed)) return StrategyStatus.Failed;
            }

            for (int i = 0; i < size; i++)
            {
                counts.Clear();
                ScanRegion(board, i, RegionType.Block, counts, lastRow, lastCol);
                if (!ApplyLogic(board, counts, lastRow, lastCol, ref changed)) return StrategyStatus.Failed;
            }

            return changed ? StrategyStatus.Changed : StrategyStatus.None;
        }

        private enum RegionType { Row, Col, Block }

        /// <summary>
        /// scans a specific region to identify and apply "unique candidate" candidates.
        /// </summary>
        /// <param name="sudoku"> <see cref="ISudokuBoard"/> object containing a valid sudoku board </param>
        /// <param name="index"> index of region so for row/col 5 is fifth row/col and for block we count left to right and down the board </param>
        /// <param name="type"> type of region, can be: row collumn or block </param>
        /// <param name="counts"> array tracking how often each value appears </param>
        /// <param name="lastRow"> array tracking the last row for each value </param>
        /// <param name="lastCol"> array tracking the last collumn for each value </param>
        private void ScanRegion(ISudokuBoard sudoku, int index, RegionType type,
                                Span<int> counts, Span<int> lastRow, Span<int> lastCol)
        {
            int size = sudoku.EdgeSize;

            if (type == RegionType.Row)
            {
                int r = index;
                for (int c = 1; c <= size; c++) ProcessCell(sudoku, r, c, counts, lastRow, lastCol);
            }
            else if (type == RegionType.Col)
            {
                int c = index;
                for (int r = 1; r <= size; r++) ProcessCell(sudoku, r, c, counts, lastRow, lastCol);
            }
            else // block
            {
                int blockSize = sudoku.BlockSize;
                int startRow = (index / blockSize) * blockSize + 1;
                int startCol = (index % blockSize) * blockSize + 1;

                for (int r = 0; r < blockSize; r++)
                {
                    for (int c = 0; c < blockSize; c++)
                    {
                        ProcessCell(sudoku, startRow + r, startCol + c, counts, lastRow, lastCol);
                    }
                }
            }
        }

        /// <summary>
        /// updates the frequency count for a cells valid values, used to tell if any value appears only once in row collumn or block
        /// </summary>
        /// <param name="sudoku"> <see cref="ISudokuBoard"/> object containing a valid sudoku board </param>
        /// <param name="r"> row of cell to count </param>
        /// <param name="c"> collumn of cell to count </param>
        /// <param name="counts"> array tracking how often each value appears </param>
        /// <param name="lastRow"> array tracking the last row for each value </param>
        /// <param name="lastCol"> array tracking the last collumn for each value </param>
        private void ProcessCell(ISudokuBoard sudoku, int r, int c,
                                 Span<int> counts, Span<int> lastRow, Span<int> lastCol)
        {
            if (sudoku.IsSet(r, c))
            {
                counts[sudoku.GetCellValue(r, c)] = -99; // Mark as handled
            }
            else
            {
                int mask = sudoku.GetCandidatesMask(r, c);
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
        }

        /// <summary>
        /// goes through all possible values and checks if value is a unique candidate in last cell
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
        private bool ApplyLogic(ISudokuBoard sudoku, Span<int> counts,
                                Span<int> lastRow, Span<int> lastCol, ref bool changed)
        {
            int size = sudoku.EdgeSize;

            for (int v = 1; v <= size; v++)
            {
                if (counts[v] == 0) return false;

                if (counts[v] == 1)
                {
                    sudoku.SetCellValue(lastRow[v], lastCol[v], v);
                    changed = true;
                }
            }
            return true;
        }
    }
}
