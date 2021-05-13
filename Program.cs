using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Security.Cryptography;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.DotNet.Writer;
using System.IO;

namespace LostMyMind
{
    class Program
    {
        private static Assembly app;
        private static ModuleDefMD asm;

        static void GetStrings()
        {
            
        }

        static void removeMindLatedStrings(MethodDef methods)
        {
            for (int x = 0; x < methods.Body.Instructions.Count; x++)
            {
                Instruction inst = methods.Body.Instructions[x];
                if (inst.OpCode.Equals(OpCodes.Ldstr))
                {
                    if (inst.Operand.ToString() == "MindLated.jpg")
                    {
                        methods.Body.Instructions.RemoveAt(x);
                        x--;
                    }
                }
            }
        }

        static void CleanCFlow(MethodDef methods)
        {
            for (int x = 0; x < methods.Body.Instructions.Count(); x++)
            {
                Instruction inst = methods.Body.Instructions[x];
                if (inst.OpCode.Equals(OpCodes.Br) || inst.OpCode.Equals(OpCodes.Br_S))
                {
                    try
                    {
                        if ((Instruction)inst.Operand == methods.Body.Instructions[x + 1])
                        {
                            methods.Body.Instructions.Remove(inst);
                            x--;
                            continue;
                        }
                    }
                    catch (Exception)
                    {
                    }
                }
            }
        }

        static void FixSizeOfs(MethodDef methods)
        {
            for (int x = 0; x < methods.Body.Instructions.Count(); x++)
            {
                Instruction inst = methods.Body.Instructions[x];
                if (inst.OpCode.Equals(OpCodes.Sizeof) && inst.Operand != null)
                {
                    switch (((TypeRef)inst.Operand).Name.ToLower())
                    {
                        case "boolean":
                            methods.Body.Instructions.Insert(x, new Instruction(OpCodes.Ldc_I4, sizeof(bool)));
                            methods.Body.Instructions.RemoveAt(x+1);
                            Console.WriteLine("fixed SizeOf! (bool)");
                            break;
                        case "single":
                            methods.Body.Instructions.Insert(x, new Instruction(OpCodes.Ldc_I4, sizeof(Single)));
                            methods.Body.Instructions.RemoveAt(x+1);
                            Console.WriteLine("fixed SizeOf! (Single)");
                            break;
                        default:
                            Console.WriteLine("unknown SizeOf! (" + ((TypeRef)inst.Operand).Name.ToLower() + ")");
                            break;
                    }
                }
            }
        }

        static void ConvertCallis(MethodDef methods)
        {
            for (int x = 0; x < methods.Body.Instructions.Count(); x++)
            {
                Instruction inst = methods.Body.Instructions[x];
                if (inst.OpCode.Equals(OpCodes.Ldftn) && methods.Body.Instructions[x+1].OpCode.Equals(OpCodes.Calli))
                {
                    inst.OpCode = OpCodes.Call;
                    methods.Body.Instructions[x + 1].OpCode = OpCodes.Nop;
                }
            }
        }

        #region Math-Based Functions
        static bool hasBasicMathCalculations(MethodDef methods)
        {
            for (int x = 0; x < methods.Body.Instructions.Count(); x++)
            {
                Instruction inst = methods.Body.Instructions[x];
                try
                {
                    Instruction newInst = null;
                    switch (inst.OpCode.ToString())
                    {
                        case "add":
                            newInst = new Instruction(OpCodes.Ldc_R8, Convert.ToDouble(methods.Body.Instructions[x - 2].Operand.ToString()) + Convert.ToDouble(methods.Body.Instructions[x - 1].Operand.ToString()));
                            break;
                        case "sub":
                            newInst = new Instruction(OpCodes.Ldc_R8, Convert.ToDouble(methods.Body.Instructions[x - 2].Operand.ToString()) - Convert.ToDouble(methods.Body.Instructions[x - 1].Operand.ToString()));
                            break;
                        case "mul":
                            newInst = new Instruction(OpCodes.Ldc_R8, Convert.ToDouble(methods.Body.Instructions[x - 2].Operand.ToString()) * Convert.ToDouble(methods.Body.Instructions[x - 1].Operand.ToString()));
                            break;
                        case "div":
                            newInst = new Instruction(OpCodes.Ldc_R8, Convert.ToDouble(methods.Body.Instructions[x - 2].Operand.ToString()) / Convert.ToDouble(methods.Body.Instructions[x - 1].Operand.ToString()));
                            break;
                        case "call":
                            if (inst.Operand.ToString().Contains("GetHINSTANCE")) { return false; }
                            continue;
                        default:
                            continue;
                    }
                    return true;
                }
                catch (Exception)
                {
                }
            }
            return false;
        }

