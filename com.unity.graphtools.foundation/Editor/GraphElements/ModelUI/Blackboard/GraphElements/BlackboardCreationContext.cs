namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public class BlackboardCreationContext : IUIContext
    {
        public static readonly BlackboardCreationContext VariableCreationContext = new BlackboardCreationContext();
        public static readonly BlackboardCreationContext VariablePropertyCreationContext = new BlackboardCreationContext();
        public bool Equals(IUIContext other)
        {
            return ReferenceEquals(this, other);
        }
    }
}
