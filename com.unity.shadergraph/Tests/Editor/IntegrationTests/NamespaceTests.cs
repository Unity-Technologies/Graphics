using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Internal;

namespace UnityEditor.ShaderGraph.UnitTests
{
    [TestFixture]
    internal class NamespaceTests
    {

        [Test]
        public void NoDanglingNamespaces()
        {
            var myAssembly = Assembly.GetAssembly(typeof(AbstractMaterialNode));
            HashSet<string> namespaces = new HashSet<string>();
            foreach (var theType in myAssembly.GetTypes().Where(t => !string.IsNullOrEmpty(t.Namespace)))
            {
                namespaces.Add(theType.Namespace);
            }
            var invalidNames = new List<string>();
            foreach (var name in namespaces)
            {
                if(name.Contains("ShaderGraph"))
                    continue;
                if (name.Contains("UnityEditor"))
                    continue;
                if(name.Contains("UnityEngine"))
                    continue;

                invalidNames.Add(name);
            }
            Assert.IsEmpty(invalidNames, "The following namespaces are invalid for the Shader Graph package:\n" + string.Join("\n", invalidNames));
        }
    }
}
