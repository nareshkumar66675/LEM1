﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LEM1
{
    class Program
    {
        static void Main(string[] args)
        {
            DataTable data =FileOperation.ReadDataFile(@"C:\Users\Naresh\Desktop\test1.txt");

            Rules rul = new Rules(data);
            rul.CheckInitialCondition();
            rul.ComputeSingleGlobalCovering();
        }
    }
}
