using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace virtual_cube
{
    class MoveHelper
    {
        public static Move FindMove(String move)
        {
            return (Move)System.Enum.Parse(typeof(Move), move.Replace("'", "I"));
        }
    }
}
