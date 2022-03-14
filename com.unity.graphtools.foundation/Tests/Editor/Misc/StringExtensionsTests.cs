using System;
using NUnit.Framework;
using UnityEngine.GraphToolsFoundation.Overdrive;

// ReSharper disable AccessToStaticMemberViaDerivedType

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.Misc
{
    [TestFixture]
    class StringExtensionsTests
    {
        [TestCase("AUpperCamelCaseString", "A Upper Camel Case String")]
        [TestCase("aLowerCamelCaseString", "A Lower Camel Case String")]
        public void NificyTest(string value, string expected)
        {
            Assert.That(value.Nicify(), Is.EqualTo(expected));
        }

        [TestCase("Asd Qwe_Asd-rr", "Asd_Qwe_Asd_rr")]
        [TestCase("asd%-$yy", "asd___yy")]
        [TestCase("uu%yy", "uu_yy")]
        [TestCase("asd--qwe_", "asd__qwe_")]
        public void CodifyNameTest(string actual, string expected)
        {
            Assert.That(actual.CodifyStringInternal(), Is.EqualTo(expected));
        }
    }
}
