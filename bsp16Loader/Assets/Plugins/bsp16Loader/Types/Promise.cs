using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Promise
{
    public static Promise<T> StartPromice<T>(this T a,MonoBehaviour b, Func<T, T> act) where T : YieldInstruction 
    {
        return new Promise<T>() {a = act, mb = b, last = a};
    }
}

public class Promise<T> where T : YieldInstruction
{
    public MonoBehaviour mb;
    public Promise<T> parent;
    public Func<T, T> a;
    static Stack<Func<T, T>> collect = new Stack<Func<T, T>>();
    public void Then(Action<T> act)
    {
        mb.StartCoroutine(ExecuteAll(act));
    }
    public T last;
    public IEnumerator ExecuteAll(Action<T> act)
    {
        collect.Clear();
        Promise<T> cur = this;
        while (cur != null)
        {
            collect.Push(cur.a);
            cur = cur.parent;
        }
        yield return last;
        while (collect.Count > 0)
        {
            last = collect.Pop()(last);
            yield return last;
        }
        act(last);
    }

    public Promise<T> Then(Func<T, T> act)
    {
        return new Promise<T>() {parent = this, a = act, mb = mb, last = last};
    }
}