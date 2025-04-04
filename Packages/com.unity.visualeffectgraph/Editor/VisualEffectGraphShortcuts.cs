using System;
using System.IO;

using UnityEditor.ShortcutManagement;
using UnityEditor.VFX.Operator;
using UnityEditor.VFX.UI;

using UnityEngine;
using UnityEngine.Rendering;
using Random = UnityEditor.VFX.Operator.Random;

namespace UnityEditor.VFX
{
    static class VisualEffectGraphShortcuts
    {
        [Shortcut("Visual Effect Graph/Frame All", typeof(VFXViewWindow), KeyCode.A)]
        static void FrameAll(ShortcutArguments args)
        {
            if (args.context is VFXViewWindow window)
            {
                window.graphView.FrameAll();
            }
        }

        [Shortcut("Visual Effect Graph/Frame Selection", typeof(VFXViewWindow), KeyCode.F)]
        static void FrameSelection(ShortcutArguments args)
        {
            if (args.context is VFXViewWindow window)
            {
                window.graphView.FrameSelection();
            }
        }

        [Shortcut("Visual Effect Graph/Frame Origin", typeof(VFXViewWindow), KeyCode.O)]
        static void FrameOrigin(ShortcutArguments args)
        {
            if (args.context is VFXViewWindow window)
            {
                window.graphView.FrameOrigin();
            }
        }

        [Shortcut("Visual Effect Graph/Frame Previous", typeof(VFXViewWindow), KeyCode.LeftBracket)]
        static void FramePrevious(ShortcutArguments args)
        {
            if (args.context is VFXViewWindow window)
            {
                window.graphView.FramePrev();
            }
        }

        [Shortcut("Visual Effect Graph/Frame Next", typeof(VFXViewWindow), KeyCode.RightBracket)]
        static void FrameNext(ShortcutArguments args)
        {
            if (args.context is VFXViewWindow window)
            {
                window.graphView.FrameNext();
            }
        }

        [Shortcut("Visual Effect Graph/Deselect All", typeof(VFXViewWindow), KeyCode.D, ShortcutModifiers.Shift)]
        static void DeselectAll(ShortcutArguments args)
        {
            if (args.context is VFXViewWindow window)
            {
                window.graphView.ClearSelection();
            }
        }

        [Shortcut("Visual Effect Graph/Compile", typeof(VFXViewWindow), KeyCode.C, ShortcutModifiers.Shift)]
        static void Compile(ShortcutArguments args)
        {
            if (args.context is VFXViewWindow window)
            {
                window.graphView.OnCompile();
            }
        }

        [Shortcut("Visual Effect Graph/Duplicate with Link", typeof(VFXViewWindow), KeyCode.D, ShortcutModifiers.Shift|ShortcutModifiers.Alt)]
        static void DuplicateWithLink(ShortcutArguments args)
        {
            if (args.context is VFXViewWindow window)
            {
                window.graphView.DuplicateSelectionWithEdges();
            }
        }

        [Shortcut("Visual Effect Graph/Restart VFX", typeof(VFXViewWindow), KeyCode.Space, ShortcutModifiers.Shift)]
        static void RestartComponent(ShortcutArguments args)
        {
            if (args.context is VFXViewWindow window)
            {
                window.graphView.ReinitComponents();
            }
        }

        [Shortcut("Visual Effect Graph/Toggle all debug panels", typeof(VFXViewWindow), KeyCode.Alpha5, ShortcutModifiers.Shift)]
        static void CloseAllDebugPanels(ShortcutArguments args)
        {
            if (args.context is VFXViewWindow window)
            {
                window.graphView.ToggleDebugPanels();
            }
        }

        [Shortcut("Visual Effect Graph/Toggle Blackboard", typeof(VFXViewWindow), KeyCode.Alpha1, ShortcutModifiers.Shift)]
        static void ToggleBlackboard(ShortcutArguments args)
        {
            if (args.context is VFXViewWindow window)
            {
                window.graphView.ToggleBlackboard();
            }
        }

        [Shortcut("Visual Effect Graph/Toggle Control Panel", typeof(VFXViewWindow), KeyCode.Alpha3, ShortcutModifiers.Shift)]
        static void ToggleControlPanel(ShortcutArguments args)
        {
            if (args.context is VFXViewWindow window)
            {
                window.graphView.ToggleComponentBoard();
            }
        }

