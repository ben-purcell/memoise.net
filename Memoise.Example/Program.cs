using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Memoise.Example
{
    class Program
    {
        private static void Main(string[] args)
        {
            var something = new Something();
            var memoised = MemoiseFactory.Create<ISomething>(something);
            Console.WriteLine(memoised.Foo("arg"));
            Console.WriteLine(memoised.Foo("arg"));

            var result2 = memoised.Foo(1, "blah");
            Console.WriteLine(result2.Id);
            Console.WriteLine(result2.Name);

            SomeResult result3;
            Console.WriteLine(memoised.Foo(1, "blah", out result3));
            Console.WriteLine(result3.Id);
            Console.WriteLine(result3.Name);

            SomeResult result4;
            Console.WriteLine(memoised.Foo(1, "blah", out result4));
            Console.WriteLine(result4.Id);
            Console.WriteLine(result4.Name);
        }
    }
}
