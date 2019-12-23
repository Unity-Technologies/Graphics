using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.VFX;
using System.Text;

namespace UnityEditor.VFX
{
    class VFXDataOutputEvent : VFXData
    {
        public override VFXDataType type => VFXDataType.OutputEvent;

        public override void CopySettings<T>(T dst)
        {
            //There is nothing serialized here
        }

        public override void FillDescs(List<VFXGPUBufferDesc> outBufferDescs, List<VFXTemporaryGPUBufferDesc> outTemporaryBufferDescs, List<VFXEditorSystemDesc> outSystemDescs, VFXExpressionGraph expressionGraph, Dictionary<VFXContext, VFXContextCompiledData> contextToCompiledData, Dictionary<VFXContext, int> contextSpawnToBufferIndex, VFXDependentBuffersData dependentBuffers, Dictionary<VFXContext, List<VFXContextLink>[]> effectiveFlowInputLinks, VFXSystemNames systemNames = null)
        {
            throw new InvalidOperationException("Success !!");
        }

        public override void GenerateAttributeLayout(Dictionary<VFXContext, List<VFXContextLink>[]> effectiveFlowInputLinks)
        {
        }

        public override string GetAttributeDataDeclaration(VFXAttributeMode mode)
        {
            throw new NotImplementedException();
        }

        public override VFXDeviceTarget GetCompilationTarget(VFXContext context)
        {
            return VFXDeviceTarget.CPU;
        }

        public override string GetLoadAttributeCode(VFXAttribute attrib, VFXAttributeLocation location)
        {
            throw new NotImplementedException();
        }

        public override string GetStoreAttributeCode(VFXAttribute attrib, string value)
        {
            throw new NotImplementedException();
        }
    }
}