        [Shortcut("Visual Effect Graph/Toggle Profiling Panel", typeof(VFXViewWindow), KeyCode.Alpha4, ShortcutModifiers.Shift)]
        static void ToggleProfilingPanel(ShortcutArguments args)
        {
            if (args.context is VFXViewWindow window)
            {
                window.graphView.ToggleProfilingBoard();
            }
        }

        [Shortcut("Visual Effect Graph/Save", typeof(VFXViewWindow), KeyCode.S, ShortcutModifiers.Action)]
        static void Save(ShortcutArguments args)
        {
            if (args.context is VFXViewWindow window)
            {
                window.graphView.OnSave();
            }
        }

        [Shortcut("Visual Effect Graph/Save As", typeof(VFXViewWindow), KeyCode.S, ShortcutModifiers.Action | ShortcutModifiers.Shift)]
        static void SaveAs(ShortcutArguments args)
        {
            if (args.context is VFXViewWindow window)
            {
                var originalPath = AssetDatabase.GetAssetPath(window.graphView.controller.model);
                var extension = Path.GetExtension(originalPath).Trim('.');
                var newFilePath = EditorUtility.SaveFilePanelInProject("Save VFX Graph As...", Path.GetFileNameWithoutExtension(originalPath), extension, "", Path.GetDirectoryName(originalPath));
                if (!string.IsNullOrEmpty(newFilePath))
                {
                    window.graphView.SaveAs(newFilePath);
                }
            }
        }

        [Shortcut("Visual Effect Graph/Open Documentation", typeof(VFXViewWindow), KeyCode.F1)]
        static void OpenDocumentation(ShortcutArguments args)
        {
            if (args.context is VFXViewWindow window)
            {
                if (window.graphView.selection.Count == 1 &&
                    window.graphView.selection[0] is VFXNodeUI nodeUI &&
                    DocumentationUtils.TryGetHelpURL(nodeUI.controller.model.GetType(), out var url))
                    Help.BrowseURL(url);
                else
                    Help.BrowseURL(Documentation.GetDefaultPackageLink());
            }
        }

        [Shortcut("Visual Effect Graph/Group Selection", typeof(VFXViewWindow), KeyCode.G, ShortcutModifiers.Shift)]
        static void GroupSelection(ShortcutArguments args)
        {
            if (args.context is VFXViewWindow window)
            {
                window.graphView.GroupSelection();
            }
        }

        [Shortcut("Visual Effect Graph/Toggle Collapse Selection", typeof(VFXViewWindow), KeyCode.P, ShortcutModifiers.Shift)]
        static void ToggleCollapseSelection(ShortcutArguments args)
        {
            if (args.context is VFXViewWindow window)
            {
                window.graphView.ToggleCollapseSelection();
            }
        }

        [Shortcut("Visual Effect Graph/Insert Node on Link", typeof(VFXViewWindow), KeyCode.R, ShortcutModifiers.Shift)]
        static void InsertNodeOnLink(ShortcutArguments args)
        {
            if (args.context is VFXViewWindow window)
            {
                window.graphView.CreateNodeOnEdge(Event.current.mousePosition);
            }
        }

        [Shortcut("Visual Effect Graph/Toggle Auto Compile", typeof(VFXViewWindow), KeyCode.A, ShortcutModifiers.Shift)]
        static void ToggleAutoCompile(ShortcutArguments args)
        {
            if (args.context is VFXViewWindow window)
            {
                window.autoCompile = !window.autoCompile;
                window.ShowNotification(new GUIContent($"Auto Compile {(window.autoCompile ? "On" : "Off")}"), 1.5);
                window.Repaint();
            }
        }

        [Shortcut("Visual Effect Graph/Add Sticky Note", typeof(VFXViewWindow), KeyCode.N, ShortcutModifiers.Shift)]
        static void AddStickyNote(ShortcutArguments args)
        {
            if (args.context is VFXViewWindow window)
            {
                window.graphView.AddStickyNote(Event.current.mousePosition);
            }
        }

        [Shortcut("Visual Effect Graph/Clean All Unlinked Operators", typeof(VFXViewWindow), KeyCode.Backspace, ShortcutModifiers.Shift)]
        static void CleanUnlinkedOperators(ShortcutArguments args)
        {
            if (args.context is VFXViewWindow window)
            {
                window.graphView.CleanUnLinkedOperators();
            }
        }

        [Shortcut("Visual Effect Graph/Convert to Subgraph Operators", typeof(VFXViewWindow), KeyCode.S, ShortcutModifiers.Shift)]
        static void ConvertToSubgraphOperator(ShortcutArguments args)
        {
            if (args.context is VFXViewWindow window)
            {
                window.graphView.ConvertToSubgraphOperator();
            }
        }

