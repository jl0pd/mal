using System;
using System.Linq;

namespace Mal
{
    internal static class Program
    {
        static void Main(string[] args)
        {
            var prompt = args.ElementAtOrDefault(0) ?? "user> ";
            Repl.Loop(prompt);
        }
    }
}
