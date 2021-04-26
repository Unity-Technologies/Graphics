using System;
using System.Collections;
using System.Collections.Generic;


public static class ListUtils
{
    private class EmptyReadOnlyListClass<T> : IReadOnlyList<T>, IEnumerable, IEnumerator<T>, IEnumerator
    {
        public static EmptyReadOnlyListClass<T> _globalInstance;

        public T this[int index] => throw new ArgumentOutOfRangeException();
        public int Count => 0;

        T IEnumerator<T>.Current => throw new InvalidOperationException();
        object IEnumerator.Current => throw new InvalidOperationException();

        public IEnumerator<T> GetEnumerator()
        {
            return this;
        }

        void IDisposable.Dispose() {}

        bool IEnumerator.MoveNext()
        {
            return false;
        }

        void IEnumerator.Reset() {}

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this;
        }
    }

    public static IReadOnlyList<TResult> EmptyReadOnlyList<TResult>()
    {
        if (EmptyReadOnlyListClass<TResult>._globalInstance == null)
            EmptyReadOnlyListClass<TResult>._globalInstance = new EmptyReadOnlyListClass<TResult>();
        return EmptyReadOnlyListClass<TResult>._globalInstance;
    }
};
