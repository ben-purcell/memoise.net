namespace Memoise.Example
{
    public class SomeResult
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public override string ToString()
        {
            return string.Format("{0} - {1}", Id, Name);
        }
    }
}