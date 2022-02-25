using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Mono.Cecil.Cil;
using OpCode = System.Reflection.Emit.OpCode;
using OpCodes = System.Reflection.Emit.OpCodes;

namespace TestDynamic
{
    public class HelloWorldClass
    {
        public void Main()
        {
            Console.WriteLine($"Hello Main() - {ProductName}");
        }

        public string ProductName { get; set; }

        public void Test(string value1, int value2)
        {
            if (value1 == null)
                throw new ValidationException("value1 cannot be null");
            Console.WriteLine($"{value1} {value2}");
        }
    }

    public class Replacer
    {
        public static bool OtherMethod(HelloWorldClass __instance)
        {
            Console.WriteLine($"this is a replacement method - {__instance.ProductName}");
            return false;
        }

        public static bool Validate_0(object __0)
        {
            if (__0 == null)
                throw new ArgumentNullException();

            return true;
        }

        public static bool Validate_1(object __1)
        {
            return (__1 != null);
        }

        public static bool Validate_2(object __2)
        {
            return (__2 != null);
        }

        public static bool Hello_Test_Validate1(string value1, int value2)
        {
            if (value1 == null)
                throw new ValidationException("value1 cannot be null");
            if (value2 <= 6)
                throw new ValidationException("value2 must be greater that 6");

            return true;
        }

        public static void CheckILValidate(string value1, int value2)
        {
            if (value1 == null)
                throw new ValidationException("value1 cannot be null");
            if (value2 <= 6)
                throw new ValidationException("value2 must be greater that 6");

            Console.WriteLine("test");
        }

        public void CheckILValidateInstance(string value1, int value2)
        {
            if (value1 == null)
                throw new ValidationException("value1 cannot be null");
            if (value2 <= 6)
                throw new ValidationException("value2 must be greater that 6");

            Console.WriteLine("test");
        }

        public static void CheckILNoValidate(string value1, int value2)
        {
            Console.WriteLine(value1 + value2);
        }

        public void CheckILNoValidateInstance(string value1, int value2)
        {
            Console.WriteLine(value1 + value2);
        }

        // static IEnumerable<CodeInstruction> AddNullCheckTranspiler(IEnumerable<CodeInstruction> instructions, string name, int index)
        // {
        //     var proc = new PatchProcessor()
        //     ILGenerator gen = new ILGenerator();
        //     var newInstructions = new List<CodeInstruction>
        //                           {
        //                               new la
        //                           }
        //     
        //     /*
        //      * var okLabel = il.DefineLabel();
        //     il.Emit(OpCodes.Ldarg, 0);
        //     il.Emit(OpCodes.Brtrue, okLabel);
        //     il.Emit(OpCodes.Ldstr, "value1 cannot be null");
        //     var exCtor = AccessTools.Constructor(typeof(ValidationException), new[] { typeof(string) });
        //     il.Emit(OpCodes.Newobj, exCtor);
        //     il.Emit(OpCodes.Throw);
        //     il.MarkLabel(okLabel);
        //      */
        // }
    }

    //[HarmonyPatch(typeof(HelloWorldClass))]
    //[HarmonyPatch("Test")]
    public static class ValidatePatcher
    {
        static IEnumerable<CodeInstruction> Transpiler(ILGenerator generator, MethodBase originalMethod, IEnumerable<CodeInstruction> instructions)
        {
            var validateInstructions = new List<CodeInstruction>();
            
            var okLabel = generator.DefineLabel();
            validateInstructions.Add(new CodeInstruction(OpCodes.Ldarg, 1));
            validateInstructions.Add(new CodeInstruction(OpCodes.Ldnull));
            validateInstructions.Add(new CodeInstruction(OpCodes.Ceq));
            validateInstructions.Add(new CodeInstruction(OpCodes.Brfalse, okLabel));
            validateInstructions.Add(new CodeInstruction(OpCodes.Ldstr, "value1 cannot be null"));
            var exCtor = AccessTools.Constructor(typeof(ValidationException), new[] { typeof(string) });
            validateInstructions.Add(new CodeInstruction(OpCodes.Newobj, exCtor));
            validateInstructions.Add(new CodeInstruction(OpCodes.Throw));
            CodeInstruction blankLabelInstruction = new CodeInstruction(OpCodes.Nop);
            blankLabelInstruction.labels.Add(okLabel);
            validateInstructions.Add(blankLabelInstruction);
        
            validateInstructions.AddRange(instructions);
            return validateInstructions;
        }

