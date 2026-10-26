using Unity.GraphCommon.LowLevel.Editor;

namespace UnityEditor.VFX
{
    /// <summary>
    /// A binding for a parameter the runtime resolves for the task, rather than one the generated code reads.
    /// Nothing is declared in the shader for it: it only exists so the value reaches the task description.
    /// </summary>
    class VfxTaskParameterBinding : TemplatedTaskBinding<IDataDescription>
    {
        public VfxTaskParameterBinding(IDataKey key) : base(key)
        {
            ReadPathSet.Add(DataPath.Root);
        }
    }
}
