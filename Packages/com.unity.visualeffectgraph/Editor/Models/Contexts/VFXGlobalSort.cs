using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.VFX;
using UnityEditor.VFX.Block;
using static UnityEditor.VFX.VFXSortingUtility;

namespace UnityEditor.VFX
{
    class VFXGlobalSort : VFXContext
    {
        public VFXGlobalSort() : base(VFXContextType.Filter, VFXDataType.Particle, VFXDataType.Particle) { }
        public override string name { get { return "GlobalSortKeys"; } }
        public override string codeGeneratorTemplate { get { return VisualEffectGraphPackageInfo.assetPackagePath + "/Shaders/VFXGlobalSortKeys"; } }
        public override bool codeGeneratorCompute { get { return true; } }
        public override VFXTaskType taskType { get { return VFXTaskType.GlobalSort; } }

        public SortCriteria sortCriterion = SortCriteria.DistanceToCamera;
        public VFXSlot customSortingSlot;
        public bool revertSorting;

        public override IEnumerable<VFXAttributeInfo> attributes
        {
            get
            {
                yield return new VFXAttributeInfo(VFXAttribute.Position, VFXAttributeMode.Read);
                if (sortCriterion is SortCriteria.YoungestInFront)
                {
                    yield return new VFXAttributeInfo(VFXAttribute.Age, VFXAttributeMode.Read);
                }
            }
        }

        public override IEnumerable<VFXMapping> additionalMappings
        {
            get
            {
                yield return new VFXMapping("globalSort", 1);
                yield return new VFXMapping("isPerCameraSort", IsPerCamera(sortCriterion) ? 1 : 0);
            }
        }

        public override VFXExpressionMapper GetExpressionMapper(VFXDeviceTarget target)
        {
            var localSpace = ((VFXDataParticle)GetData()).space == VFXCoordinateSpace.Local;
            if (localSpace && target == VFXDeviceTarget.GPU) // Needs to add locaToWorld matrix
            {
                var gpuMapper = new VFXExpressionMapper();
                if (IsPerCamera(sortCriterion))
                {
                    gpuMapper.AddExpression(VFXBuiltInExpression.LocalToWorld, "localToWorld", -1);
                    gpuMapper.AddExpression(VFXBuiltInExpression.WorldToLocal, "worldToLocal", -1);
                }

                if (sortCriterion == SortCriteria.Custom)
                {
                    var sortKeyExp = customSortingSlot.GetExpression();
                    gpuMapper.AddExpression(sortKeyExp, "sortKey", -1);
                }
                return gpuMapper;
            }

            return null; // cpu
        }

        public override IEnumerable<string> additionalDefines
        {
            get
            {
                if(IsPerCamera(sortCriterion))
                    yield return "HAVE_VFX_MODIFICATION"; //For correct handling of instanced matrices
                foreach (string additionalDef in GetSortingAdditionalDefines(sortCriterion))
                {
                    yield return additionalDef;
                }
                yield return "SORTING_SIGN " + (revertSorting ? -1 : 1);
            }
        }
    }
}
