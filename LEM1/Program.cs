using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LEM1
{
    class Program
    {
        static void Main(string[] args)
        {
            Stopwatch watch = new Stopwatch();
            watch.Start();
            DataTable data =FileOperation.ReadDataFile(@"C:\Users\Naresh\Desktop\test3.txt");
            Discretize discretize = new Discretize(data);
            data = discretize.Discretization();
            Rules rul = new Rules(data);
            rul.CheckInitialCondition();
            rul.ComputeSingleGlobalCovering();
            watch.Stop();

            Console.WriteLine($"Elapsed Time {watch.Elapsed}");
            Console.ReadLine();
        }
    }
}
