using System;
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

        public static MalType Eval(MalType obj) => obj;

        public static void Print(MalType s)
        {
            Console.WriteLine(s.AsLiteral());
        }

        public static void Loop(string prompt)
        {
            while (true)
            {
                var input = Read(prompt);
                if (input is null)
                {
                    break;
                }

                MalType obj;
                try
                {
                    obj = ReaderStatic.ReadString(input);
                }
                catch(EndOfStreamException)
                {
                    Console.WriteLine("EOF");
                    continue;
                }

                Print(Eval(obj));
            }
        }
    }
}