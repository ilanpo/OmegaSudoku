using Sudoku.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sudoku.Core.Interfaces
{
    public interface ISudokuBoard
    {
        int EdgeSize { get; }
        int BlockSize { get; }
        int TotalCells { get; }


        int GetCellValue(int row, int col);
        bool IsSet(int row, int col);
        bool IsSolved();
        bool IsLegalAssignment(int row, int col, int val);
        List<int> GetCellCandidates(int row, int col);


        void SetCellValue(int row, int col, int val);
        void RemoveCellCandidate(int row, int col, int val);


        ISudokuBoard Clone();
        List<(int Row, int Col)> GetNonAssignedCells();
        List<(int Row, int Col, int Count)>? GetNonAssignedCellsWithCount();
    }
}
