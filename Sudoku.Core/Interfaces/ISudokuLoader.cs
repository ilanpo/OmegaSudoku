using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sudoku.Core.Interfaces
{
    public interface ISudokuLoader
    {
        int[,] LoadPuzzle(string source);

        IEnumerable<int[,]> LoadAllPuzzles(string filePath);
    }
}
