using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sudoku.Core.Extensions
{
    public static class BitArrayExtensions
    {
        public static bool AreAllSet(this BitArray bits)
        {
            foreach (bool bit in bits)
            {
                if (!bit) return false;
            }
            return true;
        }

        public static int GetCount(this BitArray bits)
        {
            int count = 0;
            foreach (bool bit in bits)
            {
                if (bit) count++;
            }
            return count;
        }
    }
}
