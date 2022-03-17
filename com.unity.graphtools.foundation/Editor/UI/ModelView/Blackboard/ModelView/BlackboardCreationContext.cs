namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public class BlackboardCreationContext : IViewContext
    {
        public static readonly BlackboardCreationContext VariableCreationContext = new BlackboardCreationContext();
        public static readonly BlackboardCreationContext VariablePropertyCreationContext = new BlackboardCreationContext();
        public bool Equals(IViewContext other)
        {
            return ReferenceEquals(this, other);
        }
    }
}
