using Sudoku.Core.Entities;
using Sudoku.Core.Interfaces;
using Sudoku.Infrastructure.Loading;  
using Sudoku.Infrastructure.Rendering;
using Sudoku.Solvers;
using System;
using System.Diagnostics;
using System.IO;

namespace Sudoku.ConsoleApp
{
    class Program
    {
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

        static void SolvePuzzle(int[,] grid, int blockSize, string? strategies, int index, bool show)
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

        static void ValidateBlockSize(int blockSize)
        {
            if (blockSize <= 0) throw new ArgumentException("Block size cannot be zero or negative");

            if (blockSize > 3) throw new ArgumentException("Block sizes above 3 are not currently supported as hexadecimal values are not implemented");
        }

        static string? PromptUser(string message)
        {
            Console.WriteLine(message);
            Console.Write("> ");
            return Console.ReadLine()?.Trim();
        }

        static bool IsExit(string? input)
        {
            return input != null && (input.Equals("exit", StringComparison.OrdinalIgnoreCase) || input.Equals("quit", StringComparison.OrdinalIgnoreCase));
        }

        static bool IsYes(string? input)
        {
            return input != null && (input.Equals("y", StringComparison.OrdinalIgnoreCase) || input.Equals("yes", StringComparison.OrdinalIgnoreCase));
        }
    }
}