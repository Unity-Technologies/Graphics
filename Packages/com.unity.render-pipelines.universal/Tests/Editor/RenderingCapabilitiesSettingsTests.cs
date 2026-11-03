using NUnit.Framework;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal.Tests
{
    class RenderingCapabilitiesSettingsTests
    {
        // The setting is only visible in Developer Mode, but its value must not depend on it. A player reads the
        // serialized field, and the shader define follows the editor value: an editor that read it as off outside
        // Developer Mode would build loose-uniform shaders for players that push the constant buffer.
        [Test]
        public void UseGlobalConstantBuffer_ReturnsTheSerializedValue_WhateverTheDeveloperMode()
        {
            var settings = new RenderingCapabilitiesSettings();
            Assert.That(settings.useGlobalConstantBuffer, Is.False);

            settings.useGlobalConstantBuffer = true;

            Assert.That(settings.useGlobalConstantBuffer, Is.True,
                $"The editor must read the serialized value (Developer Mode is {(Unsupported.IsDeveloperMode() ? "on" : "off")}).");
        }
    }
}
