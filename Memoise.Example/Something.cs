namespace Memoise.Example
{
    public class Something : ISomething
    {
        private static int counter = -1;

        public int Foo(string arg)
        {
            counter++;
            return counter;
        }

        public SomeResult Foo(int id, string param)
        {
            return new SomeResult { Id = id, Name = param };
        }

        public bool Foo(int id, string param, out SomeResult result)
        {
            counter++;
            result = new SomeResult { Id = counter, Name = "blah" + counter };
            return false;
        }
    }
}