        [Shortcut("Visual Effect Graph/Toggle Property--Inline", typeof(VFXViewWindow), KeyCode.X, ShortcutModifiers.Shift)]
        static void ToggleNodePropertyOrInline(ShortcutArguments args)
        {
            if (args.context is VFXViewWindow window)
            {
                window.graphView.ToggleNodePropertyOrInline();
            }
        }

        [Shortcut("Visual Effect Graph/Save HLSL Code", typeof(VFXTextEditor), KeyCode.S, ShortcutModifiers.Action)]
        static void SaveHLSLCode(ShortcutArguments args)
        {
            if (args.context is VFXTextEditor window)
            {
                window.Save();
            }
        }

        [Shortcut("Visual Effect Graph/Undo HLSL Change", typeof(VFXTextEditor), KeyCode.Z, ShortcutModifiers.Action)]
        static void UndoHLSLCode(ShortcutArguments args)
        {
            if (args.context is VFXTextEditor window)
            {
                window.Undo();
            }
        }

        [Shortcut("Visual Effect Graph/Redo HLSL Change", typeof(VFXTextEditor), KeyCode.Y, ShortcutModifiers.Action)]
        static void RedoHLSLCode(ShortcutArguments args)
        {
            if (args.context is VFXTextEditor window)
            {
                window.Redo();
            }
        }

        [Shortcut("Visual Effect Graph/Increase Font Size in HLSL Code Editor", typeof(VFXTextEditor), KeyCode.WheelUp, ShortcutModifiers.Control)]
        static void IncreasTextSizeHLSLCode(ShortcutArguments args)
        {
            if (args.context is VFXTextEditor window)
            {
                window.ChangeTextSize(1);
            }
        }
        [Shortcut("Visual Effect Graph/Decrease Font Size in HLSL Code Editor", typeof(VFXTextEditor), KeyCode.WheelDown, ShortcutModifiers.Control)]
        static void DecreaseTextSizeHLSLCode(ShortcutArguments args)
        {
            if (args.context is VFXTextEditor window)
            {
                window.ChangeTextSize(-1);
            }
        }

        // Currently a text field eats up all shortcuts with ALT or SHIFT modifiers
        /*
        [Shortcut("Visual Effect Graph/Toggle Sub-variant in node search", typeof(VFXFilterWindow), KeyCode.V, ShortcutModifiers.Shift)]
        static void ToggleSubvariantNodeSearch(ShortcutArguments args)
        {
            if (args.context is VFXFilterWindow window)
            {
                window.ToggleSubVariantVisibility();
            }
        }*/

        private static VFXNodeController AddOperator(ShortcutArguments args, Type type)
        {
            if (args.context is VFXViewWindow window)
            {
               return window.graphView.AddOperator(type);
            }

            return null;
        }

        private static VFXNodeController AddInlineOperator(ShortcutArguments args, Type type)
        {
            if (AddOperator(args, typeof(VFXInlineOperator)) is var controller)
            {
                controller.model.SetSettingValue("m_Type", new SerializableType(type));

                return controller;
            }

            return null;
        }

        [Shortcut("Visual Effect Graph/Add Node: Substract", typeof(VFXViewWindow), KeyCode.S, ShortcutModifiers.Alt)] static void AddSubtractOperator(ShortcutArguments args) => AddOperator(args, typeof(Subtract));
        [Shortcut("Visual Effect Graph/Add Node: Multiply", typeof(VFXViewWindow), KeyCode.M, ShortcutModifiers.Alt)] static void AddMultiplyOperator(ShortcutArguments args) => AddOperator(args, typeof(Multiply));
        [Shortcut("Visual Effect Graph/Add Node: Add", typeof(VFXViewWindow), KeyCode.A, ShortcutModifiers.Alt)] static void AddAddOperator(ShortcutArguments args) => AddOperator(args, typeof(Add));
        [Shortcut("Visual Effect Graph/Add Node: Lerp", typeof(VFXViewWindow), KeyCode.L, ShortcutModifiers.Alt)] static void AddLerpOperator(ShortcutArguments args) => AddOperator(args, typeof(Lerp));
        [Shortcut("Visual Effect Graph/Add Node: Divide", typeof(VFXViewWindow), KeyCode.D, ShortcutModifiers.Alt)] static void AddDivideOperator(ShortcutArguments args) => AddOperator(args, typeof(Divide));

