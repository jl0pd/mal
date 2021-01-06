using System;
using System.Collections.Generic;
using System.IO;

namespace Mal
{
    public static class Repl
    {
        public static string? Read(string s)
        {
            Console.Write(s);
            return Console.ReadLine();
        }

        public static (MalType, ReplEnvironment) Eval(MalType obj, ReplEnvironment env) => Evaluator.Eval(obj, env);

        public static void Print(MalType s) => Console.WriteLine(s.AsLiteral());

        public static void Loop(string prompt)
        {
            var env = new ReplEnvironment();
            while (true)
            {
                string? input = Read(prompt);
                if (input is null)
                {
                    break;
                }

                MalType obj;
                try
                {
                    obj = ReaderStatic.ReadString(input);
                }
                catch (EndOfStreamException)
                {
                    Console.WriteLine("EOF");
                    continue;
                }

                try
                {
                    (obj, env) = Eval(obj, env);
                }
                catch (KeyNotFoundException)
                {
                    Console.WriteLine($"Variable not found");
                    continue;
                }

                Print(obj);
            }
        }
    }
}