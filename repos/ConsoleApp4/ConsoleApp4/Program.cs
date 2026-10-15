namespace ConsoleApp4
{
    internal class Program
    {
        static void Main(string[] args)
        {
            int keyIndex = 10;
            int [] Values = { 1, 2, 3 };
            Console.WriteLine($@"начение параметра keyIndex {keyIndex}не попадает в заданный полуинтервал [0:{Values.Length})");
        }
    }
}
