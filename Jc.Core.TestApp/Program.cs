﻿using Jc.Core.Helper;
using Jc.Core.TestApp.Test;
using System;

namespace Jc.Core.TestApp
{
    class Program
    {
        static void Main(string[] args)
        {
            NetShareHelperTest test = new NetShareHelperTest();
            test.Test();

            Console.WriteLine("测试完成,请按任意键继续.");
            Console.ReadKey();
        }
    }
}