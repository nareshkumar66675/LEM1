using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
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

            Console.WriteLine("LEM1 Implementation");
            Console.WriteLine("Enter the File Name/Path of the Input File");
            string ipFile = Console.ReadLine();

            while(!File.Exists(ipFile))
            {
                Console.WriteLine(" File {0} does not exist. Please Enter again or type EXIT to exit the program", ipFile);
                ipFile = Console.ReadLine();
                if (ipFile.ToUpper() == "EXIT")
                    Environment.Exit(1);
            }
            Console.WriteLine("Enter Output file name");
            string opFile = Console.ReadLine();

            Console.WriteLine("Computing Rules......Please Wait");

            watch.Start();
            DataTable data =FileOperation.ReadDataFile(ipFile);
            Discretize discretize = new Discretize(data);
            data = discretize.Discretization();
            Rules rul = new Rules(data);
            bool isConsistent= rul.CheckInitialCondition();
            rul.ComputeSingleGlobalCovering();
            string directory = Path.GetDirectoryName(ipFile);
            if (string.IsNullOrEmpty(directory))
                directory = Directory.GetCurrentDirectory();
            string fileName = Path.GetFileNameWithoutExtension(opFile);
            var certain = rul.GetRuleSet(RuleType.Certain);

            if (File.Exists(Path.Combine(directory, fileName + ".certain.r")))
                File.Delete(Path.Combine(directory, fileName + ".certain.r"));
            File.AppendAllText(Path.Combine(directory, fileName + ".certain.r"), certain);

            if (File.Exists(Path.Combine(directory, fileName + ".possible.r")))
                File.Delete(Path.Combine(directory, fileName + ".possible.r"));
            var possible = string.Empty;
            if (isConsistent == false)
                possible = rul.GetRuleSet(RuleType.Possible);
            else
                possible = "! Possible Rule set is not shown since it is identical with the certain rule set.";
            File.AppendAllText(Path.Combine(directory, fileName + ".possible.r"), possible);

            watch.Stop();

            Console.WriteLine("Output File created in " + directory);
            Console.WriteLine("Process Completed. Press any key to Exit.");
            Console.WriteLine($"Elapsed Time {watch.Elapsed}");
            Console.ReadLine();
        }
    }
}
