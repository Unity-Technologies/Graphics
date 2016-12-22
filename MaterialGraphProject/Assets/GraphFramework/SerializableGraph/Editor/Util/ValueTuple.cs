using System;
namespace UnityEditor.Graphing.Util
{
    public struct ValueTuple<T1, T2>
    {
        public T1 Item1 { get; set; }
        public T2 Item2 { get; set; }

        public ValueTuple(T1 item1, T2 item2)
        {
            Item1 = item1;
            Item2 = item2;
        }

        public static ValueTuple<T1, T2> Create(T1 item1, T2 item2)
        {
            return new ValueTuple<T1, T2>(item1, item2);
        }
    }

    public static class ValueTuple
    {
        public static ValueTuple<T1, T2> Create<T1, T2>(T1 item1, T2 item2)
        {
            return new ValueTuple<T1, T2>(item1, item2);
        }
    }
}
