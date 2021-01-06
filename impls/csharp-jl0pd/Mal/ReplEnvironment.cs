using System.Collections.Generic;
using System.Collections.Immutable;

namespace Mal
{
    public class ReplEnvironment
    {
        public IImmutableDictionary<string, MalType> Data { get; }
        public ReplEnvironment? Parent { get; }

        public ReplEnvironment Set(string key, MalType value)
            => new(Data.SetItem(key, value), Parent);

        public MalType? TryGet(string key)
            => Data.TryGetValue(key, out MalType? value)
                ? value
                : Parent?.TryGet(key);

        public ReplEnvironment() : this(_defaultData, null)
        {
        }

        public ReplEnvironment(ReplEnvironment parent) : this(_defaultData, parent)
        {
        }

        private ReplEnvironment(IImmutableDictionary<string, MalType> data, ReplEnvironment? parent)
        {
            Data = data;
            Parent = parent;
        }

        public MalType Get(string key)
            => TryGet(key) ?? throw new KeyNotFoundException(key);

        private static IImmutableDictionary<string, MalType> CreateDefaultData()
        {
            var builder = ImmutableDictionary.CreateBuilder<string, MalType>();

            builder.Add("+", MalDelegate.SimpleCreate((double x, double y) => x + y));
            builder.Add("-", MalDelegate.SimpleCreate((double x, double y) => x - y));
            builder.Add("*", MalDelegate.SimpleCreate((double x, double y) => x * y));
            builder.Add("/", MalDelegate.SimpleCreate((double x, double y) => x / y));

            return builder.ToImmutable();
        }

        private static readonly IImmutableDictionary<string, MalType> _defaultData = CreateDefaultData();
    }
}