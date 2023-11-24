using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Assertions;

namespace UnityEngine.Rendering
{
    internal enum InstanceType
    {
        MeshRenderer = 0,
        SpeedTree = 1,
        Count,

        LODGroup = 0, // Aliased for now because it is part of a different instance data space.
    }

    internal static class InstanceTypeInfo
    {
        public const int kInstanceTypeBitCount = 1;
        public const int kMaxInstanceTypesCount = 1 << kInstanceTypeBitCount;
        public const uint kInstanceTypeMask = kMaxInstanceTypesCount - 1;

        //@ For now it is probably fine to have inheritance, but in the future we might prefer composition if we end up with a lot of different types.
        //@ Inheritance is really needed to quickly define overlapped instance data spaces.
        private static InstanceType[] s_ParentTypes;
        private static List<InstanceType>[] s_ChildTypes;

        static InstanceTypeInfo()
        {
            Assert.IsTrue((int)InstanceType.Count <= kMaxInstanceTypesCount);
            InitParentTypes();
            InitChildTypes();
            ValidateTypeRelationsAreCorrectlySorted();
        }

        private static void InitParentTypes()
        {
            s_ParentTypes = new InstanceType[(int)InstanceType.Count];

            s_ParentTypes[(int)InstanceType.MeshRenderer] = InstanceType.MeshRenderer;
            s_ParentTypes[(int)InstanceType.SpeedTree] = InstanceType.MeshRenderer;
            // Add more parent types here if needed...
        }

        private static void InitChildTypes()
        {
            s_ChildTypes = new List<InstanceType>[(int)InstanceType.Count];

            for (int i = 0; i < (int)InstanceType.Count; ++i)
                s_ChildTypes[i] = new List<InstanceType>();

            for (int i = 0; i < (int)InstanceType.Count; ++i)
            {
                var type = (InstanceType)i;
                var parentType = s_ParentTypes[(int)type];

                if (type != parentType)
                    s_ChildTypes[(int)parentType].Add(type);
            }
        }

        private static InstanceType GetMaxChildTypeRecursively(InstanceType type)
        {
            InstanceType maxChildType = type;

            foreach (var childType in s_ChildTypes[(int)type])
                maxChildType = (InstanceType)Mathf.Max((int)maxChildType, (int)GetMaxChildTypeRecursively(childType));

            return maxChildType;
        }

        private static void FlattenChildInstanceTypes(InstanceType instanceType, NativeList<InstanceType> instanceTypes)
        {
            instanceTypes.Add(instanceType);

            foreach (var childType in s_ChildTypes[(int)instanceType])
                FlattenChildInstanceTypes(childType, instanceTypes);
        }

        private static void ValidateTypeRelationsAreCorrectlySorted()
        {
            var instanceTypesFlattened = new NativeList<InstanceType>((int)InstanceType.Count, Allocator.Temp);

            for(int i = 0; i < (int)InstanceType.Count; ++i)
            {
                InstanceType instanceType = (InstanceType)i;

                if(instanceType == s_ParentTypes[i])
                    FlattenChildInstanceTypes(instanceType, instanceTypesFlattened);
            }

            Assert.AreEqual(instanceTypesFlattened.Length, (int)InstanceType.Count);

            for (int i = 0; i < instanceTypesFlattened.Length; ++i)
                Assert.AreEqual((int)instanceTypesFlattened[i], i, "InstanceType relation is not properly ordered. Parent and child InstanceTypes should follow " +
                    "the depth first order to decrease unrelated type indices overlapping and better memory utilization.");
        }

        public static InstanceType GetParentType(InstanceType type)
        {
            return s_ParentTypes[(int)type];
        }

        public static List<InstanceType> GetChildTypes(InstanceType type)
        {
            return s_ChildTypes[(int)type];
        }
    }

    internal unsafe struct InstanceNumInfo
    {
        public fixed int InstanceNums[(int)InstanceType.Count];

        public void InitDefault()
        {
            for (int i = 0; i < (int)InstanceType.Count; ++i)
                InstanceNums[i] = 0;
        }

        public InstanceNumInfo(InstanceType type, int instanceNum)
        {
            InitDefault();
            InstanceNums[(int)type] = instanceNum;
        }

        public InstanceNumInfo(int meshRendererNum = 0, int speedTreeNum = 0)
        {
            InitDefault();
            InstanceNums[(int)InstanceType.MeshRenderer] = meshRendererNum;
            InstanceNums[(int)InstanceType.SpeedTree] = speedTreeNum;
        }

        public int GetInstanceNum(InstanceType type)
        {
            return InstanceNums[(int)type];
        }

        public int GetInstanceNumIncludingChildren(InstanceType type)
        {
            int numInstances = GetInstanceNum(type);

            var childTypes = InstanceTypeInfo.GetChildTypes(type);

            foreach (var childType in childTypes)
                numInstances += GetInstanceNumIncludingChildren(childType);

            return numInstances;
        }

        public int GetTotalInstanceNum()
        {
            int totalInstanceNum = 0;

            for (int i = 0; i < (int)InstanceType.Count; ++i)
                totalInstanceNum += InstanceNums[i];

            return totalInstanceNum;
        }
    }
}
