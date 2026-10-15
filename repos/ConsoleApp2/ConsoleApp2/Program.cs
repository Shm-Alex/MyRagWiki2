namespace ConsoleApp2
{
    internal class Program
    {
        static void Main(string[] args)
        {
            ReadOnlySpan<char> text = "Hello, World 5 !".AsSpan();
            ReadOnlySpan<char> hello = text.Slice(0, 5); // "Hello"
            ReadOnlySpan<char> five = text.Slice(13, 1); // "5"

            // Парсинг без выделения памяти
            if (int.TryParse(five, out int value))
            {
                Console.WriteLine(value);

            }
            Console.WriteLine("Hello, World!");
        }
    }
}
