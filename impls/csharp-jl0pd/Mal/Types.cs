using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace Mal
{
    public enum TypeKind
    {
        None,
        List,
        HashMap,
        Bool,
        Number,
        Symbol,
        String,
        Char,
        Nil,
        Quote,
        Unquote,
        QuasiQuote,
        Deref,
        SpliceUnquote,
        WithMeta,
        Keyword,
        Delegate,
    }

    public abstract class MalType
    {
        public abstract TypeKind Kind { get; }
        public abstract string AsLiteral();

        public override string ToString() => $"{{{Kind}}}";

        public static MalType<T> CreateGeneric<T>(T value)
        {
            object? result = null;
            if (typeof(T) == typeof(int))
            {
                result = new MalInt32((int)(object)value!);
            }
            else if (typeof(T) == typeof(double))
            {
                result = new MalDouble((double)(object)value!);
            }
            else if (typeof(T) == typeof(string))
            {
                result = new MalString((string)(object)value!);
            }
            return (MalType<T>)(result ?? throw new InvalidCastException($"Cannot convert {typeof(T).FullName} to MalType"));
        }

        public static MalBool Create(bool value)
            => value ? MalBool.True : MalBool.False;

        public static MalInt32 Create(int value)
            => new(value);

        public static MalDouble Create(double value)
            => new(value);

        public static MalType? TryCreateNumber(string value)
        {
            if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double d))
            {
                return Create(d);
            }
            else if (int.TryParse(value, out int i))
            {
                return Create(i);
            }
            else
            {
                return null;
            }
        }

        public static MalType CreateNumber(string value)
            => TryCreateNumber(value) ?? throw new FormatException();

        public static MalString Create(string s)
            => string.IsNullOrEmpty(s)
                ? MalString.Empty
                : new(s);

        public static MalNil CreateNil()
            => MalNil.Instance;

        public static MalChar Create(char c)
            => new(c);

        public static MalList Create(IEnumerable<MalType> values)
            => values switch
            {
                ICollection { Count: 0 } => MalList.Empty,
                ICollection<MalType> { Count: 0 } => MalList.Empty,
                _ => new(values),
            };

        public static MalType FromString(string value)
        {
            MalType? n = TryCreateNumber(value);
            if (n is not null)
            {
                return n;
            }

            return value switch
            {
                "true" => Create(true),
                "false" => Create(false),
                "nil" => CreateNil(),
                string str when str[0] == ':' => new MalKeyword(str[1..]),
                string str when !IsValid(str) => throw new EndOfStreamException(),
                string str when str[0] == '"' && str[^1] == '"' => Create(str),
                string str => new MalSymbol(str.Trim()),
            };
        }

        private static bool IsValid(string s)
        {
            if (s.Length == 0
            || s.Length == 1 && (s[0] == '"' || s[^1] == '"'))
            {
                return false;
            }

            bool escaped = false;
            bool inString = s[0] == '"';
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                if (c == '"' && !escaped && i != 0 && i != s.Length - 1) // unescaped " in the middle
                {
                    return false;
                }

                if (c == '"' && !escaped && i == s.Length - 1)
                {
                    inString = false;
                }

                if (c == '\\')
                {
                    escaped = !escaped;
                }
                else if (escaped)
                {
                    escaped = false;
                }
            }

            return !inString;
        }
    }

    public abstract class MalType<T> : MalType
    {
        public abstract T Value { get; }
        public override string ToString() => $"{{{Kind}: {AsLiteral()}}}";
    }

    public abstract class MalWrapper : MalType<MalType>
    {
        public override MalType Value { get; }
        protected MalWrapper(MalType value) => Value = value;
        protected abstract string Name { get; }
        public override string AsLiteral() => $"({Name} {Value.AsLiteral()})";
    }

    public class MalDeref : MalWrapper
    {
        public MalDeref(MalType value) : base(value) { }
        public override TypeKind Kind => TypeKind.Deref;
        protected override string Name => "deref";
    }

    public class MalWithMeta : MalWrapper
    {
        public MalWithMeta(MalType value) : base(value) { }
        public override TypeKind Kind => TypeKind.WithMeta;
        protected override string Name => "with-meta";
    }

    public class MalSpliceUnquote : MalWrapper
    {
        public MalSpliceUnquote(MalType value) : base(value) { }
        public override TypeKind Kind => TypeKind.SpliceUnquote;
        protected override string Name => "splice-unquote";
    }

    public class MalQuote : MalWrapper
    {
        public MalQuote(MalType value) : base(value) { }
        public override TypeKind Kind => TypeKind.Quote;
        protected override string Name => "quote";
    }

    public class MalUnquote : MalWrapper
    {
        public MalUnquote(MalType value) : base(value) { }
        public override TypeKind Kind => TypeKind.Unquote;
        protected override string Name => "unquote";
    }

    public class MalQuasiQuote : MalWrapper
    {
        public MalQuasiQuote(MalType value) : base(value) { }
        public override TypeKind Kind => TypeKind.QuasiQuote;
        protected override string Name => "quasiquote";
    }

    public abstract class MalNumber<T> : MalType<T> where T : unmanaged
    {
        public override TypeKind Kind => TypeKind.Number;
    }

    public class MalInt32 : MalNumber<int>
    {
        public MalInt32(int value) => Value = value;
        public override int Value { get; }
        public override string AsLiteral() => Value.ToString(CultureInfo.InvariantCulture);
    }

    public class MalDouble : MalNumber<double>
    {
        public MalDouble(double value) => Value = value;
        public override double Value { get; }
        public override string AsLiteral() => Value.ToString(CultureInfo.InvariantCulture);
    }

    public class MalChar : MalType<char>
    {
        public MalChar(char value) => Value = value;
        public override char Value { get; }
        public override TypeKind Kind => TypeKind.Char;
        public override string AsLiteral() => Value.ToString(CultureInfo.InvariantCulture);
    }

    public class MalString : MalType<string>
    {
        public MalString(string value) => Value = value;
        public override TypeKind Kind => TypeKind.String;
        public override string Value { get; }

        public static MalString Empty { get; } = new MalString("");
        public override string AsLiteral() => Value;
    }

    public class MalSymbol : MalType<string>
    {
        public MalSymbol(string value) => Value = value;
        public override TypeKind Kind => TypeKind.Symbol;
        public override string Value { get; }

        public static MalSymbol Empty { get; } = new MalSymbol("");
        public override string AsLiteral() => Value;
    }

    public class MalKeyword : MalType<string>
    {
        public MalKeyword(string value) => Value = value;
        public override string Value { get; }

        public override TypeKind Kind => TypeKind.Keyword;

        public override string AsLiteral() => $":{Value}";
    }

    public class MalNil : MalType
    {
        public static MalNil Instance { get; } = new MalNil();

        public override TypeKind Kind => TypeKind.Nil;
        public override string AsLiteral() => "nil";
    }

    public class MalBool : MalType<bool>
    {
        public override TypeKind Kind => TypeKind.Bool;

        public override bool Value { get; }
        public override string AsLiteral() => Value ? "true" : "false";

        public MalBool(bool value) => Value = value;

        public static MalBool True { get; } = new MalBool(true);
        public static MalBool False { get; } = new MalBool(false);
    }

    public abstract class MalSequence : MalType<IReadOnlyCollection<MalType>>
    {
        public abstract IReadOnlyCollection<MalType> Values { get; }
        public override IReadOnlyCollection<MalType> Value => Values;

        protected StringBuilder ValuesToString()
            => new StringBuilder().AppendJoin(' ', Values.Select(v => v.AsLiteral()));
    }

    public class MalDict : MalSequence
    {
        public MalDict(IEnumerable<KeyValuePair<MalType, MalType>> pairs)
        {
            Dict = new Dictionary<MalType, MalType>(pairs);
        }

        public override IReadOnlyList<MalType> Values => Dict.Values.ToArray();
        public IReadOnlyDictionary<MalType, MalType> Dict { get; }

        public override TypeKind Kind => TypeKind.HashMap;

        public override string AsLiteral()
        {
            var sb = new StringBuilder();
            sb.Append('{');
            sb.AppendJoin(' ', Dict.Select(k => $"{k.Key.AsLiteral()} {k.Value.AsLiteral()}"));
            sb.Append('}');
            return sb.ToString();
        }
    }

    public class MalList : MalSequence
    {
        public override TypeKind Kind => TypeKind.List;

        public override IReadOnlyList<MalType> Values { get; }

        public MalList(IEnumerable<MalType> elements)
            => Values = elements.ToArray();

        public static MalList Empty { get; } = new MalList(Array.Empty<MalList>());

        public override string AsLiteral()
        {
            var sb = new StringBuilder();
            sb.Append('(');
            sb.Append(ValuesToString());
            sb.Append(')');
            return sb.ToString();
        }
    }

    public class MalVector : MalSequence
    {
        public override TypeKind Kind => TypeKind.List;

        public override IReadOnlyList<MalType> Values { get; }

        public MalVector(IEnumerable<MalType> elements)
            => Values = elements.ToArray();

        public static MalVector Empty { get; } = new MalVector(Array.Empty<MalVector>());

        public override string AsLiteral()
        {
            var sb = new StringBuilder();
            sb.Append('[');
            sb.Append(ValuesToString());
            sb.Append(']');
            return sb.ToString();
        }
    }

    public abstract class MalDelegate<T> : MalType<T> where T : Delegate
    {
        public override TypeKind Kind => TypeKind.Delegate;
        public override string AsLiteral() => $"(lambda {RuntimeHelpers.GetHashCode(this):X})";

        public static implicit operator MalDelegate<Delegate>(MalDelegate<T> func)
            => new BaseDelegate(func.Value);

        private class BaseDelegate : MalDelegate<Delegate>
        {
            public BaseDelegate(Delegate value) => Value = value;
            public override Delegate Value {get;}
        }
    }

    public static class MalDelegate
    {
        public static MalProjection<TSource, TRes> Create<TSource, TRes>(Func<TSource, TRes> func)
        where TSource : MalType
        where TRes : MalType
            => new(func);

        public static MalBinaryFunction<TFirst, TSecond, TRes> Create<TFirst, TSecond, TRes>(Func<TFirst, TSecond, TRes> func)
        where TFirst : MalType
        where TSecond : MalType
        where TRes : MalType
            => new(func);

        public static MalDelegate<Delegate> SimpleCreate<TFirst, TSecond, TResult>(Func<TFirst, TSecond, TResult> func)
        {
            return new MalBinaryFunction<MalType<TFirst>, MalType<TSecond>, MalType<TResult>>(
                    (f, s) => MalType.CreateGeneric(func(f.Value, s.Value)));
        }
    }

    public class MalProjection<TSource, TRes> : MalDelegate<Func<TSource, TRes>>
    where TSource : MalType
    where TRes : MalType
    {
        public MalProjection(Func<TSource, TRes> func) => Value = func;
        public override Func<TSource, TRes> Value { get; }
    }

    public class MalBinaryFunction<TFirst, TSecond, TRes> : MalDelegate<Func<TFirst, TSecond, TRes>>
    where TFirst : MalType
    where TSecond : MalType
    where TRes : MalType
    {
        public MalBinaryFunction(Func<TFirst, TSecond, TRes> func) => Value = func;
        public override Func<TFirst, TSecond, TRes> Value { get; }
    }
}