        // static IEnumerable<CodeInstruction> Transpiler(ILGenerator generator, MethodBase originalMethod, IEnumerable<CodeInstruction> instructions)
        // {
        //     var validateInstructions = new List<CodeInstruction>();
        //
        //     //var okLabel = generator.DefineLabel();
        //     //validateInstructions.Add(new CodeInstruction(OpCodes.Ldarg, 0));
        //     //validateInstructions.Add(new CodeInstruction(OpCodes.Brtrue, okLabel));
        //     validateInstructions.Add(new CodeInstruction(OpCodes.Ldstr, "value1 cannot be null"));
        //     validateInstructions.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Console), "WriteLine", new[] { typeof(string) })));
        //     //var exCtor = AccessTools.Constructor(typeof(ValidationException), new[] { typeof(string) });
        //     //validateInstructions.Add(new CodeInstruction(OpCodes.Newobj, exCtor));
        //     //validateInstructions.Add(new CodeInstruction(OpCodes.Throw));
        //     //CodeInstruction blankLabelInstruction = new CodeInstruction(OpCodes.Nop);
        //     //blankLabelInstruction.labels.Add(okLabel);
        //     //validateInstructions.Add(blankLabelInstruction);
        //
        //     validateInstructions.AddRange(instructions);
        //     return validateInstructions;
        // }
    }


    class Program
    {
        static Harmony _harmony;
        private static BindingFlags _all = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static;

        static void Main(string[] args)
        {
            //test_il2();


            _harmony = new Harmony(Guid.NewGuid()
                                       .ToString());

            TestTrans();
            return;

            //Test();

            // var methods = AppDomain.CurrentDomain
            //                        .GetAssemblies()
            //                        .SelectMany(a => a.GetTypes())
            //                        .SelectMany(t => t.GetMethods(_all))
            //                        .Where(m => m.GetParameters()
            //                                     .SelectMany(p => p.GetCustomAttributes<ValidateAttribute>())
            //                                     .Any());

            //var validators = new[] { new HarmonyMethod(typeof(Replacer).GetMethod("Validate_0")), new HarmonyMethod(typeof(Replacer).GetMethod("Validate_1")), new HarmonyMethod(typeof(Replacer).GetMethod("Validate_2")) }.ToList();

            // var code = @"using System; public class HelloWorldClassNew { public static void Main() { Console.WriteLine(""Hello Main() from new""); } public string ProductName { get; set; } public int ProductCount { get; set; } }";
            // var asm = Dynamic.BuildFromCode(code);
            // var newType = asm.GetType("HelloWorldClassNew");
            // var notMainMeth = newType.GetMethod("Main");

            var signatures = new Validations("Validator");

            // foreach (var method in methods)
            // {
            //     var parameters = method.GetParameters().ToList();
            //     var signature = new ValidationSignature($"{method.DeclaringType.Name}_{method.Name}_Validate");
            //     signature.OriginalMethod = method;
            //     signatures.Add(signature);
            //
            //     foreach (var parameter in parameters)
            //     {
            //         var parm = new ParameterInfo(parameter.Name, parameter.ParameterType);
            //         parm.Validations.AddRange(parameter.GetCustomAttributes<ValidateAttribute>());
            //     }
            // }

            // var code = signatures.Generate();
            // var asm = Dynamic.BuildFromCode(code);

            // var newType = asm.GetType("HelloWorldClassNew");
            // var notMainMeth = newType.GetMethod("Main");

            var hello = new Hello();
            hello.Test("v1", 5);

            signatures.Init(_harmony);

            hello.Test("v1", 5);

        }

        private delegate void HelloDelegate(string value1, int value2);

        private static void test_il()
        {
            DynamicMethod hello = new DynamicMethod("Hello", null, new[] { typeof(string), typeof(int) });
            ILGenerator il = hello.GetILGenerator(256);
            il.Emit(OpCodes.Nop);
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarga_S, 1);
            il.EmitCall(OpCodes.Call, AccessTools.Method(typeof(int), "ToString"), null);
            il.EmitCall(OpCodes.Call, AccessTools.Method(typeof(string), "Concat", new[] { typeof(string), typeof(string) }), null);
            il.EmitCall(OpCodes.Call, AccessTools.Method(typeof(Console), "WriteLine", new[] { typeof(string) }), null);
            il.Emit(OpCodes.Nop);
            il.Emit(OpCodes.Ret);

