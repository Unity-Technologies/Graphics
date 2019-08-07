using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace UnityEditor.VFX.Test
{
    class VFXSystemNamesTest
    {

        private class ContextSpawner : VFXContext
        {
            public ContextSpawner() : base(VFXContextType.Spawner) { }
        }

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
                "bar",
                "J'aime les panoramas",
                "Vous savez, moi je ne crois pas qu’il y ait de bonne ou de mauvaise situation." +
                "Moi, si je devais résumer ma vie aujourd’hui avec vous, je dirais que c’est d’abord des rencontres. " +
                "Des gens qui m’ont tendu la main, peut-être à un moment où je ne pouvais pas, où j’étais seul chez moi. " +
                "Et c’est assez curieux de se dire que les hasards, les rencontres forgent une destinée… " +
                "Parce que quand on a le goût de la chose, quand on a le goût de la chose bien faite, le beau geste, " +
                "parfois on ne trouve pas l’interlocuteur en face je dirais, le miroir qui vous aide à avancer. " +
                "Alors ça n’est pas mon cas, comme je disais là, puisque moi au contraire, j’ai pu : " +
                "et je dis merci à la vie, je lui dis merci, je chante la vie, je danse la vie… je ne suis qu’amour ! " +
                "Et finalement, quand beaucoup de gens aujourd’hui me disent \" Mais comment fais-tu pour avoir cette humanité ? \", " +
                "et bien je leur réponds très simplement, je leur dis que c’est ce goût de l’amour ce goût donc qui m’a poussé aujourd’hui à entreprendre une construction mécanique, " +
                "mais demain qui sait ? Peut-être simplement à me mettre au service de la communauté, à faire le don, le don de soi… "
            };

            var spawnerCount = names.Length / 2;
            var GPUSystemCount = names.Length - spawnerCount;

            List<VFXModel> systems = new List<VFXModel>();

            int i = 0;
            for (; i < spawnerCount; ++i)
            {

                var context = ScriptableObject.CreateInstance<ContextSpawner>();
                VFXSystemNames.SetSystemName(context, names[i]);
                systems.Add(context);
            }
            for (; i < spawnerCount + GPUSystemCount; ++i)
            {
                var data = ScriptableObject.CreateInstance<VFXDataParticle>();
                VFXSystemNames.SetSystemName(data, names[i]);
                systems.Add(data);
            }

            var systemNames = new VFXSystemNames();
            systemNames.Init(systems);
            var uniqueNames = systems.Select(system => systemNames.GetUniqueSystemName(system)).Where(name => !string.IsNullOrEmpty(name)).Distinct().ToList();

            Assert.IsTrue(uniqueNames.Count() == names.Length, "Some systems have the same name or are null or empty.");
        }
    }
}
