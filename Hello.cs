using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestDynamic
{
    public class Hello
    {
        public void Test([NotNull] string value1, [GreaterThan(6)] int value2)
        {
            Console.WriteLine($"{value1} {value2}");
        }
    }
}
