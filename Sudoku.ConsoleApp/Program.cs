using Sudoku.Core.Entities;
using Sudoku.Core.Interfaces;
using Sudoku.Infrastructure.Loading;  
using Sudoku.Infrastructure.Rendering;
using System;
using System.Diagnostics;
using System.IO;

namespace Sudoku.ConsoleApp
{
    /// <summary>
    /// Console app for sudoku solver
    /// </summary>
    class Program
    {
        /// <summary>
        /// main loop for console app, prompts user for all necessary info like size of suduko, which strategies to use, and the puzzle/puzzles to solve
        /// </summary>
        /// <param name="args">not used</param>
        static void Main(string[] args)
        {
            Console.WriteLine("=== Omega Sudoku ===");
            Console.WriteLine("Block size: size of each of the blocks of the suduko so 3 for standard 9x9 board, 2 for 4x4 board etc\n");
            Console.WriteLine("Heuristic strategies: r -> Candidate 'r'eduction, u -> 'u'nique Candidate, h -> 'h'idden Pair, n -> 'n'aked Pair");
            Console.WriteLine("so type 'ruhn' for all the strategies or leave it blank for just backtracking\n");
            Console.WriteLine("Input: either path to .txt file (supports one line puzzles as well as grids) or a string containing a puzzle\n");
            Console.WriteLine("Show: if yes then renders a nice looking board, if no just spits out a string containing the solution\n");
            Console.WriteLine("type 'exit' or 'quit' at any prompt to quit");

            while (true)
            {
                try
                {
                    Console.WriteLine("--------------------------------------------------");

                    string? sizeInput = PromptUser("Enter block size (3 for standard 9x9 board, 2 for 4x4 board etc):");
                    if (IsExit(sizeInput)) break;
                    int blockSize = string.IsNullOrWhiteSpace(sizeInput) ? 3 : int.Parse(sizeInput);
                    ValidateBlockSize(blockSize);

                    string? strategies = PromptUser("Which strategies to use:");
                    if (IsExit(strategies)) break;

                    string? input = PromptUser("Enter path to .txt file or string containing puzzle:");
                    if (IsExit(input)) break;
                    if (string.IsNullOrWhiteSpace(input)) continue;

                    string? show = PromptUser("Show solution y/n:");
                    if (IsExit(input)) break;

                    IEnumerable<int[,]> puzzleStream = GetPuzzles(input, blockSize);

                    HandlePuzzles(puzzleStream, blockSize, strategies, show);
                }
                catch (FormatException)
                {
                    Console.WriteLine("[Error] invalid number format");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Error] {ex.Message}");
                }
            }

            Console.WriteLine("goodbye");
        }

