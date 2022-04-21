using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using OpCodes = System.Reflection.Emit.OpCodes;

namespace TestDynamic
{
    public class HelloWorld
    {
        public void Test(string value1, int value2)
        {
            Console.WriteLine($"{value1} {value2}");
        }
    }

    public static class ValidatePatcher
    {
        private static IEnumerable<CodeInstruction> Transpiler(ILGenerator generator, MethodBase originalMethod, IEnumerable<CodeInstruction> instructions)
        {
            var okLabel = generator.DefineLabel();
            yield return new CodeInstruction(OpCodes.Ldarg, 1);
            yield return new CodeInstruction(OpCodes.Ldnull);
            yield return new CodeInstruction(OpCodes.Ceq);
            yield return new CodeInstruction(OpCodes.Brfalse, okLabel);
            yield return new CodeInstruction(OpCodes.Ldstr, "value1 cannot be null");
            var exCtor = AccessTools.Constructor(typeof(ValidationException), new[] { typeof(string) });
            yield return new CodeInstruction(OpCodes.Newobj, exCtor);
            yield return new CodeInstruction(OpCodes.Throw);
            var blankLabelInstruction = new CodeInstruction(OpCodes.Nop);
            blankLabelInstruction.labels.Add(okLabel);
            yield return blankLabelInstruction;

            foreach (var instruction in instructions)
                yield return instruction;
        }
    }

    internal class Program
    {
        private static Harmony _harmony;
        private const BindingFlags AllMethods = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static;

        private static void Main(string[] args)
        {
            _harmony = new Harmony(Guid.NewGuid()
                                       .ToString());

            TestTrans();
        }

        private static void TestTrans()
        {
            var testMeth = typeof(HelloWorld).GetMethod("Test");

            var hello = new HelloWorld();

            hello.Test("t1", 2);
            hello.Test(null, 4);
            var trans = typeof(ValidatePatcher).GetMethod("Transpiler", AllMethods);
            _harmony.Patch(testMeth, null, null, new HarmonyMethod(trans));
            Console.WriteLine("should work");
            hello.Test("tim", 0);
            Console.WriteLine("should fail");

            try
            {
                hello.Test(null, 0);
            }
            catch (Exception ex)
            {
                Console.WriteLine("test"+ex.Message);
            }
        }
    }
}
