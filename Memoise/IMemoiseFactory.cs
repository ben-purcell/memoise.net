namespace Memoise
{
    public interface IMemoiseFactory
    {
        TInterface CreateMemoised<TInterface>(TInterface instance);
    }
}
