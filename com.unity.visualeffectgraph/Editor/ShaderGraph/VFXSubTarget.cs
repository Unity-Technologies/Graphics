using System;
using UnityEditor.ShaderGraph;

namespace UnityEditor.VFX
{
    static class VFXSubTarget
    {
        static class VFXFields
        {
            public const string kTag = "OutputType";
            public static FieldDescriptor ParticleMesh            = new FieldDescriptor(kTag, "Mesh",            "VFX_PARTICLE_MESH 1");
            public static FieldDescriptor ParticlePlanarPrimitive = new FieldDescriptor(kTag, "PlanarPrimitive", "VFX_PARTICLE_PLANAR_PRIMITIVE 1");
        }

        public static event Func<SubShaderDescriptor, VFXContext, VFXContextCompiledData, SubShaderDescriptor> OnPostProcessSubShader;

        internal static SubShaderDescriptor PostProcessSubShader(SubShaderDescriptor descriptor, VFXContext context, VFXContextCompiledData data)
        {
            // TODO: Move generic VFX sub shader processing in here and break up the callback into SRP-specific portions (like FragInputs struct).
            return OnPostProcessSubShader?.Invoke(descriptor, context, data) ?? descriptor;
        }

        internal static void GetFields(ref TargetFieldContext fieldsContext, VFXContext context)
        {
            fieldsContext.AddField(Fields.GraphVFX);

            // Select the primitive implementation.
            switch (context.taskType)
            {
                case VFXTaskType.ParticleMeshOutput:
                    fieldsContext.AddField(VFXFields.ParticleMesh);
                    break;
                case VFXTaskType.ParticleTriangleOutput:
                case VFXTaskType.ParticleOctagonOutput:
                case VFXTaskType.ParticleQuadOutput:
                    fieldsContext.AddField(VFXFields.ParticlePlanarPrimitive);
                    break;
            }
        }
    }
}