        /// <summary>
        /// iterates through all puzzles in stream, solves each one and displays time it took, shows longest time taken and number of puzzles solved at the end.
        /// </summary>
        /// <param name="puzzleSource"></param>
        /// <param name="blockSize"></param>
        /// <param name="strategies"></param>
        /// <param name="showArg"></param>
        static void HandlePuzzles(IEnumerable<int[,]> puzzleSource, int blockSize, string? strategies, string? showArg)
        {
            var stopwatch_total = Stopwatch.StartNew();
            var stopwatch_local = new Stopwatch();
            var longest_time = 0.0;
            int total = 0;
            bool show = IsYes(showArg);

            try
            {
                foreach (int[,] grid in puzzleSource)
                {
                    total++;
                    stopwatch_local.Restart();

                    SolvePuzzle(grid, blockSize, strategies, total, show);

                    stopwatch_local.Stop();
                    if (stopwatch_local.Elapsed.TotalSeconds > longest_time) longest_time = stopwatch_local.Elapsed.TotalSeconds;

                    Console.WriteLine($"Solved in {stopwatch_local.Elapsed.TotalSeconds} seconds");
                }

                stopwatch_total.Stop();

                if (total == 0)
                {
                    Console.WriteLine("File was found but contained no puzzles");
                }
                else
                {
                    Console.WriteLine($"{total} puzzles processed in {stopwatch_total.Elapsed.TotalSeconds} seconds total.");
                    Console.WriteLine($"longest time taken for any puzzle: {longest_time}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n[Error] {ex.Message}");
            }
        }

        /// <summary>
        /// shows result of attempt to solve puzzle
        /// </summary>
        /// <param name="grid"> puzzle to solve as 2d array </param>
        /// <param name="blockSize"> size of puzzle block, standard 9x9 puzzle has 3x3 blocks so block size is 3</param>
        /// <param name="strategies"> string containing strategies to use in solving </param>
        /// <param name="index"> index of puzzle in file, only used for nicer message </param>
        /// <param name="show"> toggle as to whether to show the rendered board or not </param>
        static void SolvePuzzle(int[,] grid, int blockSize, string strategies, int index, bool show)
        {
            var board = new SudokuBoard(blockSize, grid);
            var solver = new SudokuSolver();
            var renderer = new ConsoleBoardRenderer();

            var solution = solver.Solve(board, strategies);

            if (solution != null)
            {
                Console.WriteLine($"Puzzle #{index}: solved ({solver.SearchCounter} ops)");
                if (show) renderer.Render(solution);
                else Console.WriteLine($"Solution: {solution}");
            }
            else
            {
                Console.WriteLine($"Puzzle #{index}: no solution ({solver.SearchCounter} ops)");
            }
        }

        /// <summary>
        /// creates puzzle stream from puzzle string/file
        /// </summary>
        /// <param name="input"> </param>
        /// <param name="blockSize"> size of puzzle block, standard 9x9 puzzle has 3x3 blocks so block size is 3</param>
        /// <returns>puzzle stream of 2d arrays representing initial board states</returns>
        static IEnumerable<int[,]> GetPuzzles(string input, int blockSize)
        {
            ISudokuLoader loader = new TextFileSudokuLoader(blockSize * blockSize);

            if (File.Exists(input))
            {
                Console.WriteLine($"Reading from file: {Path.GetFileName(input)}");
                return loader.LoadAllPuzzles(input);
            }
            else
            {
                Console.WriteLine("Solving puzzle string");
                var grid = loader.LoadPuzzle(input);
                return [grid];
            }
        }

        /// <summary>
        /// helper to make sure user does not input invalid block size
        /// </summary>
        /// <param name="blockSize"> block size inputted by user </param>
        /// <exception cref="ArgumentException"> thrown if block size is invalid </exception>
        static void ValidateBlockSize(int blockSize)
        {
            if (blockSize <= 0) throw new ArgumentException("Block size cannot be zero or negative");

            if (blockSize > 3) throw new ArgumentException("Block sizes above 3 are not currently supported as hexadecimal values are not implemented");
        }

        /// <summary>
        /// small helper to prompt the user for input in a way that is clean looking
        /// </summary>
        /// <param name="message"> prompt message </param>
        /// <returns> user response </returns>
        static string? PromptUser(string message)
        {
            Console.WriteLine(message);
            Console.Write("> ");
            return Console.ReadLine()?.Trim();
        }

        /// <summary>
        /// helper to check if user input is some variation of exit or quit
        /// </summary>
        /// <param name="input"> user input string </param>
        /// <returns> whether input was the exit message </returns>
        static bool IsExit(string? input)
        {
            return input != null && (input.Equals("exit", StringComparison.OrdinalIgnoreCase) || input.Equals("quit", StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// helper to check if user input is some variation of y or yes
        /// </summary>
        /// <param name="input"> user input string </param>
        /// <returns> whether input was confirmation message </returns>
        static bool IsYes(string? input)
        {
            return input != null && (input.Equals("y", StringComparison.OrdinalIgnoreCase) || input.Equals("yes", StringComparison.OrdinalIgnoreCase));
        }
    }
}