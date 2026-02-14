using Sudoku.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sudoku.Core.Interfaces
{
    /// <summary>
    /// Interface that represents a sudoku board and contains methods for interacting and extracting data from board
    /// </summary>
    public interface ISudokuBoard
    {
        /// <summary>
        /// size of the edge of the suduko, so in a 9x9 suduko the edge size is 9
        /// </summary>
        int EdgeSize { get; }

        /// <summary>
        /// size of the edge of a block, so in a 9x9 suduko every block is 3x3 so the block size is 3
        /// </summary>
        int BlockSize { get; }

        /// <summary>
        /// number of cells total in puzzle, so in a 9x9 suduko total cell count is 81
        /// </summary>
        int TotalCells { get; }


        /// <summary>
        /// returns value of specified cell, 0 if empty
        /// </summary>
        /// <param name="row">row of cell</param>
        /// <param name="col">column of cell</param>
        /// <returns>value of cell,  0 if empty</returns>
        int GetCellValue(int row, int col);

        /// <summary>
        /// returns bool of if cell's value is set
        /// </summary>
        /// <param name="row">row of cell</param>
        /// <param name="col">column of cell</param>
        /// <returns>true if cell's values is set, false if cell is empty</returns>
        bool IsSet(int row, int col);

        /// <summary>
        /// checks to see if puzzle is solved
        /// </summary>
        /// <returns> true if solved, false if not</returns>
        bool IsSolved();

        /// <summary>
        /// checks if given assignment is legal
        /// </summary>
        /// <param name="row">row of cell</param>
        /// <param name="col">column of cell</param>
        /// <param name="val">new value</param>
        /// <returns>true if assignment is legal, false if it isnt</returns>
        bool IsLegalAssignment(int row, int col, int val);

        /// <summary>
        /// finds all valid candidates for a cell
        /// </summary>
        /// <param name="row">row of cell</param>
        /// <param name="col">column of cell</param>
        /// <returns> list of all valid candidates </returns>
        List<int> GetCellCandidates(int row, int col);

        /// <summary>
        /// sets a cells value to val
        /// </summary>
        /// <param name="row">row of cell</param>
        /// <param name="col">column of cell</param>
        /// <param name="val">new value</param>
        void SetCellValue(int row, int col, int val);

        /// <summary>
        /// removes a candidate from cell
        /// </summary>
        /// <param name="row">row of cell</param>
        /// <param name="col">column of cell</param>
        /// <param name="val">value removed</param>
        void RemoveCellCandidate(int row, int col, int val);

        /// <summary>
        /// clones board into new identical board object
        /// </summary>
        /// <returns>new identical board object</returns>
        ISudokuBoard Clone();

        /// <summary>
        /// returns a list of all cells which have not been assigned a value
        /// </summary>
        /// <returns>list of cells which have not been assigned a value</returns>
        List<(int Row, int Col)> GetNonAssignedCells();

        /// <summary>
        /// returns a list of all cells which have not been assigned a value and their count of valid candidates
        /// </summary>
        /// <returns>list of cells which have not been assigned a value and their count of valid candidates</returns>
        List<(int Row, int Col, int Count)>? GetNonAssignedCellsWithCount();

        /// <summary>
        /// gets the complete candidate bitmask for a cell
        /// </summary>
        /// <param name="row">row of cell</param>
        /// <param name="col">column of cell</param>
        /// <returns>complete candidate bitmask for given cell</returns>
        int GetCandidatesMask(int row, int col);

        /// <summary>
        /// finds and returns best empty cell in puzzle according to lowest number of possible candidates 
        /// </summary>
        /// <returns>best empty cell in puzzle and its count of valid candidates</returns>
        (int Row, int Col, int Count) GetBestEmptyCell();
    }
}
