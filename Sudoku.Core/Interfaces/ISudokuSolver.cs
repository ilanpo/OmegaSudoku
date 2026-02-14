using Sudoku.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sudoku.Core.Interfaces
{
    /// <summary>
    /// solves sudoku puzzle board
    /// </summary>
    public interface ISudokuSolver
    {
        /// <summary>
        /// number of operations taken to solve board, not automatically reset
        /// </summary>
        long SearchCounter {get;}

        /// <summary>
        /// Method that solves a given sudoku board using strategies specified in given strategies <c>string</c>
        /// </summary>
        /// <param name="sudoku"> <see cref="ISudokuBoard"/> object containing a valid sudoku board </param>
        /// <param name="strategies"> a <c>string</c> contining the strategies to be used when solving the suduko, 
        /// currently supported are candidate reduction ('r') and unique candidate ('u') both would be used if strategies was "ru"
        /// </param>
        /// <returns> the solved version of the board or <c>null</c> if the board cannot be solved </returns>
        ISudokuBoard? Solve(ISudokuBoard sudoku, string strategies = "");

        /// <summary>
        /// reset for SearchCounter
        /// </summary>
        void ResetCounter();
    }
}
