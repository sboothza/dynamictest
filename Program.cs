using System;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using HarmonyLib;

namespace TestDynamic
{
    public class HelloWorldClass
    {
        public void Main()
        {
            Console.WriteLine($"Hello Main() - {ProductName}");
        }

        public string ProductName { get; set; }
    }

    public class Replacer
    {
        public static bool OtherMethod(HelloWorldClass __instance)
        {
            Console.WriteLine($"this is a replacement method - {__instance.ProductName}");
            return false;
        }
    }


    class Program
    {
        static void Main(string[] args)
        {
            var harmony = new Harmony(Guid.NewGuid()
                                          .ToString());

            var mainMeth = typeof(HelloWorldClass).GetMethod("Main");
            var notMainMeth = typeof(Replacer).GetMethod("OtherMethod");

            Console.WriteLine("output should be hello main");
            var hello = new HelloWorldClass
            {
                ProductName = "hello silly people"
            };

            hello.Main();

            Console.WriteLine("injecting replacer in place of main");
            harmony.Patch(mainMeth, new HarmonyMethod(notMainMeth));

            Console.WriteLine("output should be replaced main");
            hello.Main();
        }
    }
}
