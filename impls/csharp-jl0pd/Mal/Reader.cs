using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Mal
{
    public static class EnumerableExtensions
    {
        public static IEnumerable<KeyValuePair<T, T>> ToKeyValuePairs<T>(this IEnumerable<T> seq)
        {
            T? prev = default;
            int i = 0;
            foreach (T el in seq)
            {
                if (i % 2 == 1)
                {
                    yield return new KeyValuePair<T, T>(prev!, el);
                }
                prev = el;
                i++;
            }
        }
    }

    public static class ReaderStatic
    {
        public static MalType ReadString(string s)
        {
            var tok = new Reader(s);
            var it = tok.GetEnumerator();
            return ReadForm(it);
        }

        public static MalType ReadForm(IEnumerator<string> it, bool move = true)
        {
            if (move && !it.MoveNext())
            {
                return ThrowEOF<MalType>();
            }

            return it.Current switch
            {
                "~@" => new MalSpliceUnquote(ReadForm(it)),
                string s => s[0] switch
                {
                    '(' =>  ReadSequence(')', e => new MalList(e), it),
                    '[' =>  ReadSequence(']', e => new MalVector(e), it),
                    '{' => ReadSequence('}', e => new MalDict(e.ToKeyValuePairs()), it),
                    '`' => new MalQuasiQuote(ReadForm(it)),
                    '\'' => new MalQuote(ReadForm(it)),
                    '@' => new MalDeref(ReadForm(it)),
                    '^' => new MalWithMeta(ReadForm(it)),
                    '~' => new MalUnquote(ReadForm(it)),
                    _ => ReadAtom(it),
                }
            };
        }

        public static MalSequence ReadSequence(char closingBracket, Func<IEnumerable<MalType>, MalSequence> constructor, IEnumerator<string> it)
        {
            var elements = new List<MalType>();
            while (it.MoveNext())
            {
                if (it.Current[0] == closingBracket)
                {
                    return constructor.Invoke(elements);
                }
                else
                {
                    elements.Add(ReadForm(it, false));
                }
            }
            return ThrowEOF<MalVector>();
        }

        [DoesNotReturn]
        private static T ThrowEOF<T>() => throw new EndOfStreamException();

        public static MalType ReadAtom(IEnumerator<string> it)
            => MalType.FromString(it.Current);
    }

    public sealed class Reader : IEnumerable<string>
    {
        private const string _regexp = @"[\s,]*(~@|[\[\]{}()'`~^@]|""(?:\\.|[^\\""])*""?|;.*|[^\s\[\]{}('""`,;)]*)";

        private readonly string _input;
        private readonly IReadOnlyList<string> _tokens;

        public Reader(string input)
        {
            _input = input;
            var matches = Regex.Matches(input, _regexp);
            _tokens = matches
                        .Select(m => m.Value.Trim().Trim(','))
                        .Where(m => !string.IsNullOrWhiteSpace(m))
                        .ToArray();
        }

        public IEnumerator<string> GetEnumerator() => new Enumerator(this);

        IEnumerator IEnumerable.GetEnumerator() => new Enumerator(this);

        private class Enumerator : IEnumerator<string>
        {
            private readonly Reader _reader;
            private int _index = -1;

            internal Enumerator(Reader reader) => _reader = reader;

            public string Current => _reader._tokens[_index];
            object IEnumerator.Current => Current;

            public void Dispose()
            {
            }

            public bool MoveNext() => ++_index < _reader._tokens.Count;

            public void Reset() => throw new NotSupportedException();
        }
    }
}