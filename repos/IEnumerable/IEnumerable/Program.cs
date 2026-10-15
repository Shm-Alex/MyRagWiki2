namespace IEnumerable
{
    internal class Program
    {
        public class SomeClass
        {
            public int X { get; set; }
            public int Y { get; set; }
            public int Z { get; set; }

        }
        static SomeClass[] someClasses = [
            new SomeClass() { X=1,Y=1,Z=1} ,
            new SomeClass() { X=2,Y=2,Z=2} ,
            new SomeClass() { X=4,Y=4,Z=3} ,
            new SomeClass() { X=3,Y=3,Z=4} ,
        ];
        static SomeClass[] someAditionalClasses = [
            new SomeClass() { X=7,Y=0,Z=-1} ,
            new SomeClass() { X=8,Y=7,Z=-2} ,
            new SomeClass() { X=9,Y=5,Z=-3} ,
            new SomeClass() { X=6,Y=4,Z=-4} ,
        ];
        //static int someF(IEnumerable<SomeClass> list)
        //=> list.Aggregate(0, agregationF);
        //static int agregationF(int acccumulator,SomeClass c)=> acccumulator+c.X+c.Z-c.Y;
        static void Main(string[] args)
        {
            var union = someClasses.Union(someAditionalClasses);
            //Console.WriteLine(@$"Hello, World!{someF(someClasses.Select(x=>x))}");

        }
    }
}
