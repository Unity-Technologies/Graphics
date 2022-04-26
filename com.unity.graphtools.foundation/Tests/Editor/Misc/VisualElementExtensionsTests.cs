using System;
using NUnit.Framework;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.Misc
{
    [TestFixture]
    class VisualElementExtensionsTests
    {
        const string k_Prefix = "class-prefix-";
        const string k_Suffix = "and-suffix";

        [TestCase(new string [] {"class-a", k_Prefix + "-a"}, new string [] {"class-a", k_Prefix + k_Suffix})]
        [TestCase(new string [] {"class-a"}, new string [] {"class-a", k_Prefix + k_Suffix})]
        [TestCase(new string [] {"class-a", k_Prefix + k_Suffix}, new string [] {"class-a", k_Prefix + k_Suffix})]
        [TestCase(new string [] {"class-a", k_Prefix + "-a", k_Prefix + "-b", k_Prefix + "-c"}, new string [] {"class-a", k_Prefix + k_Suffix})]
        public void PrefixEnableInClassListWorks(string[] initialClasses, string[] expectedClasses)
        {
            var ve = new VisualElement();
            foreach (var c in initialClasses)
            {
                ve.AddToClassList(c);
            }

            ve.PrefixEnableInClassList(k_Prefix, k_Suffix);

            Assert.That(ve.GetClasses(), Is.EqualTo(expectedClasses));
        }
    }
}
