using Sudoku.Core.Entities;
using Sudoku.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sudoku.Infrastructure.Loading
{
    public class TextFileSudokuLoader(int n) : ISudokuLoader
    {
        private readonly int _edgeSize = n;
        private readonly int _totalCells = n * n;

        public int[,] LoadPuzzle(string source)
        {
            if (source.Length < 260 && File.Exists(source))
            {
                int[,]? puzzle = LoadAllPuzzles(source).FirstOrDefault();
                return puzzle ?? throw new IOException($"No valid {n}x{n} puzzle found in file: {source}");
            }

            return LoadFromOneLineString(source);
        }

        public IEnumerable<int[,]> LoadAllPuzzles(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"File not found {filePath}");

            var currentGridLines = new List<string>();

            foreach (string rawLine in File.ReadLines(filePath))
            {
                string line = rawLine.Trim();
                if (string.IsNullOrWhiteSpace(line)) continue;

                int digitCount = line.Count(c => char.IsDigit(c) || c == '.');

                if (digitCount >= _totalCells)
                {
                    currentGridLines.Clear();

                    int[,]? puzzle = null;
                    try { puzzle = LoadFromOneLineString(line); } catch { }

                    if (puzzle != null) yield return puzzle;
                }
                else
                {
                    currentGridLines.Add(line);

                    if (currentGridLines.Count == _edgeSize)
                    {
                        int[,]? gridPuzzle = LoadFromGrid(currentGridLines);
                        if (gridPuzzle != null)
                        {
                            yield return gridPuzzle;
                        }
                        currentGridLines.Clear();
                    }
                }
            }
        }

        private int[,]? LoadFromGrid(List<string> rows)
        {
            var grid = new int[_edgeSize, _edgeSize];
            int row = 0;

            foreach (string line in rows)
            {
                var validChars = line.Where(c => char.IsDigit(c) || c == '.').ToList();

                if (validChars.Count != _edgeSize)
                {
                    return null;
                }

                for (int col = 0; col < _edgeSize; col++)
                {
                    char c = validChars[col];
                    grid[row, col] = (c == '.') ? 0 : c - '0';
                }
                row++;
            }

            return grid;
        }

        private int[,] LoadFromOneLineString(string input)
        {
            var cleanDigits = new List<int>();
            foreach (char c in input)
            {
                if (char.IsDigit(c))
                    cleanDigits.Add(int.Parse(c.ToString()));
            }

            if (cleanDigits.Count != _totalCells)
            {
                throw new Exception($"invalid puzzle string expected {_totalCells} digits, found {cleanDigits.Count}.");
            }

            var grid = new int[_edgeSize, _edgeSize];
            int index = 0;
            for (int row = 0; row < _edgeSize; row++)
            {
                for (int col = 0; col < _edgeSize; col++)
                {
                    grid[row, col] = cleanDigits[index++];
                }
            }

            return grid;
        }
    }
}

