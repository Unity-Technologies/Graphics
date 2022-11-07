using NUnit.Framework;
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering.Tests
{
    partial class RenderingDebuggerTests
    {
        private static void PerformUndoRedoGeneric<TDebugState, T>(UnityEngine.Rendering.DebugUI.IValueField field, T defaultValue, T valueToSet)
            where TDebugState : DebugState<T>, new()
        {
            DebugState<T> state = ScriptableObject.CreateInstance<TDebugState>();
            state.SetValue(defaultValue, field);

            Undo.RecordObject(state, nameof(PerformUndoRedoGeneric));
            state.SetValue(valueToSet, field);

            Undo.PerformUndo();
            Assert.AreEqual(defaultValue, state.value);

            Undo.PerformRedo();
            Assert.AreEqual(valueToSet, state.value);
        }

        public class BitFieldTests<T> : UnityEngine.Rendering.DebugUI.BitField
        {
            public BitFieldTests()
            {
                enumType = typeof(T);
            }
        }

        public class AutoEnumFieldTest : UnityEngine.Rendering.DebugUI.EnumField
        {
            public AutoEnumFieldTest()
            {
                autoEnum = typeof(LightType);
            }
        }

        public class PathEnumFieldTest : UnityEngine.Rendering.DebugUI.EnumField
        {
            public PathEnumFieldTest()
            {
                enumNames = new GUIContent[] { new GUIContent("Item1"), new GUIContent("Catergory/Item1"), new GUIContent("Catergory/Item2") };
                enumValues = new int[] { 0, 2, 4 };
            }
        }

        static TestCaseData[] s_TestCaseDatas =
        {
            new TestCaseData(typeof(DebugStateEnum), typeof(UnityEngine.Rendering.DebugUI.EnumField), typeof(int), 0, 2)
                .SetName("Undo/Redo works for Enums"),
            new TestCaseData(typeof(DebugStateEnum), typeof(AutoEnumFieldTest), typeof(int), LightType.Disc, LightType.Spot)
                .SetName("Undo/Redo works for Auto Enums"),
            new TestCaseData(typeof(DebugStateEnum), typeof(PathEnumFieldTest), typeof(int), 0, 4)
                .SetName("Undo/Redo works for Path Enums"),
            new TestCaseData(typeof(DebugStateBool), typeof(UnityEngine.Rendering.DebugUI.BoolField), typeof(bool), false, true)
                .SetName("Undo/Redo works for Booleans"),
            new TestCaseData(typeof(DebugStateColor), typeof(UnityEngine.Rendering.DebugUI.ColorField), typeof(Color), Color.green, Color.red)
                .SetName("Undo/Redo works for Colors"),
            new TestCaseData(typeof(DebugStateFloat), typeof(UnityEngine.Rendering.DebugUI.FloatField), typeof(float), 1.0f, 5.0f)
                .SetName("Undo/Redo works for Floats"),
            new TestCaseData(typeof(DebugStateInt), typeof(UnityEngine.Rendering.DebugUI.IntField), typeof(int), -1, 5)
                .SetName("Undo/Redo works for integers"),
            new TestCaseData(typeof(DebugStateUInt), typeof(UnityEngine.Rendering.DebugUI.UIntField), typeof(uint), 1u, 5u)
                .SetName("Undo/Redo works for unsigned integers"),
            new TestCaseData(typeof(DebugStateVector2), typeof(UnityEngine.Rendering.DebugUI.Vector2Field), typeof(Vector2), Vector2.zero, Vector2.up)
                .SetName("Undo/Redo works for vector 2"),
            new TestCaseData(typeof(DebugStateVector3), typeof(UnityEngine.Rendering.DebugUI.Vector3Field), typeof(Vector3), Vector3.zero, Vector3.up)
                .SetName("Undo/Redo works for vector 3"),
            new TestCaseData(typeof(DebugStateVector4), typeof(UnityEngine.Rendering.DebugUI.Vector4Field), typeof(Vector4), Vector4.zero, Vector4.one)
                .SetName("Undo/Redo works for vector 4"),
            new TestCaseData(typeof(DebugStateFlags), typeof(BitFieldTests<UnityEngine.Rendering.DebugUI.Flags>), typeof(Enum), UnityEngine.Rendering.DebugUI.Flags.EditorOnly, UnityEngine.Rendering.DebugUI.Flags.RuntimeOnly)
                .SetName("Undo/Redo works for bit fields"),
        };

        [Test, TestCaseSource(nameof(s_TestCaseDatas))]
        public void PerformUndoRedo(Type debugStateType, Type widgetType, Type innerType, object defaultValue, object changeValue)
        {
            var widget = Activator.CreateInstance(widgetType);
            var performUndoRedoGeneric = GetType()
                .GetMethod(nameof(PerformUndoRedoGeneric), System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)
                .MakeGenericMethod(new[] { debugStateType, innerType } );

            object[] args = new object[] { widget, defaultValue, changeValue };
            performUndoRedoGeneric.Invoke(null, args);
        }
    }
}
