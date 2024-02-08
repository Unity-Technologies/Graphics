using System.Collections.Generic;
using System.Linq;

namespace UnityEngine
{
#if UNITY_EDITOR
    using UnityEditor;

    public class InputManagerEntry
    {
        public enum Kind
        {
            KeyOrButton,
            Mouse,
            Axis
        }

        public enum Axis
        {
            X,
            Y,
            Third,
            Fourth,
            Fifth,
            Sixth,
            Seventh,
            Eigth
        }

        public enum Joy
        {
            All,
            First,
            Second
        }

        public string name = "";
        public string desc = "";
        public string btnNegative = "";
        public string btnPositive = "";
        public string altBtnNegative = "";
        public string altBtnPositive = "";
        public float gravity = 0.0f;
        public float deadZone = 0.0f;
        public float sensitivity = 0.0f;
        public bool snap = false;
        public bool invert = false;
        public Kind kind = Kind.Axis;
        public Axis axis = Axis.X;
        public Joy joystick = Joy.All;
    }

    public static class InputRegistering
    {
        static void CopyEntry(SerializedProperty spAxis, InputManagerEntry entry)
        {
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

        static void AddEntriesWithoutCheck(SerializedProperty spAxes, List<InputManagerEntry> newEntries)
        {
            if (newEntries.Count == 0)
                return;

            int endOfCurrentInputList = spAxes.arraySize;
            spAxes.arraySize = endOfCurrentInputList + newEntries.Count;
            // Assignment to spAxes.arraySize resizes the spAxes array to at least 1 larger than it used to be, and 
            // therefore it is OK to use endOfCurrentInputList ("one-past-end of previous size") to get the array iterator.
            SerializedProperty spAxis = spAxes.GetArrayElementAtIndex(endOfCurrentInputList);
            for (int i = 0; i < newEntries.Count; ++i, spAxis.Next(false))
                CopyEntry(spAxis, newEntries[i]);
        }

        // Get a representation of the already registered inputs
        static List<(string name, InputManagerEntry.Kind kind)> GetCachedInputs(SerializedProperty spAxes)
        {
            int size = spAxes.arraySize;
            List<(string name, InputManagerEntry.Kind kind)> result = new List<(string name, InputManagerEntry.Kind kind)>(size);

            if (size == 0)
                return result;

            SerializedProperty spAxis = spAxes.GetArrayElementAtIndex(0);
            for (int i = 0; i < size; ++i, spAxis.Next(false))
                result.Add((spAxis.FindPropertyRelative("m_Name").stringValue, (InputManagerEntry.Kind)spAxis.FindPropertyRelative("type").intValue));
            return result;
        }

        internal static List<InputManagerEntry> GetEntriesWithoutDuplicates(List<InputManagerEntry> entries)
        {
            return entries
                .GroupBy(x => new { x.name, x.kind }) // Create groups { name, kind }
                .Select(y => y.First()) // Select first entry from each group, ignoring duplicates
                .ToList();
        }

        internal static List<InputManagerEntry> GetEntriesWithoutAlreadyRegistered(List<InputManagerEntry> entries, List<(string name, InputManagerEntry.Kind kind)> cachedEntries)
        {
            return entries
                .Where(entry => !cachedEntries.Any(cachedEntry => cachedEntry.name == entry.name && cachedEntry.kind == entry.kind))
                .ToList();
        }

        static SerializedObject GetInputManagerSO()
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/InputManager.asset");

            // Potentially there are multiple objects inside the Asset, we need to make sure we only grab the correct one.
            foreach (var obj in assets)
            {
                // Check the asset name is correct
                if (obj != null && obj.name == "InputManager")
                {
                    // There is an issue where multiple objects can be named InputManager for a
                    // period when they are added to the file, even if they are given unique names.
                    // In the editor we also check the type is InputManager to work around this, however
                    // that type is not available here. So instead we inspect the contents to confirm it's
                    // the object we expect.
                    var soInputManager = new SerializedObject(obj);
                    var spAxes = soInputManager.FindProperty("m_Axes");
                    if (spAxes != null)
                        return soInputManager;
                }
            }
            return null;
        }

        public static void RegisterInputs(List<InputManagerEntry> entries)
        {
#if ENABLE_INPUT_SYSTEM && ENABLE_INPUT_SYSTEM_PACKAGE
            Debug.LogWarning("Trying to add entry in the legacy InputManager but using InputSystem package. Skipping.");
            return;
#else
            // Grab reference to input manager
            var soInputManager = GetInputManagerSO();

            // Temporary fix. This happens some time with HDRP init when it's called before asset database is initialized (probably related to package load order).
            if (soInputManager == null)
                return;

            var spAxes = soInputManager.FindProperty("m_Axes");

            // At this point, we assume that entries in spAxes are already unique.

            // Ensure no double entry are tried to be registered (trim early)
            var uniqueEntries = GetEntriesWithoutDuplicates(entries);

            // Cache already existing entries to minimally use serialization
            var cachedEntries = GetCachedInputs(spAxes);

            // And trim pending entries regarding already cached ones.
            var uniqueNewEntries = GetEntriesWithoutAlreadyRegistered(uniqueEntries, cachedEntries);

            // Add now unique entries
            AddEntriesWithoutCheck(spAxes, uniqueNewEntries);

            // Commit
            soInputManager.ApplyModifiedProperties();
#endif
        }
    }
#endif
}
