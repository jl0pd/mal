using System;
using System.Linq;

namespace Mal
{
    public static class Evaluator
    {
        private static (MalType res, ReplEnvironment env) EvalAst(MalType ast, ReplEnvironment env)
        {
            return ast switch
            {
                MalSymbol sym => (env.Functions[sym.Value], env),
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
                case MalList list:
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
                default:
                    return EvalAst(obj, env);
            }
        }
    }
}