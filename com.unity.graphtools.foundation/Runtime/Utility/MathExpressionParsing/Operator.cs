namespace UnityEngine.GraphToolsFoundation.Overdrive
{
    readonly struct Operator
    {
        public readonly OperationType Type;
        public readonly string Str;
        public readonly int Precedence;
        public readonly Associativity Associativity;
        public readonly bool Unary;

        public Operator(OperationType type, string str, int precedence, Associativity associativity = Associativity.None,
                        bool unary = false)
        {
            Type = type;
            Str = str;
            Precedence = precedence;
            Associativity = associativity;
            Unary = unary;
        }
    }
}
