namespace Memoise.Tests.Support
{
    public interface ITestable
    {
        void Blah(string str);
        int MethodA(int a);
        int MethodB(int a, int b);
        int MethodC(int a, int b, int c);
    }
}