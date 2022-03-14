namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// The type of <see cref="TracingStep"/>.
    /// </summary>
    public enum TracingStepType : byte
    {
        None,
        ExecutedNode,
        TriggeredPort,
        WrittenValue,
        ReadValue,
        Error
    }
}
