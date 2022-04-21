using System;
using System.ComponentModel.DataAnnotations;

namespace TestDynamic
{
    public class Hello
    {
        public void Test([NotNull] string value1, [GreaterThan(6)] int value2)
        {
            Console.WriteLine($"{value1} {value2}");
        }

        public void Test2([NotNull] string value1, [GreaterThan(6)] int value2)
        {
            Console.WriteLine($"{value1} {value2}");
        }

        [StringLength(1)]
        public string Id { get; set; }

        [Range(minimum:4, maximum:18)]
        public int ShoeSize { get; set; }
    }
}
