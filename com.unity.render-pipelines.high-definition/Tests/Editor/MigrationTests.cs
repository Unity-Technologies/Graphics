using NUnit.Framework;

namespace UnityEngine.Rendering.HighDefinition.Tests
{
    class MigrationTests
    {
        class MigrableComponent : IVersionable<MigrableComponent.Version>
        {
            public enum Version
            {
                // 0 is default
                One = 1, Two = 2, Three = 3, Four = 4, Five = 5
            }

            public Version version;
            public int intValue;
            public float floatValue;
            Version IVersionable<Version>.version { get => version; set => version = value; }
        }

        [Test]
        public void MigrationStepMigrate()
        {
            var step = MigrationStep.New(MigrableComponent.Version.One, (MigrableComponent c) =>
            {
                ++c.intValue;
            });

            var instance = new MigrableComponent { intValue = 1 };
            step.Migrate(instance);
            Assert.AreEqual(2, instance.intValue);
            Assert.AreEqual(MigrableComponent.Version.One, instance.version);

            step.Migrate(instance);
            Assert.AreEqual(2, instance.intValue);
            Assert.AreEqual(MigrableComponent.Version.One, instance.version);

            step = MigrationStep.New(MigrableComponent.Version.Two, (MigrableComponent c) =>
            {
                ++c.intValue;
            });
            instance.version = MigrableComponent.Version.Three;
            step.Migrate(instance);
            Assert.AreEqual(2, instance.intValue);
            Assert.AreEqual(MigrableComponent.Version.Three, instance.version);
        }

        [Test]
        public void MigrationDescriptionMigrate()
        {
            var description = MigrationDescription.New(
                MigrationStep.New(MigrableComponent.Version.Two, (MigrableComponent c) => { ++c.intValue; }),
                MigrationStep.New(MigrableComponent.Version.Three, (MigrableComponent c) => { ++c.floatValue; }),
                MigrationStep.New(MigrableComponent.Version.Five, (MigrableComponent c) => { c.intValue += 2; })
            );

            var instance = new MigrableComponent { intValue = 1, floatValue = 2, version = 0 };
            description.Migrate(instance);
            Assert.AreEqual(MigrableComponent.Version.Five, instance.version);
            Assert.AreEqual(4, instance.intValue);
            Assert.AreEqual(3.0f, instance.floatValue);

            description.Migrate(instance);
            Assert.AreEqual(MigrableComponent.Version.Five, instance.version);
            Assert.AreEqual(4, instance.intValue);
            Assert.AreEqual(3.0f, instance.floatValue);

            instance.version = MigrableComponent.Version.Four;
            description.Migrate(instance);
            Assert.AreEqual(MigrableComponent.Version.Five, instance.version);
            Assert.AreEqual(6, instance.intValue);
            Assert.AreEqual(3.0f, instance.floatValue);
        }
    }
}
