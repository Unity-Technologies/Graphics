namespace UnityEditor.VFX
{
    /// <summary>
    /// HLSL naming for the struct the generated code scopes a task's own parameters in. Shared between
    /// whoever declares that struct and whoever writes code assigning or reading one of its members.
    /// </summary>
    static class VfxContextParametersHlsl
    {
        /// <summary>The generated struct scoping the parameters, and the name of its instance.</summary>
        public const string TypeName = "ContextParameters";
        public const string VariableName = "contextParameters";

        public static string Member(string parameterName) => $"{VariableName}.{parameterName}";
    }
}
