using Unity.GraphCommon.LowLevel.Editor;
using UnityEngine.VFX;

namespace UnityEditor.VFX
{
    class ExternalSetTask : ExternalAccessTask
    {
        public string ExposedName { get; }
        public VFXExpression Expression { get; }
        public VFXSpace Space { get; }

        public ExternalSetTask(string exposedName, VFXExpression expression, VFXSpace space)
            : base(new NameDataKey(exposedName), BindingUsage.Write)
        {
            ExposedName = exposedName;
            Expression = expression;
            Space = space;
        }
    }
}