            var hi = (HelloDelegate)hello.CreateDelegate(typeof(HelloDelegate));

            hi("t1", 3);

            /*
             * IL_0000: nop
	IL_0001: ldarg.0
	IL_0002: ldarga.s  value2
	IL_0004: call      instance string [System.Runtime]System.Int32::ToString()
	IL_0009: call      string [System.Runtime]System.String::Concat(string, string)
	IL_000E: call      void [System.Console]System.Console::WriteLine(string)
	IL_0013: nop
	IL_0014: ret
             */
        }

        private static void test_il2()
        {
            DynamicMethod hello = new DynamicMethod("Hello", null, new[] { typeof(string), typeof(int) });
            ILGenerator il = hello.GetILGenerator(256);

            var okLabel = il.DefineLabel();
            il.Emit(OpCodes.Ldarg, 0);
            il.Emit(OpCodes.Brtrue, okLabel);
            il.Emit(OpCodes.Ldstr, "value1 cannot be null");
            var exCtor = AccessTools.Constructor(typeof(ValidationException), new[] { typeof(string) });
            il.Emit(OpCodes.Newobj, exCtor);
            il.Emit(OpCodes.Throw);
            il.MarkLabel(okLabel);

            ////
            il.Emit(OpCodes.Nop);
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarga_S, 1);
            il.EmitCall(OpCodes.Call, AccessTools.Method(typeof(int), "ToString"), null);
            il.EmitCall(OpCodes.Call, AccessTools.Method(typeof(string), "Concat", new[] { typeof(string), typeof(string) }), null);
            il.EmitCall(OpCodes.Call, AccessTools.Method(typeof(Console), "WriteLine", new[] { typeof(string) }), null);
            il.Emit(OpCodes.Nop);
            il.Emit(OpCodes.Ret);

            var hi = (HelloDelegate)hello.CreateDelegate(typeof(HelloDelegate));

            hi("555-", 3);
            hi(null, 3);

            /*
            IL_0000: nop
	IL_0001: ldarg.0
	IL_0002: ldnull
	IL_0003: ceq
	IL_0005: stloc.0
	IL_0006: ldloc.0
	IL_0007: brfalse.s IL_0014

	IL_0009: ldstr     "value1 cannot be null"
	IL_000E: newobj    instance void TestDynamic.ValidationException::.ctor(string)
	IL_0013: throw

             * IL_0000: nop
	IL_0001: ldarg.0
	IL_0002: ldarga.s  value2
	IL_0004: call      instance string [System.Runtime]System.Int32::ToString()
	IL_0009: call      string [System.Runtime]System.String::Concat(string, string)
	IL_000E: call      void [System.Console]System.Console::WriteLine(string)
	IL_0013: nop
	IL_0014: ret
             */
        }

        //
        // foreach (var attrib in validateAttributes)
        // {
        //     var name = attrib.ParameterName;
        //     var parameter = parameters.FirstOrDefault(p => p.Name.ToLowerInvariant() == name.ToLowerInvariant());
        //     if (parameter != null)
        //     {
        //         var meth = validators[parameter.Position];
        //         _harmony.Patch(method, meth);
        //     }
        // }

        private static void Test()
        {
            var mainMeth = typeof(HelloWorldClass).GetMethod("Main");
            var notMainMeth = typeof(Replacer).GetMethod("OtherMethod");

            Console.WriteLine("output should be hello main");
            var hello = new HelloWorldClass
            {
                ProductName = "hello silly people"
            };

            hello.Main();

            Console.WriteLine("injecting replacer in place of main");
            _harmony.Patch(mainMeth, new HarmonyMethod(notMainMeth));

            Console.WriteLine("output should be replaced main");
            hello.Main();
        }

        private static void TestTrans()
        {
            var testMeth = typeof(HelloWorldClass).GetMethod("Test");

            var hello = new HelloWorldClass
            {
                ProductName = "hello silly people"
            };

            hello.Test("t1", 2);
            var trans = typeof(ValidatePatcher).GetMethod("Transpiler", _all);

            _harmony.Patch(testMeth, null, null, new HarmonyMethod(trans));
            //_harmony.PatchAll();


            Console.WriteLine("output should be replaced main");
            hello.Test("tim", 0);
            hello.Test(null, 0);
        }
    }
}
