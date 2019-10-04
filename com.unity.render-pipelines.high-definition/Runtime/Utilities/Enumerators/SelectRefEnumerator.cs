namespace UnityEditor.Rendering.HighDefinition
{
    using System.Collections;

    /// <summary>EXPERIMENTAL: An enumerator performing a select function.</summary>
    /// <typeparam name="I">Type of the input value.</typeparam>
    /// <typeparam name="O">Type of the output value.</typeparam>
    /// <typeparam name="En">Type of the enumerator to consume by reference.</typeparam>
    /// <typeparam name="S">Type of the select function.</typeparam>
    public struct SelectRefEnumerator<I, O, En, S> : System.Collections.Generic.IEnumerator<O>
        where En : struct, IRefEnumerator<I>
        where S: struct, IInFunc<I, O>
    {
        En m_Enumerator;
        S m_Select;

        public SelectRefEnumerator(En enumerator, S select)
        {
            m_Enumerator = enumerator;
            m_Select = select;
        }

        public O Current => m_Select.Execute(m_Enumerator.current);

        object IEnumerator.Current => m_Select.Execute(m_Enumerator.current);

        public void Dispose() {}

        public bool MoveNext() => m_Enumerator.MoveNext();

        public void Reset() => m_Enumerator.Reset();
    }
}
