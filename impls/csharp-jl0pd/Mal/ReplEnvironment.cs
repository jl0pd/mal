using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Mal
{
    public class ReplEnvironment
    {
        public IImmutableDictionary<string, MalType> Data { get; }

        public ReplEnvironment? Parent { get; }

        public ReplEnvironment()
        {
            var builder = ImmutableDictionary.CreateBuilder<string, MalType>();

            builder.Add("+", MalDelegate.SimpleCreate((double x, double y) => x + y));
            builder.Add("-", MalDelegate.SimpleCreate((double x, double y) => x - y));
            builder.Add("*", MalDelegate.SimpleCreate((double x, double y) => x * y));
            builder.Add("/", MalDelegate.SimpleCreate((double x, double y) => x / y));

            Data = builder.ToImmutable();
        }

        public ReplEnvironment(ReplEnvironment parent) : this() => Parent = parent;

        private ReplEnvironment(IImmutableDictionary<string, MalType> data, ReplEnvironment? parent)
        {
            Data = data;
            Parent = parent;
        }

        public ReplEnvironment Set(string key, MalType value)
            => new(Data.SetItem(key, value), Parent);

        public MalType? TryGet(string key)
            => Data.TryGetValue(key, out MalType? value)
                ? value
                : (Parent?.TryGet(key));

        public MalType Get(string key)
            => TryGet(key) ?? throw new KeyNotFoundException($"The given key '{key}' was not present in the dictionary");
    }
}