        static void ComputeBasicMath(MethodDef methods)
        {
            for (int x = 0; x < methods.Body.Instructions.Count(); x++)
            {
                Instruction inst = methods.Body.Instructions[x];
                try
                {
                    Instruction newInst = null;
                    Console.Title = methods.DeclaringType.Name + "::" + methods.Name +" | " + x + " - " + methods.Body.Instructions.Count();
                    switch (inst.OpCode.ToString())
                    {
                        case "add":
                            newInst = new Instruction(OpCodes.Ldc_R8, Convert.ToDouble(methods.Body.Instructions[x - 2].Operand.ToString()) + Convert.ToDouble(methods.Body.Instructions[x - 1].Operand.ToString()));
                            break;
                        case "sub":
                            newInst = new Instruction(OpCodes.Ldc_R8, Convert.ToDouble(methods.Body.Instructions[x - 2].Operand.ToString()) - Convert.ToDouble(methods.Body.Instructions[x - 1].Operand.ToString()));
                            break;
                        case "mul":
                            newInst = new Instruction(OpCodes.Ldc_R8, Convert.ToDouble(methods.Body.Instructions[x - 2].Operand.ToString()) * Convert.ToDouble(methods.Body.Instructions[x - 1].Operand.ToString()));
                            break;
                        case "div":
                            newInst = new Instruction(OpCodes.Ldc_R8, Convert.ToDouble(methods.Body.Instructions[x - 2].Operand.ToString()) / Convert.ToDouble(methods.Body.Instructions[x - 1].Operand.ToString()));
                            break;
                        case "call":
                            if (inst.Operand.ToString().Contains("GetHINSTANCE")) { return; }
                            continue;
                        default:
                            continue;
                    }
                    if (x + 1 < methods.Body.Instructions.Count())
                    {
                        if (methods.Body.Instructions[x + 1].OpCode.Equals(OpCodes.Call))
                        {
                            if (((MemberRef)methods.Body.Instructions[x + 1].Operand).Name == "ToInt32")
                            {
                                newInst = new Instruction(OpCodes.Ldc_I4, Convert.ToInt32(newInst.Operand.ToString()));
                                methods.Body.Instructions.RemoveAt(x + 1);
                            }
                        }
                    }
                    methods.Body.Instructions[x - 2] = newInst;
                    methods.Body.Instructions.RemoveAt(x - 1);
                    methods.Body.Instructions.RemoveAt(x - 1);
                    x -= 2;
                    Console.WriteLine("fixed basic math!");
                }
                catch (Exception)
                {
                }
            }
        }
        static void ComputeCalledEquations(MethodDef methods)
        {
            for (int x = 0; x < methods.Body.Instructions.Count(); x++)
            {
                Instruction inst = methods.Body.Instructions[x];
                if (inst.OpCode.Equals(OpCodes.Ldftn) || inst.OpCode.Equals(OpCodes.Call))
                {
                    try
                    {
                        if (inst.Operand.ToString().Contains("System.Math") && methods.Body.Instructions[x - 1].OpCode.Equals(OpCodes.Ldc_R8))
                        {
                            switch (((MemberRef)inst.Operand).Name.ToLower())
                            {
                                case "cos":
                                    methods.Body.Instructions[x - 1].Operand = Math.Cos(Convert.ToDouble(methods.Body.Instructions[x - 1].Operand.ToString()));
                                    break;
                                case "tan":
                                    methods.Body.Instructions[x - 1].Operand = Math.Tan(Convert.ToDouble(methods.Body.Instructions[x - 1].Operand.ToString()));
                                    break;
                                case "sin":
                                    methods.Body.Instructions[x - 1].Operand = Math.Sin(Convert.ToDouble(methods.Body.Instructions[x - 1].Operand.ToString()));
                                    break;
                                case "log":
                                    methods.Body.Instructions[x - 1].Operand = Math.Log(Convert.ToDouble(methods.Body.Instructions[x - 1].Operand.ToString()));
                                    break;
                                case "log10":
                                    methods.Body.Instructions[x - 1].Operand = Math.Log10(Convert.ToDouble(methods.Body.Instructions[x - 1].Operand.ToString()));
                                    break;
                                case "ceiling":
                                    methods.Body.Instructions[x - 1].Operand = Math.Ceiling(Convert.ToDouble(methods.Body.Instructions[x - 1].Operand.ToString()));
                                    break;
                                case "sqrt":
                                    methods.Body.Instructions[x - 1].Operand = Math.Sqrt(Convert.ToDouble(methods.Body.Instructions[x - 1].Operand.ToString()));
                                    break;
                                case "round":
                                    methods.Body.Instructions[x - 1].Operand = Math.Round(Convert.ToDouble(methods.Body.Instructions[x - 1].Operand.ToString()));
                                    break;
                                case "abs":
                                    methods.Body.Instructions[x - 1].Operand = Math.Abs(Convert.ToDouble(methods.Body.Instructions[x - 1].Operand.ToString()));
                                    break;
                                case "tanh":
                                    methods.Body.Instructions[x - 1].Operand = Math.Tanh(Convert.ToDouble(methods.Body.Instructions[x - 1].Operand.ToString()));
                                    break;
                                case "truncate":
                                    methods.Body.Instructions[x - 1].Operand = Math.Truncate(Convert.ToDouble(methods.Body.Instructions[x - 1].Operand.ToString()));
                                    break;
                                case "floor":
                                    methods.Body.Instructions[x - 1].Operand = Math.Floor(Convert.ToDouble(methods.Body.Instructions[x - 1].Operand.ToString()));
                                    break;
                                default:
                                    Console.WriteLine("unknown equation name! (" + ((MemberRef)inst.Operand).Name.ToLower() + ")");
                                    continue;
                            }
                            Console.WriteLine("fixed equation! (" + ((MemberRef)inst.Operand).Name.ToLower() + ")");
                            methods.Body.Instructions.Remove(inst);
                            x--;
                        }
                    }
                    catch (Exception)
                    {
                    }
                }
            }
        }
        #endregion