        [Shortcut("Visual Effect Graph/Add Node: Float", typeof(VFXViewWindow), KeyCode.Alpha1, ShortcutModifiers.Alt)] static void AddFloatOperator(ShortcutArguments args) => AddInlineOperator(args, typeof(float));
        [Shortcut("Visual Effect Graph/Add Node: Vector 2", typeof(VFXViewWindow), KeyCode.Alpha2, ShortcutModifiers.Alt)] static void AddVector2Operator(ShortcutArguments args) => AddInlineOperator(args, typeof(Vector2));
        [Shortcut("Visual Effect Graph/Add Node: Vector 3", typeof(VFXViewWindow), KeyCode.Alpha3, ShortcutModifiers.Alt)] static void AddVector3Operator(ShortcutArguments args) => AddInlineOperator(args, typeof(Vector3));
        [Shortcut("Visual Effect Graph/Add Node: Vector 4", typeof(VFXViewWindow), KeyCode.Alpha4, ShortcutModifiers.Alt)] static void AddVector4Operator(ShortcutArguments args) => AddInlineOperator(args, typeof(Vector4));
        [Shortcut("Visual Effect Graph/Add Node: Age over lifetime", typeof(VFXViewWindow), KeyCode.O, ShortcutModifiers.Alt)] static void AddAgeOverLifeTimeOperator(ShortcutArguments args) => AddOperator(args, typeof(AgeOverLifetime));
        [Shortcut("Visual Effect Graph/Add Node: VFX Time", typeof(VFXViewWindow), KeyCode.T, ShortcutModifiers.Alt)] static void AddVFXTimeOperator(ShortcutArguments args) => AddOperator(args, typeof(VFXDynamicBuiltInParameter));
        [Shortcut("Visual Effect Graph/Add Node: Random Number", typeof(VFXViewWindow), KeyCode.F, ShortcutModifiers.Alt)] static void AddRandomNumberOperator(ShortcutArguments args) => AddOperator(args, typeof(Random));
        [Shortcut("Visual Effect Graph/Add Node: One Minus", typeof(VFXViewWindow), KeyCode.I, ShortcutModifiers.Alt)] static void AddOneMinusOperator(ShortcutArguments args) => AddOperator(args, typeof(OneMinus));
        [Shortcut("Visual Effect Graph/Add Node: Saturate", typeof(VFXViewWindow), KeyCode.Q, ShortcutModifiers.Alt)] static void AddSaturateOperator(ShortcutArguments args) => AddOperator(args, typeof(Saturate));
        [Shortcut("Visual Effect Graph/Add Node: Custom HLSL", typeof(VFXViewWindow), KeyCode.H, ShortcutModifiers.Alt)] static void AddCustomHLSLOperator(ShortcutArguments args) => AddOperator(args, typeof(CustomHLSL));
        [Shortcut("Visual Effect Graph/Add Node: Sample Curve", typeof(VFXViewWindow), KeyCode.U, ShortcutModifiers.Alt)] static void AddSampleCurveOperator(ShortcutArguments args) => AddOperator(args, typeof(SampleCurve));
        [Shortcut("Visual Effect Graph/Add Node: Sample Gradient", typeof(VFXViewWindow), KeyCode.G, ShortcutModifiers.Alt)] static void AddSampleGradientOperator(ShortcutArguments args) => AddOperator(args, typeof(SampleGradient));
        [Shortcut("Visual Effect Graph/Add Node: Power", typeof(VFXViewWindow), KeyCode.P, ShortcutModifiers.Alt)] static void AddPowerOperator(ShortcutArguments args) => AddOperator(args, typeof(Power));
        [Shortcut("Visual Effect Graph/Add Node: Floor", typeof(VFXViewWindow), KeyCode.LeftBracket, ShortcutModifiers.Alt)] static void AddFloorOperator(ShortcutArguments args) => AddOperator(args, typeof(Floor));
        [Shortcut("Visual Effect Graph/Add Node: Ceil", typeof(VFXViewWindow), KeyCode.RightBracket, ShortcutModifiers.Alt)] static void AddCeilOperator(ShortcutArguments args) => AddOperator(args, typeof(Ceiling));
        [Shortcut("Visual Effect Graph/Add Node: Clamp", typeof(VFXViewWindow), KeyCode.Equals, ShortcutModifiers.Alt)] static void AddClampOperator(ShortcutArguments args) => AddOperator(args, typeof(Clamp));
        [Shortcut("Visual Effect Graph/Add Node: Minimum", typeof(VFXViewWindow), KeyCode.B, ShortcutModifiers.Alt)] static void AddMinimumOperator(ShortcutArguments args) => AddOperator(args, typeof(Minimum));
        [Shortcut("Visual Effect Graph/Add Node: Maximum", typeof(VFXViewWindow), KeyCode.N, ShortcutModifiers.Alt)] static void AddMaximumOperator(ShortcutArguments args) => AddOperator(args, typeof(Maximum));
        [Shortcut("Visual Effect Graph/Add Node: Smoothstep", typeof(VFXViewWindow), KeyCode.Quote, ShortcutModifiers.Alt)] static void AddSmoothstepOperator(ShortcutArguments args) => AddOperator(args, typeof(Smoothstep));
        [Shortcut("Visual Effect Graph/Add Node: Remap", typeof(VFXViewWindow), KeyCode.R, ShortcutModifiers.Alt)] static void AddRemapOperator(ShortcutArguments args) => AddOperator(args, typeof(Remap));
        [Shortcut("Visual Effect Graph/Add Node: Step", typeof(VFXViewWindow), KeyCode.J, ShortcutModifiers.Alt)] static void AddStepOperator(ShortcutArguments args) => AddOperator(args, typeof(Step));
        [Shortcut("Visual Effect Graph/Add Node: Absolute", typeof(VFXViewWindow), KeyCode.Backslash, ShortcutModifiers.Alt)] static void AddAbsoluteOperator(ShortcutArguments args) => AddOperator(args, typeof(Absolute));
        [Shortcut("Visual Effect Graph/Add Node: Fraction", typeof(VFXViewWindow), KeyCode.Slash, ShortcutModifiers.Alt)] static void AddFractionOperator(ShortcutArguments args) => AddOperator(args, typeof(Fractional));
        [Shortcut("Visual Effect Graph/Add Node: Modulo", typeof(VFXViewWindow), KeyCode.Alpha5, ShortcutModifiers.Alt)] static void AddModuloOperator(ShortcutArguments args) => AddOperator(args, typeof(Modulo));
        [Shortcut("Visual Effect Graph/Add Node: Compare", typeof(VFXViewWindow), KeyCode.K, ShortcutModifiers.Alt)] static void AddCompareOperator(ShortcutArguments args) => AddOperator(args, typeof(Condition));
        [Shortcut("Visual Effect Graph/Add Node: Branch", typeof(VFXViewWindow), KeyCode.Y, ShortcutModifiers.Alt)] static void AddBranchOperator(ShortcutArguments args) => AddOperator(args, typeof(Branch));
        [Shortcut("Visual Effect Graph/Add Node: Swizzle", typeof(VFXViewWindow), KeyCode.W, ShortcutModifiers.Alt)] static void AddSwizzleOperator(ShortcutArguments args) => AddOperator(args, typeof(Swizzle));
        [Shortcut("Visual Effect Graph/Add Node: Normalize", typeof(VFXViewWindow), KeyCode.Z, ShortcutModifiers.Alt)] static void AddNormalizeOperator(ShortcutArguments args) => AddOperator(args, typeof(Normalize));
        [Shortcut("Visual Effect Graph/Add Node: Cross Product", typeof(VFXViewWindow), KeyCode.X, ShortcutModifiers.Alt)] static void AddCrossProductOperator(ShortcutArguments args) => AddOperator(args, typeof(CrossProduct));
        [Shortcut("Visual Effect Graph/Add Node: Dot Product", typeof(VFXViewWindow), KeyCode.Period, ShortcutModifiers.Alt)] static void AddDotProductOperator(ShortcutArguments args) => AddOperator(args, typeof(DotProduct));
        [Shortcut("Visual Effect Graph/Add Node: Negate", typeof(VFXViewWindow), KeyCode.Minus, ShortcutModifiers.Alt)] static void AddDotNegateOperator(ShortcutArguments args) => AddOperator(args, typeof(Negate));
        [Shortcut("Visual Effect Graph/Add Node: Sample Texture 2D", typeof(VFXViewWindow), KeyCode.E, ShortcutModifiers.Alt)] static void AddSampleTexture2DOperator(ShortcutArguments args) => AddOperator(args, typeof(SampleTexture2D));
        [Shortcut("Visual Effect Graph/Add Node: Color", typeof(VFXViewWindow), KeyCode.C, ShortcutModifiers.Alt)] static void AddColorOperator(ShortcutArguments args) => AddInlineOperator(args, typeof(Color));
    }
}
