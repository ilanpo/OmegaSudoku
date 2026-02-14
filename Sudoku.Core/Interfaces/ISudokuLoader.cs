using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sudoku.Core.Interfaces
{
    /// <summary>
    /// loads 2d array game boards from file or string
    /// </summary>
    public interface ISudokuLoader
    {
        /// <summary>
        /// loads single puzzle from source.
        /// </summary>
        /// <param name="source">string containing a puzzle or filepath for file containing at least 1 puzzle</param>
        /// <returns>2d array containing puzzle</returns>
        /// <exception cref="IOException">thrown if no puzzle found in file</exception>
        int[,] LoadPuzzle(string source);

        /// <summary>
        /// reads file and returns all valid puzzles found
        /// </summary>
        /// <remarks>
        /// supports two formats: one line puzzle, and grid puzzle. file can contain both mixed.
        /// </remarks>
        /// <param name="filePath">the path to the txt file</param>
        /// <returns>2D arrays representing the loaded puzzles.</returns>
        IEnumerable<int[,]> LoadAllPuzzles(string filePath);
    }
}
