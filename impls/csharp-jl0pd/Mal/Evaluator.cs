using System;
using System.Data;
using System.Linq;

namespace Mal
{
    public readonly struct EvalContext<T> where T : MalType
    {
        public EvalContext(T obj, ReplEnvironment env) => (Obj, Env) = (obj, env);
        public T Obj { get; }
        public ReplEnvironment Env { get; }

        public EvalContext<T> WithEnv(ReplEnvironment env)
            => new (Obj, env);

        public EvalContext<T> WithEnv<TOther>(EvalContext<TOther> context) where TOther : MalType
            => new (Obj, context.Env);

        public EvalContext<TNew> WithObj<TNew>(TNew obj) where TNew : MalType
            => new (obj, Env);

        public EvalContext<T> Set(string key, MalType value)
            => new (Obj, Env.Set(key, value));

        public MalType Get(string key)
            => Env.Get(key);

        public void Deconstruct(out T obj, out ReplEnvironment env)
        {
            obj = Obj;
            env = Env;
        }

        public static implicit operator EvalContext<MalType>(EvalContext<T> context)
            => context.WithObj((MalType)context.Obj);
    }

    public static class EvalContext
    {
        public static EvalContext<T> Create<T>(T obj, ReplEnvironment env) where T : MalType
            => new(obj, env);
    }

    public static class Evaluator
    {
        private static EvalContext<MalType> EvalAst(EvalContext<MalType> context)
        {
            MalType obj = context.Obj switch
            {
                MalSymbol sym => context.Env.Get(sym.Value),
                MalList list => new MalList(list.Values.Select(v => Eval(context.WithObj(v)).Obj)),
                MalVector vec => new MalVector(vec.Values.Select(v => Eval(context.WithObj(v)).Obj)),
                MalDict dict => new MalDict(dict.Dict.Flatten().Select(v => Eval(context.WithObj(v)).Obj).ToKeyValuePairs()),
                var x => x,
            };

            return context.WithObj(obj);
        }

        public static EvalContext<MalType> Eval(EvalContext<MalType> context)
            => context.Obj switch
            {
                MalList { Values: { Count: 0 } } => context,
                MalList def when def.Values[0] is MalSymbol { Value: "def!" } && def.Values[1] is MalSymbol => EvalDefBang(context.WithObj(def)),
                MalList let when let.Values[0] is MalSymbol { Value: "let*" } => EvalLetMul(context.WithObj(let)),
                MalList list => EvalListApplication(context.WithObj(list)),
                _ => EvalAst(context),
            };

        private static EvalContext<MalType> EvalDefBang(EvalContext<MalList> context)
        {
            EvalContext<MalType> newContext = Eval(context.WithObj(context.Obj.Values[2]));
            newContext = newContext.Set(((MalSymbol)context.Obj.Values[1]).Value, newContext.Obj);
            return newContext;
        }

        private static EvalContext<MalType> EvalLetMul(EvalContext<MalList> context)
        {
            var def = context.Obj.Values[1] switch
            {
                MalVector vec => vec.Values,
                MalList list => list.Values,
                _ => throw new InvalidCastException(),
            };

            if (def.Count % 2 != 0)
            {
                throw new ArgumentException("Args count is odd");
            }

            EvalContext<MalType> newContext = context.WithObj((MalType)MalType.CreateNil());
            for (int i = 0; i < def.Count; i += 2)
            {
                if (def[i] is MalSymbol { Value: var sym })
                {
                    newContext = Eval(newContext.WithObj(def[i+1]));
                    newContext = newContext.Set(sym, newContext.Obj);
                }
                else
                {
                    throw new SyntaxErrorException("Value following 'let*' is not symbol");
                }
            }

            var res = Eval(newContext.WithObj(context.Obj.Values[2])).WithEnv(context);
            return res;
        }

        private static EvalContext<MalType> EvalListApplication(EvalContext<MalList> context)
        {
            var newContext = EvalAst(context.WithObj(context.Obj.Values[0]));
            var func = (MalDelegate<Delegate>)newContext.Obj;

            var newNewContext = EvalAst(newContext.WithObj(new MalList(context.Obj.Values.Skip(1))));
            var args = ((MalList)newNewContext.Obj).Values.Cast<object>().ToArray();
            var result = (MalType?)func.Value.DynamicInvoke(args);
            if (result is null)
            {
                throw new NullReferenceException("Function returned null");
            }

            return newNewContext.WithObj(result);
        }

        // private static (MalType res, ReplEnvironment)
    }
}