using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sudoku.Core.Interfaces
{
    public interface ISudokuStrategy
    {
        public enum StrategyStatus
        {
            None,       // board not changed
            Changed,    // board changed
            Failed      // board invalid
        }

        char Key { get; }

        StrategyStatus Apply(ISudokuBoard board);
    }
}
