using System.Collections.Generic;
using UnityEngine;

namespace UnityEngine.Experimental
{
#if UNITY_EDITOR
    public class InputManagerEntry
    {
        public enum Kind { KeyOrButton, Mouse, Axis }
        public enum Axis { X, Y, Third, Fourth, Fifth, Sixth, Seventh, Eigth }
        public enum Joy { All, First, Second }

        public string	name = "";
        public string	desc = "";
        public string	btnNegative = "";
        public string	btnPositive = "";
        public string	altBtnNegative = "";
        public string	altBtnPositive = "";
        public float	gravity = 0.0f;
        public float	deadZone = 0.0f;
        public float	sensitivity = 0.0f;
        public bool		snap = false;
        public bool		invert = false;
        public Kind		kind = Kind.Axis;
        public Axis		axis = Axis.X;
        public Joy		joystick = Joy.All;
    }

    public class InputRegistering
    {

        static bool InputAlreadyRegistered(string name, InputManagerEntry.Kind kind, UnityEditor.SerializedProperty spAxes)
        {
            for (var i = 0; i < spAxes.arraySize; ++i)
            {
                var spAxis = spAxes.GetArrayElementAtIndex(i);
                var axisName = spAxis.FindPropertyRelative("m_Name").stringValue;
                var kindValue = spAxis.FindPropertyRelative("type").intValue;
                if (axisName == name && (int)kind == kindValue)
                    return true;
            }

            return false;
        }

        static void WriteEntry(UnityEditor.SerializedProperty spAxes, InputManagerEntry entry)
        {
            if (InputAlreadyRegistered(entry.name, entry.kind, spAxes))
                return;

            spAxes.InsertArrayElementAtIndex(spAxes.arraySize);
            var spAxis = spAxes.GetArrayElementAtIndex(spAxes.arraySize - 1);
            spAxis.FindPropertyRelative("m_Name").stringValue = entry.name;
            spAxis.FindPropertyRelative("descriptiveName").stringValue = entry.desc;
            spAxis.FindPropertyRelative("negativeButton").stringValue = entry.btnNegative;
            spAxis.FindPropertyRelative("altNegativeButton").stringValue = entry.altBtnNegative;
            spAxis.FindPropertyRelative("positiveButton").stringValue = entry.btnPositive;
            spAxis.FindPropertyRelative("altPositiveButton").stringValue = entry.altBtnPositive;
            spAxis.FindPropertyRelative("gravity").floatValue = entry.gravity;
            spAxis.FindPropertyRelative("dead").floatValue = entry.deadZone;
            spAxis.FindPropertyRelative("sensitivity").floatValue = entry.sensitivity;
            spAxis.FindPropertyRelative("snap").boolValue = entry.snap;
            spAxis.FindPropertyRelative("invert").boolValue = entry.invert;
            spAxis.FindPropertyRelative("type").intValue = (int)entry.kind;
            spAxis.FindPropertyRelative("axis").intValue = (int)entry.axis;
            spAxis.FindPropertyRelative("joyNum").intValue = (int)entry.joystick;
        }

        static public void RegisterInputs(List<InputManagerEntry> entries)
        {
            // Grab reference to input manager
            var currentSelection = UnityEditor.Selection.activeObject;
            UnityEditor.EditorApplication.ExecuteMenuItem("Edit/Project Settings/Input");
            var inputManager = UnityEditor.Selection.activeObject;

            // Wrap in serialized object
            var soInputManager = new UnityEditor.SerializedObject(inputManager);
            var spAxes = soInputManager.FindProperty("m_Axes");

            foreach(InputManagerEntry entry in entries)
            {
                WriteEntry(spAxes, entry);
            }

            // Commit
            soInputManager.ApplyModifiedProperties();

            UnityEditor.Selection.activeObject = currentSelection;
        }
    }
#endif
}