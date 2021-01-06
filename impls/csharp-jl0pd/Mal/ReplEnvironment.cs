using System;
using System.Collections.Generic;

namespace Mal
{
    public class ReplEnvironment
    {
        public IReadOnlyDictionary<string, MalDelegate<Delegate>> Functions { get; }

        public ReplEnvironment()
        {
            Functions = new Dictionary<string, MalDelegate<Delegate>>
            {
                ["+"] = MalDelegate.SimpleCreate((double x, double y) => x + y),
                ["-"] = MalDelegate.SimpleCreate((double x, double y) => x - y),
                ["*"] = MalDelegate.SimpleCreate((double x, double y) => x * y),
                ["/"] = MalDelegate.SimpleCreate((double x, double y) => x / y),
            };
        }
    }
}