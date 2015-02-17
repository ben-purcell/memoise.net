namespace Memoise.Example
{
    public interface ISomething
    {
        int Foo(string arg);
        SomeResult Foo(int id, string param);
        bool Foo(int id, string param, out SomeResult result);
    }
}