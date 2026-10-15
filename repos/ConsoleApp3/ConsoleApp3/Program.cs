using System.Xml.Linq;

namespace ConsoleApp3
{
    interface ISomeInterface 
    { 
        public string getName { get; }
        protected string SetPropert(string PropertyName, string PropertyValue);

    }
    class InterfaceImplementation : ISomeInterface
    {
        private readonly string Name;
        Dictionary<string, string> Properties;
        public InterfaceImplementation(string name) 
        {
            this.Name = name;
        }

        public string getName => Name;

        public string SetPropert(string PropertyName, string PropertyValue)
        {
            Properties[PropertyName] = PropertyValue;
            return Properties[PropertyName];
        }
    }
    internal class Program
    {
        static void Main(string[] args)
        {
            ISomeInterface x = new InterfaceImplementation("Vasya");
            Console.WriteLine($@"Hello, World! x.{nameof(x.getName)} = {x.getName}");
        }
    }
}
