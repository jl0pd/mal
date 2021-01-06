using System;
using System.Data;
using System.Linq;

namespace Mal
{
    public static class Evaluator
    {
        private static (MalType res, ReplEnvironment env) EvalAst(MalType ast, ReplEnvironment env)
        {
            return ast switch
            {
                MalSymbol sym => (env.Get(sym.Value), env),
                MalList list => (new MalList(list.Values.Select(v => Eval(v, env).res)), env),
                MalVector vec => (new MalVector(vec.Values.Select(v => Eval(v, env).res)), env),
                MalDict dict => (new MalDict(dict.Dict.Flatten().Select(v => Eval(v, env).res).ToKeyValuePairs()), env),
                _ => (ast, env),
            };
        }

        public static (MalType res, ReplEnvironment env) Eval(MalType obj, ReplEnvironment env)
        {
            switch (obj)
            {
                case MalList { Values: { Count: 0 } }:
                    return (obj, env);
                case MalList def when def.Values[0] is MalSymbol { Value: "def!" }
                                   && def.Values[1] is MalSymbol { Value: string symbol }:
                    {
                        var (value, newEnv) = Eval(def.Values[2], env);
                        var newNewEnv = newEnv.Set(symbol, value);
                        return (value, newNewEnv);
                    }
                case MalList let when let.Values[0] is MalSymbol { Value: "let*" }
                                   && let.Values[2] is MalType expr:
                    {
                        var def = let.Values[1] switch
                        {
                            MalVector vec => vec.Values,
                            MalList list => list.Values,
                            _ => throw new InvalidCastException(),
                        };
                        if (def.Count > 1)
                        {
                            if (def.Count % 2 != 0) // odd count
                            {
                                throw new ArgumentException("Args count is odd");
                            }
                            MalType? value = null;
                            ReplEnvironment newEnv = env;
                            for (int i = 0; i < def.Count; i += 2)
                            {
                                if (def[i] is MalSymbol { Value: var sym })
                                {
                                    (value, newEnv) = Eval(def[i + 1], newEnv);
                                    newEnv = newEnv.Set(sym, value);
                                }
                                else
                                {
                                    throw new SyntaxErrorException("Value following 'let*' is not symbol");
                                }
                            }
                            if (value is null || newEnv is null)
                            {
                                throw new Exception();
                            }

                            (value, _) = Eval(expr, newEnv);

                            return (value, env);
                        }
                        else
                        {
                            if (def[0] is MalSymbol { Value: var sym })
                            {
                                var (value, newEnv) = Eval(def[1], env);
                                var newNewEnv = newEnv.Set(sym, value);
                                (value, _) = Eval(expr, newNewEnv);
                                return (value, env);
                            }
                            else
                            {
                                throw new Exception("Value following 'let*' is not symbol");
                            }
                        }
                    }
                case MalList list:
                    {
                        var (newObj, newEnv) = EvalAst(list.Values[0], env);
                        var func = (MalDelegate<Delegate>)newObj;

                        var (newList, newNewEnv) = EvalAst(new MalList(list.Values.Skip(1)), newEnv);
                        var args = ((MalList)newList).Values.Cast<object>().ToArray();
                        var result = func.Value.DynamicInvoke(args);
                        if (result is null)
                        {
                            throw new NullReferenceException("Function returned null");
                        }

                        return ((MalType)result, newNewEnv);
                    }
                default:
                    return EvalAst(obj, env);
            }
        }
    }
}