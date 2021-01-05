using System;

namespace Mal
{
    public static class Repl
    {
        public static string Read(string s)
        {
            Console.WriteLine(s);
            return Console.ReadLine();
        }

        public static string Eval(string s) => s;

        public static string Print(string s) => s;

        public static string Loop(string s)
        {
            while (true)
            {
                Print(Eval(Read(s)));
            }
        }
    }
}