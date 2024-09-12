using System;
using System.Text.RegularExpressions;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEngine;
using UnityEditor.Graphing;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.ShaderGraph.Internal;
using System.Reflection;

namespace UnityEditor.ShaderGraph
{
    using PopupList = UnityEditor.ShaderGraph.Drawing.Controls.PopupList;
    [Title("Input", "Geometry", "View Vector")]
    class ViewVectorNode : CodeFunctionNode
    {
        public ViewVectorNode()
        {
            name = "View Vector";
            synonyms = new string[] { "eye vector" };
        }

        public virtual List<CoordinateSpace> validSpaces => new List<CoordinateSpace> { CoordinateSpace.Object, CoordinateSpace.View, CoordinateSpace.World, CoordinateSpace.Tangent };

        [SerializeField]
        private CoordinateSpace m_Space = CoordinateSpace.World;

        [PopupControl("Space")]
        public PopupList spacePopup
        {
            get
            {
                var names = validSpaces.Select(cs => cs.ToString().PascalToLabel()).ToArray();
                return new PopupList(names, (int)m_Space);
            }
            set
            {
                if (m_Space == (CoordinateSpace)value.selectedEntry)
                    return;

                m_Space = (CoordinateSpace)value.selectedEntry;
                UpdateNodeAfterDeserialization();
                Dirty(ModificationScope.Topological);
            }
        }
        public CoordinateSpace space => m_Space;

        public override bool hasPreview
        {
            get { return true; }
        }

        public override PreviewMode previewMode
        {
            get { return PreviewMode.Preview3D; }
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            switch (space)
            {
                case CoordinateSpace.Object:
                    return GetType().GetMethod("Unity_ViewVectorObject", BindingFlags.Static | BindingFlags.NonPublic);
                case CoordinateSpace.View:
                    return GetType().GetMethod("Unity_ViewVectorView", BindingFlags.Static | BindingFlags.NonPublic);
                case CoordinateSpace.World:
                    return GetType().GetMethod("Unity_ViewVectorWorld", BindingFlags.Static | BindingFlags.NonPublic);
                case CoordinateSpace.Tangent:
                    return GetType().GetMethod("Unity_ViewVectorTangent", BindingFlags.Static | BindingFlags.NonPublic);
                default:
                    throw new Exception();
            }
        }

        static string Unity_ViewVectorObject(
            [Slot(3, Binding.WorldSpacePosition, true, ShaderStageCapability.All)] Vector3 WorldSpacePosition,
            [Slot(0, Binding.None)] out Vector3 Out)
        {
            Out = new Vector3();
            return
@"
{
    Out = _WorldSpaceCameraPos.xyz - GetAbsolutePositionWS(WorldSpacePosition);
    if(!IsPerspectiveProjection())
    {
        Out = GetViewForwardDir() * dot(Out, GetViewForwardDir());
    }
    Out = TransformWorldToObjectDir(Out, false);
}
";
        }

        static string Unity_ViewVectorView(
            [Slot(2, Binding.ViewSpacePosition, true, ShaderStageCapability.All)] Vector3 ViewSpacePosition,
            [Slot(0, Binding.None)] out Vector3 Out)
        {
            Out = new Vector3();
            return
@"
{
    if(IsPerspectiveProjection())
    {
        Out = -ViewSpacePosition;
    }
    else
    {
        Out = -$precision3(0.0f, 0.0f, ViewSpacePosition.z);
    }
}
";
        }

        static string Unity_ViewVectorWorld(
            [Slot(3, Binding.WorldSpacePosition, true, ShaderStageCapability.All)] Vector3 WorldSpacePosition,
            [Slot(0, Binding.None)] out Vector3 Out)
        {
            Out = new Vector3();
            return
@"
{
    Out = _WorldSpaceCameraPos.xyz - GetAbsolutePositionWS(WorldSpacePosition);
    if(!IsPerspectiveProjection())
    {
        Out = GetViewForwardDir() * dot(Out, GetViewForwardDir());
    }
}
";
        }

        static string Unity_ViewVectorTangent(
            [Slot(3, Binding.WorldSpacePosition, true, ShaderStageCapability.All)] Vector3 WorldSpacePosition,
            [Slot(4, Binding.WorldSpaceTangent, true, ShaderStageCapability.All)] Vector3 WorldSpaceTangent,
            [Slot(5, Binding.WorldSpaceBitangent, true, ShaderStageCapability.All)] Vector3 WorldSpaceBitangent,
            [Slot(6, Binding.WorldSpaceNormal, true, ShaderStageCapability.All)] Vector3 WorldSpaceNormal,
            [Slot(0, Binding.None)] out Vector3 Out)
        {
            Out = new Vector3();
            return
@"
{
    $precision3x3 basisTransform = $precision3x3(WorldSpaceTangent, WorldSpaceBitangent, WorldSpaceNormal);
    Out = _WorldSpaceCameraPos.xyz - GetAbsolutePositionWS(WorldSpacePosition);
    if(!IsPerspectiveProjection())
    {
        Out = GetViewForwardDir() * dot(Out, GetViewForwardDir());
    }
    Out = length(Out) * TransformWorldToTangent(Out, basisTransform);
}
";
        }
    }
}
