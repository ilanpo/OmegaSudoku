using Sudoku.Core.Entities;
using Sudoku.Core.Interfaces;

namespace Sudoku.Tests
{
    /// <summary>
    /// tests for the sudokuboard class
    /// </summary>
    public class SudokuBoardTests
    {
        // helper in order to not rely on the loader
        private static SudokuBoard CreateBoardFromString(string puzzle, int size)
        {
            int EdgeSize = size * size;
            int[,] grid = new int[EdgeSize, EdgeSize];
            for (int i = 0; i < EdgeSize*EdgeSize; i++)
            {
                int val = puzzle[i] == '.' || puzzle[i] == '0' ? 0 : puzzle[i] - '0';
                grid[i / EdgeSize, i % EdgeSize] = val;
            }
            return new SudokuBoard(size, grid);
        }

        [Fact]
        public void Constructor_ValidBoard_ShouldInitializeCorrectly()
        {
            int[,] grid = new int[9, 9];
            grid[0, 0] = 5;

            ISudokuBoard board = new SudokuBoard(3, grid);

            Assert.Equal(5, board.GetCellValue(1, 1));
            Assert.Equal(3, board.BlockSize);
            Assert.False(board.IsSolved());
        }

        [Theory]
        [InlineData(1, 2, 5, true)]  
        [InlineData(1, 5, 1, false)] 
        [InlineData(2, 1, 1, false)] 
        [InlineData(2, 2, 1, false)] 
        public void IsLegalAssignment_1InTopLeftCorner_ShouldValidateRules(int row, int col, int val, bool expected)
        {
            var grid = new int[9, 9];
            grid[0, 0] = 1;
            ISudokuBoard board = new SudokuBoard(3, grid);

            bool result = board.IsLegalAssignment(row, col, val);

            Assert.Equal(expected, result);
        }

        [Fact]
        public void IsSolved_FullAndValid_ReturnsTrue()
        {
            string solved = "435269781682571493197834562826195347374682915951743628519326874248957136763418259";
            ISudokuBoard board = CreateBoardFromString(solved, 3);

            bool result = board.IsSolved();

            Assert.True(result);
        }

        [Fact]
        public void Clone_3x3Board_ShouldCreateDeepCopy()
        {
            ISudokuBoard original = new SudokuBoard(3, new int[9, 9]);
            original.SetCellValue(1, 1, 5);

            ISudokuBoard copy = original.Clone();
            copy.SetCellValue(1, 1, 9);

            Assert.Equal(5, original.GetCellValue(1, 1));
            Assert.Equal(9, copy.GetCellValue(1, 1));    
            Assert.NotSame(original, copy);
        }

        [Fact]
        public void GetCellCandidates_3x3Board_ShouldFilterInvalidNumbers()
        {
            var grid = new int[9, 9];
            grid[0, 8] = 1;
            grid[8, 0] = 2;
            grid[1, 1] = 3; 

            ISudokuBoard board = new SudokuBoard(3, grid);

            List<int> candidates = board.GetCellCandidates(1, 1);

            Assert.DoesNotContain(1, candidates);
            Assert.DoesNotContain(2, candidates);
            Assert.DoesNotContain(3, candidates);
            Assert.Contains(4, candidates);
        }
    }
}