        #region Filter useless nops (from DevT02's Junk Remover)
        static void RemoveUselessNops()
        {
            foreach (var type in asm.Types.Where(t => t.HasMethods))
            {
                foreach (var method in type.Methods.Where(m => m.HasBody && m.Body.HasInstructions))
                {
                    if (method.HasBody)
                    {
                        var instr = method.Body.Instructions;
                        for (int i = 0; i < instr.Count; i++)
                        {
                            if (instr[i].OpCode == OpCodes.Nop &&
                                !IsNopBranchTarget(method, instr[i]) &&
                                !IsNopSwitchTarget(method, instr[i]) &&
                                !IsNopExceptionHandlerTarget(method, instr[i]))
                            {
                                instr.RemoveAt(i);
                                Console.WriteLine("removed nop!");
                                nopRemoved++;
                                i--;
                            }
                        }
                    }
                }
            }
        }

        // Junk Remover Functions
        private static bool IsNopBranchTarget(MethodDef method, Instruction nopInstr)
        {
            var instr = method.Body.Instructions;
            for (int i = 0; i < instr.Count; i++)
            {
                if (instr[i].OpCode.OperandType == OperandType.InlineBrTarget || instr[i].OpCode.OperandType == OperandType.ShortInlineBrTarget && instr[i].Operand != null)
                {
                    Instruction instruction2 = (Instruction)instr[i].Operand;
                    if (instruction2 == nopInstr)
                        return true;
                }
            }
            return false;
        }
        private static bool IsNopSwitchTarget(MethodDef method, Instruction nopInstr)
        {
            var instr = method.Body.Instructions;
            for (int i = 0; i < instr.Count; i++)
            {
                if (instr[i].OpCode.OperandType == OperandType.InlineSwitch && instr[i].Operand != null)
                {
                    Instruction[] source = (Instruction[])instr[i].Operand;
                    if (source.Contains(nopInstr))
                        return true;
                }
            }
            return false;
        }
        private static bool IsNopExceptionHandlerTarget(MethodDef method, Instruction nopInstr)
        {
            bool result;
            if (!method.Body.HasExceptionHandlers)
                result = false;
            else
            {
                var exceptionHandlers = method.Body.ExceptionHandlers;
                foreach (var exceptionHandler in exceptionHandlers)
                {
                    if (exceptionHandler.FilterStart == nopInstr ||
                        exceptionHandler.HandlerEnd == nopInstr ||
                        exceptionHandler.HandlerStart == nopInstr ||
                        exceptionHandler.TryEnd == nopInstr ||
                        exceptionHandler.TryStart == nopInstr)
                        return true;
                }
                result = false;
            }
            return result;
        }
        // End of Junk Remover Functions
        #endregion

