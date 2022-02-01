using NUnit.Framework;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.Misc
{
    [TestFixture]
    public class PreferencesTests
    {
        const string k_Prefix = "GTFPreferenceTests.";
        const string k_ObsoleteName = "ObsoleteName";

        public class MyIntPref : IntPref
        {
            public static readonly MyIntPref MyPref1 = new MyIntPref(k_ToolBasePrefId + 1, nameof(MyPref1), new[] { k_ObsoleteName });

            protected MyIntPref(int id, string name, string[] obsoleteNames = null)
                : base(id, name, obsoleteNames) {}
        }

        public sealed class MyPreferences : Preferences
        {
            public static MyPreferences CreatePreferences()
            {
                var preferences = new MyPreferences();
                preferences.Initialize<BoolPref, MyIntPref, StringPref>();
                return preferences;
            }

            MyPreferences() : base(k_Prefix) {}

            protected override void SetDefaultValues()
            {
                base.SetDefaultValues();
                SetIntNoEditorUpdate(MyIntPref.MyPref1, 21);
            }
        }

        [Test]
        public void TestSettingValueUpdatesEditorPrefs()
        {
            EditorPrefs.DeleteKey(k_Prefix + k_ObsoleteName);
            EditorPrefs.DeleteKey(k_Prefix + nameof(MyIntPref.MyPref1));

            var preferences = MyPreferences.CreatePreferences();
            Assert.IsFalse(EditorPrefs.HasKey(k_Prefix + k_ObsoleteName),
                "Unexpected preference key " + k_Prefix + k_ObsoleteName);
            Assert.IsFalse(EditorPrefs.HasKey(k_Prefix + nameof(MyIntPref.MyPref1)),
                "Unexpected preference key " + k_Prefix + nameof(MyIntPref.MyPref1));

            preferences.SetInt(MyIntPref.MyPref1, 64);

            Assert.AreEqual(64, EditorPrefs.GetInt(k_Prefix + nameof(MyIntPref.MyPref1)), "Unexpected preference value.");
            Assert.IsFalse(EditorPrefs.HasKey(k_Prefix + k_ObsoleteName), "Unexpected preference key.");
        }

#if UNITY_2021_2_OR_NEWER
        [Ignore("Bug with EditorPrefs in 2021.2 when prefs file is missing (like when running on Yamato). (https://fogbugz.unity3d.com/f/cases/1307252/)", Until = "2021-03-01")]
#endif
        [Test]
        public void TestPreferenceWithObsoleteNameIsLoadedCorrectly()
        {
            EditorPrefs.DeleteKey(k_Prefix + k_ObsoleteName);
            EditorPrefs.DeleteKey(k_Prefix + nameof(MyIntPref.MyPref1));

            EditorPrefs.SetInt(k_Prefix + k_ObsoleteName, 42);
            // Check that the value is in the prefs.
            Assert.IsTrue(EditorPrefs.HasKey(k_Prefix + k_ObsoleteName),
                "Missing preference key " + k_Prefix + k_ObsoleteName);
            Assert.IsFalse(EditorPrefs.HasKey(k_Prefix + nameof(MyIntPref.MyPref1)),
                "Unexpected preference key " + k_Prefix + nameof(MyIntPref.MyPref1));
            Assert.AreEqual(-100, EditorPrefs.GetInt(k_Prefix + nameof(MyIntPref.MyPref1), -100), "Unexpected initial value.");
            Assert.AreEqual(42, EditorPrefs.GetInt(k_Prefix + k_ObsoleteName), "Unexpected initial value.");

            var preferences = MyPreferences.CreatePreferences();
            var value = preferences.GetInt(MyIntPref.MyPref1);
            // In spite of the default value being 21, MyIntPref.MyPref1 should take the value of the obsolete key.
            Assert.AreEqual(42, value, "Value is not the one of the obsolete key.");
        }

#if UNITY_2021_2_OR_NEWER
        [Ignore("Bug with EditorPrefs in 2021.2 when prefs file is missing (like when running on Yamato). (https://fogbugz.unity3d.com/f/cases/1307252/)", Until = "2021-03-01")]
#endif
        [Test]
        public void TestSettingValueOfPrefWithObsoleteNamesRemoveObsoleteKeys()
        {
            EditorPrefs.DeleteKey(k_Prefix + k_ObsoleteName);
            EditorPrefs.DeleteKey(k_Prefix + nameof(MyIntPref.MyPref1));

            EditorPrefs.SetInt(k_Prefix + k_ObsoleteName, 42);
            // Check that the value is in the prefs.
            Assert.IsTrue(EditorPrefs.HasKey(k_Prefix + k_ObsoleteName),
                "Missing preference key " + k_Prefix + k_ObsoleteName);
            Assert.IsFalse(EditorPrefs.HasKey(k_Prefix + nameof(MyIntPref.MyPref1)),
                "Unexpected preference key " + k_Prefix + nameof(MyIntPref.MyPref1));
            Assert.AreEqual(42, EditorPrefs.GetInt(k_Prefix + k_ObsoleteName), "Unexpected initial value.");

            var preferences = MyPreferences.CreatePreferences();
            preferences.SetInt(MyIntPref.MyPref1, 28);
            var value = preferences.GetInt(MyIntPref.MyPref1);
            Assert.AreEqual(28, value);

            Assert.IsFalse(EditorPrefs.HasKey(k_Prefix + k_ObsoleteName));
        }
    }
}
