using Sudoku.Core.Entities;
using Sudoku.Core.Interfaces;
using Sudoku.Solvers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sudoku.Tests
{
    /// <summary>
    /// Tests for the sudokusolver class
    /// </summary>
    public class SudokuSolverTests
    {
        // helper in order to not rely on the loader
        private static SudokuBoard CreateBoardFromString(string puzzle, int size)
        {
            int EdgeSize = size * size;
            int[,] grid = new int[EdgeSize, EdgeSize];
            for (int i = 0; i < EdgeSize * EdgeSize; i++)
            {
                int val = puzzle[i] == '.' || puzzle[i] == '0' ? 0 : puzzle[i] - '0';
                grid[i / EdgeSize, i % EdgeSize] = val;
            }
            return new SudokuBoard(size, grid);
        }

        [Fact]
        public void Solve_EasyPuzzle_ReturnsSolution()
        {
            string input = "003020600900305001001806400008102900700000008006708200002609500800203009005010300";
            ISudokuBoard board = CreateBoardFromString(input, 3);
            var solver = new SudokuSolver();

            ISudokuBoard? result = solver.Solve(board);

            Assert.NotNull(result);
            Assert.True(result.IsSolved());
            Assert.Empty(result.GetNonAssignedCells());
        }

        [Theory]
        [InlineData("550000000000000000000000000000000000000000000000000000000000000000000000000000000")]
        public void Solve_ImpossiblePuzzle_ReturnsNull(string impossible)
        {
            ISudokuBoard board = CreateBoardFromString(impossible, 3);
            var solver = new SudokuSolver();

            ISudokuBoard? result = solver.Solve(board, "ru");

            Assert.Null(result);
        }

        [Theory]
        [InlineData("", 74)]
        [InlineData("r", 37)]
        [InlineData("u", 40)]
        [InlineData("ru", 37)]
        public void Solve_UsingStrategy_ExpectedNumberOfSearches(string strategy, int searches)
        {
            int[,] grid = new int[9, 9];
            for (int k = 1; k < 9; k++) grid[0, k] = k;

            ISudokuBoard board = new SudokuBoard(3, grid);
            var solver = new SudokuSolver();

            ISudokuBoard? result = solver.Solve(board, strategy);

            Assert.NotNull(result);
            Assert.True(result.IsSolved());
            Assert.Equal(searches, solver.SearchCounter);
        }

        [Fact]
        public void Solve_4X4Board_ReturnsSolution()
        {
            string puzzle = "1002000000000000";
            ISudokuBoard board = CreateBoardFromString(puzzle, 2);
            var solver = new SudokuSolver();

            ISudokuBoard? result = solver.Solve(board);

            Assert.NotNull(result);
            Assert.True(result.IsSolved());
            Assert.Equal(4, result.EdgeSize);
        }
    }
}
