using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace UnityEditor.VFX.Test
{
    class VFXSystemNamesTest
    {
        [Test]
        public void UniqueSystemNames()
        {
            string[] names =
            {
                "foo",
                "bar",
                null,
                "foo",
                "bar",
                "foobar",
                "foobar (1)",
                "foobar (1)",
                null,
                "",
                VFXSystemNames.DefaultSystemName,
                VFXSystemNames.DefaultSystemName,
                "foo",
                "bar"
            };

            var spawnerCount = names.Length / 2;
            var GPUSystemCount = names.Length - spawnerCount;

            List<VFXModel> systems = new List<VFXModel>();

            int i = 0;
            for (; i < spawnerCount; ++i)
            {
                var context = new VFXContext(VFXContextType.Spawner);
                VFXSystemNames.SetSystemName(context, names[i]);
                systems.Add(context);
            }
            for (; i < spawnerCount + GPUSystemCount; ++i)
            {
                var data = new VFXDataParticle();
                VFXSystemNames.SetSystemName(data, names[i]);
                systems.Add(data);
            }

            var systemNames = new VFXSystemNames();
            systemNames.Init(systems);
            var uniqueNames = systems.Select(system => systemNames.GetUniqueSystemName(system)).Where(name => !string.IsNullOrEmpty(name)).Distinct();

            Assert.IsTrue(uniqueNames.Count() == names.Length, "Some system have the same name or are null or empty.");
        }
    }
}
