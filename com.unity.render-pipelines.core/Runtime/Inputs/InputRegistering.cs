using System.Collections.Generic;

namespace UnityEngine
{
#if UNITY_EDITOR
    using UnityEditor;

    public class InputManagerEntry
    {
        public enum Kind { KeyOrButton, Mouse, Axis }
        public enum Axis { X, Y, Third, Fourth, Fifth, Sixth, Seventh, Eigth }
        public enum Joy { All, First, Second }

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

        internal bool IsEqual((string name, InputManagerEntry.Kind kind) partialEntry)
            => this.name == partialEntry.name && this.kind == partialEntry.kind;

        internal bool IsEqual(InputManagerEntry other)
            => this.name == other.name && this.kind == other.kind;
    }

    public static class InputRegistering
    {
        static List<InputManagerEntry> s_PendingInputsToRegister = new List<InputManagerEntry>();

        static bool havePendingOperation => s_PendingInputsToRegister.Count > 0;

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
            int endOfCurrentInputList = spAxes.arraySize;
            spAxes.arraySize = endOfCurrentInputList + newEntries.Count;

            SerializedProperty spAxis = spAxes.GetArrayElementAtIndex(endOfCurrentInputList - 1);
            spAxis.Next(false);
            for (int i = 0; i < newEntries.Count; ++i, spAxis.Next(false))
                CopyEntry(spAxis, newEntries[i]);
        }

        // Get a representation of the already registered inputs
        static List<(string name, InputManagerEntry.Kind kind)> GetCachedInputs(SerializedProperty spAxes)
        {
            int size = spAxes.arraySize;
            List<(string name, InputManagerEntry.Kind kind)> result = new List<(string name, InputManagerEntry.Kind kind)>(size);

            SerializedProperty spAxis = spAxes.GetArrayElementAtIndex(0);
            for (int i = 0; i < size; ++i, spAxis.Next(false))
                result.Add((spAxis.FindPropertyRelative("m_Name").stringValue, (InputManagerEntry.Kind)spAxis.FindPropertyRelative("type").intValue));
            return result;
        }

        static void MakeUniquePendingInputsToRegister()
        {
            for (int pendingIndex = s_PendingInputsToRegister.Count - 1; pendingIndex > 0; --pendingIndex)
            {
                InputManagerEntry pendingEntry = s_PendingInputsToRegister[pendingIndex];
                int checkedIndex = pendingIndex - 1;
                for (; checkedIndex > -1 && !pendingEntry.IsEqual(s_PendingInputsToRegister[checkedIndex]); --checkedIndex) ;

                if (checkedIndex == -1)
                    continue;

                // There is a duplicate entry in PendingInputesToRegister.
                // Debug.LogWarning($"Two entries with same name and kind are tryed to be added at same time. Only first occurence is kept. Name:{pendingEntry.name} Kind:{pendingEntry.kind}");

                // Keep only first.
                // Counting decreasingly will have no impact on index before pendingIndex. So we can safely remove it.
                s_PendingInputsToRegister.RemoveAt(pendingIndex);
            }
        }

        static void RemovePendingInputsToAddThatAreAlreadyRegistered(List<(string name, InputManagerEntry.Kind kind)> cachedEntries, List<InputManagerEntry> newEntries)
        {
            for (int newIndex = newEntries.Count - 1; newIndex >= 0; --newIndex)
            {
                var newEntry = newEntries[newIndex];
                int checkedIndex = cachedEntries.Count - 1;
                for (; checkedIndex > -1 && !newEntry.IsEqual(cachedEntries[checkedIndex]); --checkedIndex) ;

                if (checkedIndex == -1)
                    continue;

                // There is a already a cached entry that correspond.
                // Debug.LogWarning($"Another entry with same name and kind already exist. Skiping this one. Name:{newEntry.name} Kind:{newEntry.kind}");

                // Keep only first.
                // Counting decreasingly will have no impact on index before pendingIndex. So we can safely remove it.
                s_PendingInputsToRegister.RemoveAt(newIndex);
            }
        }

        static void DelayedRegisterInput()
        {
            // Exit quickly if nothing more to register
            // (case when several different class try to register, only first call will do all)
            if (!havePendingOperation)
                return;

            // Grab reference to input manager
            var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/InputManager.asset");
            // Temporary fix. This happens some time with HDRP init when it's called before asset database is initialized (probably related to package load order).
            if (assets.Length == 0)
                return;

            var inputManager = assets[0];

            // Wrap in serialized object to access c++ fields
            var soInputManager = new SerializedObject(inputManager);
            var spAxes = soInputManager.FindProperty("m_Axes");

            // At this point, we assume that entries in spAxes are already unique.

            // Ensure no double entry are tried to be registered (trim early)
            MakeUniquePendingInputsToRegister();

            // Cache already existing entries to minimaly use serialization
            var cachedEntries = GetCachedInputs(spAxes);
            // And trim pending entries regarding already cached ones.
            RemovePendingInputsToAddThatAreAlreadyRegistered(cachedEntries, s_PendingInputsToRegister);

            // Add now unique entries
            AddEntriesWithoutCheck(spAxes, s_PendingInputsToRegister);

            // Commit
            soInputManager.ApplyModifiedProperties();
        }

        public static void RegisterInputs(List<InputManagerEntry> entries)
        {
#if ENABLE_INPUT_SYSTEM && ENABLE_INPUT_SYSTEM_PACKAGE
            Debug.LogWarning("Trying to add entry in the legacy InputManager but using InputSystem package. Skiping.");
            return;
#else
            s_PendingInputsToRegister.AddRange(entries);

            //delay the call in order to do only one pass event if several different class register inputs
            EditorApplication.delayCall += DelayedRegisterInput;
#endif
        }
    }
#endif
}
