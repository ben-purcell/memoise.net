namespace Memoise.Tests.Support
{
    public interface IOutAndRefTestable
    {
        bool TrySomething(string s, out int a);
        bool TrySomethingElse(string s, ref int a);
    }
}