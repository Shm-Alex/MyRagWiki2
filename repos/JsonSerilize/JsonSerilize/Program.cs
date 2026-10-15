using Newtonsoft.Json;
namespace JsonSerilize
{
    
    public class divHandlerParameters
    {
        private double ?divided_;

        public required string Description { get; set; }
        public double Result { get; set; }
        public List<double> Divisors { get; set; }=new List<double>();
        
        public double Divided
        {
            get
            {
                if (divided_ == null)
                {

                    divided_ = Divisors.Aggregate(Result, (Accumulator, Divisor) => Accumulator * Divisor);
                }
                
                return divided_.Value;
            }

            set => divided_ = value;
        }
    }
    public class MyClassEception : Exception
    {
        public MyClassEception() : base("случилось  исключение  моего типа")
        { }
    }
    internal class Program
    {
        static void Main(string[] args)
        {
            Type x = typeof(MyClassEception);
            object XOB = Activator.CreateInstance(x);
            MyClassEception myClassEception = XOB as MyClassEception;
            throw myClassEception;

        }
    }
}
