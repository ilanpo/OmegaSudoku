using Sudoku.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sudoku.Core.Strategies
{
    public static class StrategyFactory
    {
        private static readonly Dictionary<char, ISudokuStrategy> _registry = new()
        {
            { 'r', new CandidateReductionStrategy() },
            { 'u', new UniqueCandidateStrategy() },
            { 'h', new HiddenPairStrategy() }
            // { 'n', new NakedPairStrategy() }
        };

        public static List<ISudokuStrategy> GetStrategies(string strategyString)
        {
            var active = new List<ISudokuStrategy>();
            if (string.IsNullOrEmpty(strategyString)) return active;

            foreach (char key in strategyString)
            {
                if (_registry.TryGetValue(key, out var strategy))
                {
                    active.Add(strategy);
                }
            }
            return active;
        }
    }
}
