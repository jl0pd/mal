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

        public static EvalContext<MalType> Eval(EvalContext<MalType> context)
            => Evaluator.Eval(context);

        public static void Print(MalType s)
            => Console.WriteLine(s.AsLiteral());

        public static void Loop(string prompt)
        {
            var context = EvalContext.Create((MalType)MalType.CreateNil(), new ReplEnvironment());
            while (true)
            {
                string? input = Read(prompt);
                if (input is null)
                {
                    break;
                }

                try
                {
                    context = context.WithObj(Reader.ReadString(input));
                }
                catch (EndOfStreamException)
                {
                    Console.WriteLine("EOF");
                    continue;
                }

                try
                {
                    context = Eval(context);
                }
                catch (KeyNotFoundException ex)
                {
                    Console.WriteLine($"'{ex.Message}' not found");
                    continue;
                }

                Print(context.Obj);
            }
        }
    }
}