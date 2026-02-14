using Sudoku.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sudoku.Core.Interfaces
{
    /// <summary>
    /// renders board in visual format
    /// </summary>
    public interface ISudokuRenderer
    {
        /// <summary>
        /// renders board
        /// </summary>
        /// <param name="board">ISudokuBoard object containing board</param>
        void Render(ISudokuBoard board);
    }
}