        static void FixMethod()
        {
            foreach (TypeDef t_ in asm.Types)
            {
                if (!t_.HasMethods) { continue; }
                foreach (MethodDef methods in t_.Methods)
                {
                    if (methods.HasImplMap) { continue; }
                    if (!methods.HasBody) { continue; }
                    methods.Body.Instructions.RemoveAt(methods.Body.Instructions.Count - 1);
                    methods.Body.Instructions.RemoveAt(methods.Body.Instructions.Count - 1);
                    methods.Body.Instructions.RemoveAt(methods.Body.Instructions.Count - 1);
                    methods.Body.Instructions.RemoveAt(methods.Body.Instructions.Count - 1);
                    for (int x = 0; x < methods.Body.Instructions.Count; x++)
                    {
                        methods.Body.Instructions.RemoveAt(0);
                        if (methods.Body.Instructions[0].OpCode.Equals(OpCodes.Sizeof))
                        {
                            Instruction[] instr_arr = new Instruction[methods.Body.Instructions.Count];
                            for (int x_ = 1; x_ < methods.Body.Instructions.Count; x_++)
                            {
                                instr_arr[x_-1] = methods.Body.Instructions[x_];
                            }
                            methods.Body.Instructions.Clear();
                            foreach (Instruction inst_ in instr_arr)
                            {
                                if (inst_ != null)
                                {
                                    methods.Body.Instructions.Add(inst_);
                                }
                            }
                            break;
                        }
                    }
                    //RemoveUselessNops();
                    removeMindLatedStrings(methods);
                    CleanCFlow(methods);
                    FixSizeOfs(methods);
                    ConvertCallis(methods);
                    ComputeBasicMath(methods);
                    ComputeCalledEquations(methods);
                    //while (hasBasicMathCalculations(methods))
                    //{
                        //RemoveUselessNops();
                        ComputeBasicMath(methods); // recalculated because called equation might have created new operations
                    //}
                    if (methods.Body.HasExceptionHandlers)
                    {
                        methods.Body.ExceptionHandlers.RemoveAt(0);
                    }
                    methods.Body.KeepOldMaxStack = true;
                }
            }
        }
        static int nopRemoved = 0;
        static void Main(string[] args)
        {
            asm = ModuleDefMD.Load(args[0]);
            FixMethod();
            ModuleWriterOptions moduleWriterOptions = new ModuleWriterOptions(asm);
            moduleWriterOptions.MetadataOptions.Flags |= MetadataFlags.PreserveAll;
            moduleWriterOptions.Logger = DummyLogger.NoThrowInstance;
            NativeModuleWriterOptions nativeModuleWriterOptions = new NativeModuleWriterOptions(asm, true);
            nativeModuleWriterOptions.MetadataOptions.Flags |= MetadataFlags.PreserveAll;
            nativeModuleWriterOptions.Logger = DummyLogger.NoThrowInstance;
            if (asm.IsILOnly) { asm.Write(Path.GetFileNameWithoutExtension(args[0]) + "-LostMyMind" + Path.GetExtension(args[0]), moduleWriterOptions); } else { asm.NativeWrite(Path.GetFileNameWithoutExtension(args[0]) + "-LostMyMind" + Path.GetExtension(args[0])); }
            Console.WriteLine("done (" +nopRemoved + ")");
            Console.ReadKey();
        }
    }
}
