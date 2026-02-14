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
    public class HiddenPairStrategy : ISudokuStrategy
    {
        public char Key => 'h';

        /// <summary>
        /// applies the "hidden pair" heuristic to the board
        /// </summary>
        /// <remarks>
        /// a "hidden pair" is when a value is valid only in 2 cells in that region, this method uses stackalloc to create shared temporary buffers for high performance
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

            // bitmask that maps where every candidate can be found in current region
            // for example when we scan row 1, if 3 is found in index 0 of row 1 and in index 6 of row 1 then regionPositions[3] will be: 001000001
            Span<int> regionPositions = stackalloc int[size + 1];

            for (int r = 1; r <= size; r++)
            {
                regionPositions.Clear();
                ScanRegion(sudoku, r, RegionType.Row, regionPositions);
                if (!FindAndEliminatePairs(sudoku, r, RegionType.Row, regionPositions, ref changed)) return StrategyStatus.Failed;
            }

            for (int c = 1; c <= size; c++)
            {
                regionPositions.Clear();
                ScanRegion(sudoku, c, RegionType.Col, regionPositions);
                if (!FindAndEliminatePairs(sudoku, c, RegionType.Col, regionPositions, ref changed)) return StrategyStatus.Failed;
            }

            for (int b = 0; b < size; b++)
            {
                regionPositions.Clear();
                ScanRegion(sudoku, b, RegionType.Block, regionPositions);
                if (!FindAndEliminatePairs(sudoku, b, RegionType.Block, regionPositions, ref changed)) return StrategyStatus.Failed;
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
        /// <param name="map"> bitmask that maps where every candidate can be found in current region </param>
        private void ScanRegion(ISudokuBoard sudoku, int index, RegionType type, Span<int> map)
        {
            int size = sudoku.EdgeSize;

            if (type == RegionType.Row)
            {
                int r = index;
                for (int c = 1; c <= size; c++) AddCellToMap(sudoku, r, c, c - 1, map);
            }
            else if (type == RegionType.Col)
            {
                int c = index;
                for (int r = 1; r <= size; r++) AddCellToMap(sudoku, r, c, r - 1, map); 
            }
            else // block
            {
                int blockSize = sudoku.BlockSize;
                int startRow = (index / blockSize) * blockSize + 1;
                int startCol = (index % blockSize) * blockSize + 1;

                for (int offset = 0; offset < size; offset++)
                {
                    int r = startRow + (offset / blockSize);
                    int c = startCol + (offset % blockSize);
                    AddCellToMap(sudoku, r, c, offset, map);
                }
            }
        }

        /// <summary>
        /// helper that adds all candidates of a cell to the regionPositions map at the cells position in the map
        /// </summary>
        /// <param name="sudoku"> <see cref="ISudokuBoard"/> object containing a valid sudoku board </param>
        /// <param name="r"> row of cell to add </param>
        /// <param name="c"> collumn of cell to add </param>
        /// <param name="position"> position of cell in map, 0 indexed </param>
        /// <param name="map"> bitmask that maps where every candidate can be found in current region </param>
        private void AddCellToMap(ISudokuBoard sudoku, int r, int c, int position, Span<int> map)
        {
            if (!sudoku.IsSet(r, c))
            {
                int mask = sudoku.GetCandidatesMask(r, c);
                int positionBit = 1 << position;

                while (mask != 0)
                {
                    int lowBit = mask & -mask;
                    int val = BitOperations.TrailingZeroCount(lowBit) + 1;
                    mask ^= lowBit;

                    map[val] |= positionBit; // marks candidate with value 'val' as available at this position in the region
                }
            }
        }

        /// <summary>
        /// Finds cells that are hidden pairs according to the map and eliminates all of their other candidates
        /// </summary>
        /// <param name="sudoku"> <see cref="ISudokuBoard"/> object containing a valid sudoku board </param>
        /// <param name="index"> index of region so for row/col 5 is fifth row/col and for block we count left to right and down the board </param>
        /// <param name="type"> type of region, can be: row collumn or block </param>
        /// <param name="map"> bitmask that maps where every candidate can be found in current region </param>
        /// <param name="changed"> bool value indicative of whether the board was changed by the heuristic or not</param>
        /// <returns>
        /// <c>true</c> if the board remains valid after applying strategy
        /// <c>false</c> if a mismatch was found (like a cell with zero candidates) indicative of a dead end
        /// </returns>
        private bool FindAndEliminatePairs(ISudokuBoard sudoku, int index, RegionType type,
                                           Span<int> map, ref bool changed)
        {
            int size = sudoku.EdgeSize;

            for (int v1 = 1; v1 < size; v1++)
            {
                int posMask1 = map[v1];

                if (BitOperations.PopCount((uint)posMask1) != 2) continue; // a hidden pair must appear in exactly two cells

                for (int v2 = v1 + 1; v2 <= size; v2++)
                {
                    int posMask2 = map[v2];

                    if (posMask1 == posMask2)
                    {
                        int pairCandidatesMask = (1 << (v1 - 1)) | (1 << (v2 - 1));

                        int cellIdx1 = BitOperations.TrailingZeroCount(posMask1);
                        int cellIdx2 = BitOperations.TrailingZeroCount(posMask1 & ~(1 << cellIdx1));

                        // eliminate other candidates which arent part of the pair
                        if (!EliminateOtherCandidates(sudoku, index, type, cellIdx1, pairCandidatesMask, ref changed)) return false;
                        if (!EliminateOtherCandidates(sudoku, index, type, cellIdx2, pairCandidatesMask, ref changed)) return false;
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// Eliminates other candidates which arent part of the pair
        /// </summary>
        /// <param name="sudoku"> <see cref="ISudokuBoard"/> object containing a valid sudoku board </param>
        /// <param name="index"> index of region so for row/col 5 is fifth row/col and for block we count left to right and down the board </param>
        /// <param name="type"> type of region, can be: row collumn or block </param>
        /// <param name="cellIndex"> index of cell to work on </param>
        /// <param name="pairMaskToKeep"> mask of the two values to keep </param>
        /// <param name="changed"> bool value indicative of whether the board was changed by the heuristic or not</param>
        /// <returns>
        /// <c>true</c> if the board remains valid after applying strategy
        /// <c>false</c> if a mismatch was found (like a cell with zero candidates) indicative of a dead end
        /// </returns>
        private bool EliminateOtherCandidates(ISudokuBoard sudoku, int index, RegionType type,
                                              int cellIndex, int pairMaskToKeep, ref bool changed)
        {
            int r = 0, c = 0;
            if (type == RegionType.Row)
            {
                r = index;
                c = cellIndex + 1;
            }
            else if (type == RegionType.Col)
            {
                r = cellIndex + 1;
                c = index;
            }
            else // block
            {
                int blockSize = sudoku.BlockSize;
                int startRow = (index / blockSize) * blockSize + 1;
                int startCol = (index % blockSize) * blockSize + 1;
                r = startRow + (cellIndex / blockSize);
                c = startCol + (cellIndex % blockSize);
            }

            int currentMask = sudoku.GetCandidatesMask(r, c);
            int maskToRemove = currentMask & ~pairMaskToKeep;

            if (maskToRemove != 0)
            {
                sudoku.RemoveCandidates(r, c, maskToRemove);
                changed = true;

                if (sudoku.GetCandidatesMask(r, c) == 0) return false;
            }
            return true;
        }
    }
}
