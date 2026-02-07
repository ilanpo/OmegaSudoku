using Sudoku.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sudoku.Core.Interfaces
{
    public interface ISudokuSolver
    {
        long SearchCounter {get;}
        ISudokuBoard? Solve(ISudokuBoard sudoku, string strategies = "");
        void ResetCounter();
    }
}
