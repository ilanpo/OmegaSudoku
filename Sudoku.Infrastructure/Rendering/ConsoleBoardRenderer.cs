using Sudoku.Core.Entities;
using Sudoku.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sudoku.Infrastructure.Rendering
{
    public class ConsoleBoardRenderer : ISudokuRenderer
    {
        public void Render(ISudokuBoard board)
        {
            int n = board.EdgeSize;
            int b = board.BlockSize;

            for (int i = 1; i <= n; i++)
            {
                for (int j = 1; j <= n; j++)
                {
                    int val = board.GetCellValue(i, j);
                    string display = val == 0 ? "." : val.ToString();
                    Console.Write($"{display,2}");
                    if (j % b == 0 && j != n) Console.Write(" |");
                }
                if (i % b == 0 && i != n)
                {
                    Console.WriteLine();
                    Console.Write(" ---");
                    for (int sep = 1; sep <= n; sep++) Console.Write("--");
                }
                Console.WriteLine();
            }
            Console.WriteLine();
        }
    }
}
