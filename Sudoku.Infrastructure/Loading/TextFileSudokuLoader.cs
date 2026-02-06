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
        private readonly int _n = n;
        private readonly int _totalCells = n * n;

        public int[,] LoadPuzzle(string source)
        {
            if (source.Length < 260 && File.Exists(source))
            {
                string? firstLine = File.ReadLines(source).FirstOrDefault();

                if (firstLine != null && firstLine.Trim().Length >= _n * _n)
                {
                    return LoadFromOneLineFile(source);
                }
                else
                {
                    return LoadFromGridFile(source);
                }
            }

            return LoadFromOneLineString(source);
        }

        private int[,] LoadFromGridFile(string filePath)
        {
            var grid = new int[_n, _n];
            try
            {
                string[] lines = File.ReadAllLines(filePath);
                int row = 0;

                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line) || row > _n) continue;

                    string[] vals = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                    if (vals.Length != _n)
                        throw new FormatException($"Error in file {filePath}: row {row} does not contain {_n} numbers.");

                    for (int col = 0; col < vals.Length; col++)
                    {
                        grid[row, col] = int.Parse(vals[col]);
                    }
                    row++;
                }
            }
            catch (Exception ex)
            {
                throw new IOException("Failed to read grid file", ex);
            }
            return grid;
        }

        private int[,] LoadFromOneLineFile(string filePath)
        {
            string fileContent;
            try
            {
                fileContent = File.ReadAllText(filePath).Trim();
            }
            catch (Exception ex)
            {
                throw new IOException($"Failed to read One-Line File: {filePath}", ex);
            }

            return LoadFromOneLineString(fileContent);
        }


        private int[,] LoadFromOneLineString(string input)
        {
            var cleanDigits = new List<int>();
            foreach (char c in input)
            {
                if (char.IsDigit(c))
                    cleanDigits.Add(int.Parse(c.ToString()));
            }

            if (cleanDigits.Count != _n * _n)
            {
                throw new ArgumentException($"Invalid puzzle string. expected {_n * _n} digits, found {cleanDigits.Count}.");
            }

            var grid = new int[_n, _n];
            int index = 0;
            for (int row = 0; row < _n; row++)
            {
                for (int col = 0; col < _n; col++)
                {
                    grid[row, col] = cleanDigits[index++];
                }
            }

            return grid;
        }
    }
}

