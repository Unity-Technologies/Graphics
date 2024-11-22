using System;
using System.Collections.Generic;
using UnityEditor.VFX.Block;
using UnityEditor.VFX.UI;
using UnityEngine;
using UnityEngine.VFX;

namespace UnityEditor.VFX
{
    partial struct VFXAttribute
    {
        private const string kBasicSimulationCategory = "#1Basic Simulation";
        private const string kAdvancedSimulationCategory = "#2Advanced Simulation";
        private const string kRenderingCategory = "#3Rendering";
        private const string kCollisionCategory = "#4Collision";
        private const string kStripCategory = "#5Strips";
        private const string kSystemCategory = "#6System";

        public static readonly float kDefaultSize = 0.1f;
        public static readonly VFXAttribute Seed = new VFXAttribute("seed", VFXValueType.Uint32, "A unique seed used for random number computations.") { category = kSystemCategory };
        public static readonly VFXAttribute OldPosition = new VFXAttribute("oldPosition", VFXValueType.Float3, "This attribute is a storage helper if you want to back up the current position of a particle, before integrating its velocity.", VFXVariadic.False, SpaceableType.Position) { category = kAdvancedSimulationCategory };
        public static readonly VFXAttribute Position = new VFXAttribute("position", VFXValueType.Float3, "The position of the particle expressed in the space (World or Local) of its parent system.", VFXVariadic.False, SpaceableType.Position) { category = kBasicSimulationCategory };
        public static readonly VFXAttribute Velocity = new VFXAttribute("velocity", VFXValueType.Float3, "The velocity of the particle expressed as a 3D Vector in the Space (World/Local) of the parent system.", VFXVariadic.False, SpaceableType.Vector) { category = kBasicSimulationCategory };
        public static readonly VFXAttribute Direction = new VFXAttribute("direction", VFXValue.Constant(new Vector3(0.0f, 0.0f, 1.0f)), "A vector3 attribute that can be used to store arbitrary data. By default, this attributes is set by numerous blocks like :Shape Position to store the Shapes’s normals.", VFXVariadic.False, SpaceableType.Vector) { category = kAdvancedSimulationCategory };
        public static readonly VFXAttribute Color = new VFXAttribute("color", VFXValue.Constant(Vector3.one), "The color of the particle.") { category = kRenderingCategory };
        public static readonly VFXAttribute Alpha = new VFXAttribute("alpha", VFXValue.Constant(1.0f), "The transparency value of the particle. Transparent particles with a value of 0 or less are invisible.") { category = kRenderingCategory };
        public static readonly VFXAttribute Size = new VFXAttribute("size", VFXValue.Constant(kDefaultSize), "The uniform size of the particles expressed in meter. If present, this attribute is multiplied by the scale attribute to get the final size of the rendered particles. The default value is 0.1.") { category = kRenderingCategory };
        public static readonly VFXAttribute ScaleX = new VFXAttribute("scaleX", VFXValue.Constant(1.0f), "The scale of the particle along the X axis, as a multiplier to its size.", VFXVariadic.BelongsToVariadic);
        public static readonly VFXAttribute ScaleY = new VFXAttribute("scaleY", VFXValue.Constant(1.0f), "The per-axis scale of the particle along the Y axis, as a multiplier to its size.", VFXVariadic.BelongsToVariadic);
        public static readonly VFXAttribute ScaleZ = new VFXAttribute("scaleZ", VFXValue.Constant(1.0f), "The per-axis scale of the particle along the Z axis, as a multiplier to its size.", VFXVariadic.BelongsToVariadic);
        public static readonly VFXAttribute Lifetime = new VFXAttribute("lifetime", VFXValue.Constant(1.0f), "Indicates how long the particle can stay alive. By default, If the particle’s age exceeds its lifetime, then the particle will be destroyed by the Update Context.") { category = kBasicSimulationCategory };
        public static readonly VFXAttribute Age = new VFXAttribute("age", VFXValueType.Float, "The age of the particles since it spawns, expressed in seconds. By default, age is updated by the Update Context that will also destroy the particle if the particle’s age exceeds its lifetime.") { category = kBasicSimulationCategory };
        public static readonly VFXAttribute AngleX = new VFXAttribute("angleX", VFXValueType.Float, "The particle angle per axis. For Camera-facing billboard particles, the Z axis is most likely the desired axis of rotation.", VFXVariadic.BelongsToVariadic);
        public static readonly VFXAttribute AngleY = new VFXAttribute("angleY", VFXValueType.Float, "The particle angle per axis. For Camera-facing billboard particles, the Z axis is most likely the desired axis of rotation.", VFXVariadic.BelongsToVariadic);
        public static readonly VFXAttribute AngleZ = new VFXAttribute("angleZ", VFXValueType.Float, "The particle angle per axis. For Camera-facing billboard particles, the Z axis is most likely the desired axis of rotation.", VFXVariadic.BelongsToVariadic);
        public static readonly VFXAttribute AngularVelocityX = new VFXAttribute("angularVelocityX", VFXValueType.Float, "The angular rotation of the particle, in degrees per second.", VFXVariadic.BelongsToVariadic);
        public static readonly VFXAttribute AngularVelocityY = new VFXAttribute("angularVelocityY", VFXValueType.Float, "The angular rotation of the particle, in degrees per second.", VFXVariadic.BelongsToVariadic);
        public static readonly VFXAttribute AngularVelocityZ = new VFXAttribute("angularVelocityZ", VFXValueType.Float, "The angular rotation of the particle, in degrees per second.", VFXVariadic.BelongsToVariadic);
        public static readonly VFXAttribute TexIndex = new VFXAttribute("texIndex", VFXValueType.Float, "The current frame index of the flipbook. This attribute is used if ‘UV Mode’ in the output is set to use flipbooks.") { category = kRenderingCategory };
        public static readonly VFXAttribute TexIndexBlend = new VFXAttribute("texIndexBlend", VFXValue.Constant(1.0f), "The next frame index of the flipbook, if flipbook frame blending is enabled.") { category = kRenderingCategory };
        public static readonly VFXAttribute MeshIndex = new VFXAttribute("meshIndex", VFXValueType.Uint32, "The current index of the mesh. This attribute determines which mesh to use when Mesh Count setting of a Mesh Output is higher than one.") { category = kRenderingCategory };
        public static readonly VFXAttribute PivotX = new VFXAttribute("pivotX", VFXValue.Constant(0.0f), "The point around which the particle rotates, moves, or is scaled. By default, this is the center of the particle.", VFXVariadic.BelongsToVariadic);
        public static readonly VFXAttribute PivotY = new VFXAttribute("pivotY", VFXValue.Constant(0.0f), "The point around which the particle rotates, moves, or is scaled. By default, this is the center of the particle.", VFXVariadic.BelongsToVariadic);
        public static readonly VFXAttribute PivotZ = new VFXAttribute("pivotZ", VFXValue.Constant(0.0f), "The point around which the particle rotates, moves, or is scaled. By default, this is the center of the particle.", VFXVariadic.BelongsToVariadic);
        public static readonly VFXAttribute ParticleId = new VFXAttribute("particleId", VFXValueType.Uint32, "Outputs the ID of the particle. Each particle gets assigned an incremental unique ID value when it is created.") { category = kSystemCategory };
        public static readonly VFXAttribute AxisX = new VFXAttribute("axisX", VFXValue.Constant(Vector3.right), "Determines which is the X (right-left) axis of the particle. This is used to properly orient the particle. The Orient Block in the update context is usually used to set up this attribute.", VFXVariadic.False, SpaceableType.Vector) { category = kRenderingCategory };
        public static readonly VFXAttribute AxisY = new VFXAttribute("axisY", VFXValue.Constant(Vector3.up), "Determines which is the Y (up-down) axis of the particle. This is used to properly orient the particle. The Orient Block in the update context is usually used to set up this attribute.", VFXVariadic.False, SpaceableType.Vector) { category = kRenderingCategory };
        public static readonly VFXAttribute AxisZ = new VFXAttribute("axisZ", VFXValue.Constant(Vector3.forward), "Determines which is the Z (forward-back) axis of the particle. This is used to properly orient the particle. The Orient Block in the update context is usually used to set up this attribute.", VFXVariadic.False, SpaceableType.Vector) { category = kRenderingCategory };
        public static readonly VFXAttribute Alive = new VFXAttribute("alive", VFXValue.Constant(true), "Indicates whether a particle is alive or should be destroyed. Can also be used within an output to toggle the rendering of that particle, without destroying it.") { category = kBasicSimulationCategory };
        public static readonly VFXAttribute Mass = new VFXAttribute("mass", VFXValue.Constant(1.0f), "The mass of the particle, which is used in many physics calculations. The value is expressed in Kg/dm^3 and the default mass is 1.0 (1\u00a0kg per liter of water).") { category = kAdvancedSimulationCategory };
        public static readonly VFXAttribute TargetPosition = new VFXAttribute("targetPosition", VFXValueType.Float3, "The attributes is usually used to store the position where the particle is aiming to go. Blocks like the ‘Position Sequential Shape’ can set it to be use in the output. But this attribute is also used by the line output and can be used as a storage helper.", VFXVariadic.False, SpaceableType.Position) { category = kAdvancedSimulationCategory };
        public static readonly VFXAttribute EventCount = new VFXAttribute("eventCount", VFXValueType.Uint32, string.Empty) { category = kSystemCategory };
        public static readonly VFXAttribute SpawnTime = new VFXAttribute("spawnTime", VFXValueType.Float, "Outputs the time since the Spawn context was triggered. To use, add a 'Set Spawn Time' block to the desired Spawn Context.") { category = kSystemCategory };
        public static readonly VFXAttribute ParticleIndexInStrip = new VFXAttribute("particleIndexInStrip", VFXValueType.Uint32, "Outputs the index of the particle within its particle strip. Each particle gets assigned an incremental index value for the strip within which it is created. This attribute is available in systems using the 'Particle Strip' data type.") { category = kStripCategory };
        public static readonly VFXAttribute SpawnIndex = new VFXAttribute("spawnIndex", VFXValueType.Uint32, "The index of the particle within all the particles spawned in the current frame.") { category = kSystemCategory };
        public static readonly VFXAttribute StripIndex = new VFXAttribute("stripIndex", VFXValueType.Uint32, "The index of the current strip. Each strip gets assigned an incremental value when it is created. This attribute is available in systems using the 'Particle Strip' data type.") { category = kStripCategory };
        public static readonly VFXAttribute ParticleCountInStrip = new VFXAttribute("particleCountInStrip", VFXValueType.Uint32, "Outputs the total particle count within the current strip. This attribute is available in systems using the 'Particle Strip' data type.") { category = kStripCategory };
        public static readonly VFXAttribute SpawnIndexInStrip = new VFXAttribute("spawnIndexInStrip", VFXValueType.Uint32, "The spawn index of the particle within its strip. Contrary to the ‘particleIndexInStrip’ attribute that is, unique for each particle, the spawnIndexInStrip value can be similar between particles in different strips.") { category = kStripCategory };
        public static readonly VFXAttribute SpawnCount = new VFXAttribute("spawnCount", VFXValue.Constant(1.0f), "The number of particles that have been spawned in this frame. It can be read in the Initialize Context as source or in the Spawn Context as Current.") { category = kSystemCategory };
        public static readonly VFXAttribute HasCollisionEvent = new VFXAttribute("hasCollisionEvent", VFXValue.Constant(false), "Outputs true at particle collision") {category = kCollisionCategory };
        public static readonly VFXAttribute CollisionEventNormal = new VFXAttribute("collisionEventNormal", VFXValue.Constant(Vector3.zero), "Outputs the collider normal at collision point at this frame. (0,0,0) if no collision", VFXVariadic.False, SpaceableType.Direction) {category = kCollisionCategory };
        public static readonly VFXAttribute CollisionEventPosition = new VFXAttribute("collisionEventPosition", VFXValue.Constant(Vector3.zero), "Outputs the collision point at this frame. (0,0,0) if no collision", VFXVariadic.False, SpaceableType.Position) {category = kCollisionCategory };
        public static readonly VFXAttribute CollisionEventCount = new VFXAttribute("collisionEventCount", VFXValueType.Uint32, "Outputs the number of total collisions detected by the particle since its birth") {category = kCollisionCategory };
        //public static readonly VFXAttribute ContinuousCollisionCount = new VFXAttribute("continuousCollisionCount", VFXValueType.Uint32, "Outputs the number of continuous collision detected (i.e detected at each frame) by the particle");
        public static readonly VFXAttribute OldVelocity = new VFXAttribute("oldVelocity", VFXValueType.Float3, "The velocity at the beginning of the context, before any force integration") {category = kAdvancedSimulationCategory };

        // Internal as we don't want it to appear in the graph
        internal static readonly VFXAttribute StripAlive = new VFXAttribute("stripAlive", VFXValue.Constant(true), string.Empty); // Internal attribute used to keep track of the state of the attached strip (TODO: Use a number to handle more tha 1 strip) { category = kStripCategory }
        internal static readonly VFXAttribute angle = new VFXAttribute("angle", VFXValueType.Float3, "The particle’s Euler rotation on each axis. Expressed as Angle in degree. For Camera-facing billboard particles, the Z axis is most likely the desired axis of rotation.", VFXVariadic.True) { category = kAdvancedSimulationCategory };
        internal static readonly VFXAttribute angularVelocity = new VFXAttribute("angularVelocity", VFXValueType.Float3, "The angular rotation of the particle, in degrees per second. By default, the Update Context is responsible to calculate the Rotation by integrating the angularVelocity each frame.", VFXVariadic.True) { category = kAdvancedSimulationCategory };
        internal static readonly VFXAttribute pivot = new VFXAttribute("pivot", VFXValueType.Float3, "The point around which the particle rotates, moves and is scaled. By default, this is the center of the particle. The value is computed in a one unit box size. By default, it is (0,0,0), the center of the box. You can change its value to adjust the center of the box. Every face is located at -0.5 or 0.5 in each axis.", VFXVariadic.True) { category = kRenderingCategory };
        internal static readonly VFXAttribute scale = new VFXAttribute("scale", VFXValue.Constant(new Vector3((float)VFXAttribute.ScaleX.value.GetContent(), (float)VFXAttribute.ScaleY.value.GetContent(), (float)VFXAttribute.ScaleZ.value.GetContent())), "The non-uniform scale of the particle that act as a multiplier to its size.", VFXVariadic.True) { category = kRenderingCategory };
    }

    class VFXAttributesManager : IVFXAttributesManager
    {
        private readonly Dictionary<string, VFXAttribute> m_CustomAttributes = new (StringComparer.OrdinalIgnoreCase);

        private static readonly List<VFXAttribute> s_BuiltInAttributes = new()
        {
            VFXAttribute.Seed,
            VFXAttribute.OldPosition,
            VFXAttribute.Position,
            VFXAttribute.Velocity,
            VFXAttribute.Direction,
            VFXAttribute.Color,
            VFXAttribute.Alpha,
            VFXAttribute.Size,
            VFXAttribute.ScaleX,
            VFXAttribute.ScaleY,
            VFXAttribute.ScaleZ,
            VFXAttribute.Lifetime,
            VFXAttribute.Age,
            VFXAttribute.AngleX,
            VFXAttribute.AngleY,
            VFXAttribute.AngleZ,
            VFXAttribute.AngularVelocityX,
            VFXAttribute.AngularVelocityY,
            VFXAttribute.AngularVelocityZ,
            VFXAttribute.TexIndex,
            VFXAttribute.MeshIndex,
            VFXAttribute.PivotX,
            VFXAttribute.PivotY,
            VFXAttribute.PivotZ,
            VFXAttribute.ParticleId,
            VFXAttribute.AxisX,
            VFXAttribute.AxisY,
            VFXAttribute.AxisZ,
            VFXAttribute.Alive,
            VFXAttribute.Mass,
            VFXAttribute.TargetPosition,
            VFXAttribute.EventCount,
            VFXAttribute.SpawnTime,
            VFXAttribute.ParticleIndexInStrip,
            VFXAttribute.SpawnIndex,
            VFXAttribute.StripIndex,
            VFXAttribute.ParticleCountInStrip,
            VFXAttribute.SpawnIndexInStrip,
            VFXAttribute.SpawnCount,
			VFXAttribute.HasCollisionEvent,
			VFXAttribute.CollisionEventNormal,
			VFXAttribute.CollisionEventPosition,
            VFXAttribute.CollisionEventCount,
            //VFXAttribute.ContinuousCollisionCount,
            VFXAttribute.OldVelocity,
        };

        private static readonly List<VFXAttribute> s_ReadOnlyAttributes = new ()
        {
            VFXAttribute.Seed,
            VFXAttribute.ParticleId,
            VFXAttribute.ParticleIndexInStrip,
            VFXAttribute.SpawnTime,
            VFXAttribute.SpawnIndex,
            VFXAttribute.SpawnCount,
            VFXAttribute.StripIndex,
            VFXAttribute.ParticleCountInStrip,
            VFXAttribute.SpawnIndexInStrip,
            VFXAttribute.HasCollisionEvent,
            VFXAttribute.CollisionEventNormal,
            VFXAttribute.CollisionEventPosition,
            VFXAttribute.CollisionEventCount,
            //VFXAttribute.ContinuousCollisionCount,
            VFXAttribute.OldVelocity
        };

        private static readonly List<VFXAttribute> s_WriteOnlyAttributes = new () { VFXAttribute.EventCount };
        private static readonly List<VFXAttribute> s_LocalOnlyAttributes = new () { VFXAttribute.EventCount, VFXAttribute.ParticleIndexInStrip, VFXAttribute.StripIndex, VFXAttribute.ParticleCountInStrip, VFXAttribute.HasCollisionEvent, VFXAttribute.OldVelocity };
        private static readonly List<VFXAttribute> s_AffectingAABBAttributes = new () { VFXAttribute.Position, VFXAttribute.PivotX, VFXAttribute.PivotY, VFXAttribute.PivotZ, VFXAttribute.Size, VFXAttribute.ScaleX, VFXAttribute.ScaleY, VFXAttribute.ScaleZ, VFXAttribute.AxisX, VFXAttribute.AxisY, VFXAttribute.AxisZ, VFXAttribute.AngleX, VFXAttribute.AngleY, VFXAttribute.AngleZ, };
        private static readonly List<VFXAttribute> s_VariadicComponentsAttributes = new() { VFXAttribute.AngleX, VFXAttribute.AngleY, VFXAttribute.AngleZ, VFXAttribute.AngularVelocityX, VFXAttribute.AngularVelocityY, VFXAttribute.AngularVelocityZ, VFXAttribute.PivotX, VFXAttribute.PivotY, VFXAttribute.PivotZ, VFXAttribute.ScaleX, VFXAttribute.ScaleY, VFXAttribute.ScaleZ };
        private static readonly List<VFXAttribute> s_VariadicAttribute = new () { VFXAttribute.angle, VFXAttribute.angularVelocity, VFXAttribute.pivot, VFXAttribute.scale };

        public static VFXAttribute[] AffectingAABBAttributes => s_AffectingAABBAttributes.ToArray();

        private static readonly Dictionary<string, VFXAttribute> s_BuiltinAttributeNameMap;

        static VFXAttributesManager()
        {
            s_BuiltinAttributeNameMap = new Dictionary<string, VFXAttribute>(StringComparer.OrdinalIgnoreCase);
            foreach (var attr in s_BuiltInAttributes)
            {
                s_BuiltinAttributeNameMap.Add(attr.name, attr);
            }

            foreach (var attr in s_VariadicAttribute)
            {
                s_BuiltinAttributeNameMap.Add(attr.name, attr);
            }
        }

        /* To be removed when the VFXLibrary will not be static anymore */
        public static VFXAttribute FindBuiltInOnly(string name)
        {
            if (string.IsNullOrEmpty(name))
                return default;
            s_BuiltinAttributeNameMap.TryGetValue(name, out var attribute);
            return attribute;
        }

        public static bool ExistsBuiltInOnly(string name)
        {
            return !string.IsNullOrEmpty(name) && s_BuiltinAttributeNameMap.ContainsKey(name);
        }

        public static IEnumerable<VFXAttribute> GetBuiltInAttributesOrCombination(bool includeVariadic, bool includeVariadicComponents, bool includeReadOnly, bool includeWriteOnly)
        {
            if (includeVariadic && includeVariadicComponents && includeReadOnly && includeWriteOnly)
            {
                foreach (var attribute in s_BuiltInAttributes)
                    yield return attribute;
                foreach (var attribute in s_VariadicAttribute)
                    yield return attribute;
                yield break;
            }
            foreach (var attribute in s_BuiltInAttributes)
            {
                if (!includeVariadicComponents && s_VariadicComponentsAttributes.Contains(attribute))
                    continue;
                if (!includeReadOnly && s_ReadOnlyAttributes.Contains(attribute))
                    continue;
                if (!includeWriteOnly && s_WriteOnlyAttributes.Contains(attribute))
                    continue;

                yield return attribute;
            }

            if (includeVariadic)
            {
                foreach (var attribute in s_VariadicAttribute)
                {
                    yield return attribute;
                }
            }
        }

        public static IEnumerable<VFXAttribute> GetBuiltInAttributesAndCombination(bool includeVariadic, bool includeVariadicComponents, bool includeReadOnly, bool includeWriteOnly)
        {
            foreach (var attribute in s_BuiltInAttributes)
            {
                var isVariadicComponent = s_VariadicComponentsAttributes.Contains(attribute);
                if (includeVariadicComponents && !isVariadicComponent || !includeVariadicComponents && isVariadicComponent)
                    continue;

                var isReadOnly = s_ReadOnlyAttributes.Contains(attribute);
                if (includeReadOnly && !isReadOnly || !includeReadOnly && isReadOnly)
                    continue;

                var isWriteOnly = s_WriteOnlyAttributes.Contains(attribute);
                if (includeWriteOnly && !isWriteOnly || !includeWriteOnly && isWriteOnly)
                    continue;

                yield return attribute;
            }

            if (includeVariadic)
            {
                foreach (var attribute in s_VariadicAttribute)
                {
                    yield return attribute;
                }
            }
        }

        public static IEnumerable<string> GetBuiltInNamesOrCombination(bool includeVariadic, bool includeVariadicComponents, bool includeReadOnly, bool includeWriteOnly)
        {
            foreach (var attribute in GetBuiltInAttributesOrCombination(includeVariadic, includeVariadicComponents, includeReadOnly, includeWriteOnly))
            {
                yield return attribute.name;
            }
        }

        public static IEnumerable<string> GetBuiltInNamesAndCombination(bool includeVariadic, bool includeVariadicComponents, bool includeReadOnly, bool includeWriteOnly)
        {
            foreach (var attribute in GetBuiltInAttributesAndCombination(includeVariadic, includeVariadicComponents, includeReadOnly, includeWriteOnly))
            {
                yield return attribute.name;
            }
        }

        /****************************************************************/

        public static IEnumerable<VFXAttribute> LocalOnlyAttributes => s_LocalOnlyAttributes;

        public IEnumerable<VFXAttribute> GetAllAttributesOrCombination(bool includeVariadic, bool includeVariadicComponents, bool includeReadOnly, bool includeWriteOnly)
        {
            foreach (var attribute in GetBuiltInAttributesOrCombination(includeVariadic, includeVariadicComponents, includeReadOnly, includeWriteOnly))
            {
                yield return attribute;
            }

            foreach (var attribute in m_CustomAttributes.Values)
            {
                yield return attribute;
            }
        }

        public IEnumerable<VFXAttribute> GetAllAttributesAndCombination(bool includeVariadic, bool includeVariadicComponents, bool includeReadOnly, bool includeWriteOnly)
        {
            foreach (var attribute in GetBuiltInAttributesAndCombination(includeVariadic, includeVariadicComponents, includeReadOnly, includeWriteOnly))
            {
                yield return attribute;
            }

            foreach (var attribute in m_CustomAttributes.Values)
            {
                yield return attribute;
            }
        }

        public IEnumerable<string> GetAllNamesOrCombination(bool includeVariadic, bool includeVariadicComponents, bool includeReadOnly, bool includeWriteOnly)
        {
            foreach (var attribute in GetAllAttributesOrCombination(includeVariadic, includeVariadicComponents, includeReadOnly, includeWriteOnly))
            {
                yield return attribute.name;
            }
        }

        public IEnumerable<string> GetAllNamesAndCombination(bool includeVariadic, bool includeVariadicComponents, bool includeReadOnly, bool includeWriteOnly)
        {
            foreach (var attribute in GetAllAttributesAndCombination(includeVariadic, includeVariadicComponents, includeReadOnly, includeWriteOnly))
            {
                yield return attribute.name;
            }
        }

        public IEnumerable<string> GetCustomAttributeNames()
        {
            foreach (var attribute in m_CustomAttributes.Keys)
            {
                yield return attribute;
            }
        }

        public IEnumerable<string> GetBuiltInAndVariadicNames()
        {
            foreach (var attribute in s_BuiltInAttributes)
            {
                yield return attribute.name;
            }

            foreach (var attribute in s_VariadicAttribute)
            {
                yield return attribute.name;
            }
        }

        public IEnumerable<VFXAttribute> GetCustomAttributes()
        {
            foreach (var attribute in m_CustomAttributes.Values)
            {
                yield return attribute;
            }
        }

        public bool TryFind(string name, out VFXAttribute attribute)
        {
            if (s_BuiltinAttributeNameMap.TryGetValue(name, out attribute))
                return true;

            if (m_CustomAttributes.TryGetValue(name, out attribute))
                return true;

            attribute = default;
            return false;
        }

        public bool TryFindWithMode(string name, VFXAttributeMode mode, out VFXAttribute attribute)
        {
            if (TryFind(name, out attribute))
            {
                if (IsCustom(name))
                {
                    return true;
                }

                switch (mode)
                {
                    case VFXAttributeMode.Read:
                        return !s_WriteOnlyAttributes.Contains(attribute);
                    case VFXAttributeMode.Write:
                        return !s_ReadOnlyAttributes.Contains(attribute);
                    case VFXAttributeMode.ReadWrite:
                        return !s_WriteOnlyAttributes.Contains(attribute) && !s_ReadOnlyAttributes.Contains(attribute);
                    case VFXAttributeMode.ReadSource:
                        break;
                }
            }

            return false;
        }

        public bool Exist(string name)
        {
            return ExistsBuiltInOnly(name) || m_CustomAttributes.ContainsKey(name);
        }

        public bool TryUpdate(string name, CustomAttributeUtility.Signature type, string description)
        {
            bool found = m_CustomAttributes.TryGetValue(name, out var customAttribute);
            if (!found)
                return false;
            var valueType = CustomAttributeUtility.GetValueType(type);
            if (!string.IsNullOrEmpty(customAttribute.name) && (valueType != customAttribute.type || description != customAttribute.description))
            {
                m_CustomAttributes.Remove(customAttribute.name);
                m_CustomAttributes.Add(name, new VFXAttribute(name, valueType, description));

                return true;
            }

            return false;
        }

        public bool IsCustom(string name)
        {
            return !string.IsNullOrEmpty(name) && m_CustomAttributes.ContainsKey(name);
        }

        public void ClearCustomAttributes()
        {
            m_CustomAttributes.Clear();
        }

        public bool TryRegisterCustomAttribute(string name, CustomAttributeUtility.Signature type, string description, out VFXAttribute newAttribute)
        {
            name = MakeValidName(name);

            if (TryFindExistingAttributeOrCreate(name, CustomAttributeUtility.GetValueType(type), out newAttribute))
            {
                return false;
            }

            newAttribute.description = description;
            m_CustomAttributes.Add(newAttribute.name, newAttribute);

            return true;
        }

        public void UnregisterCustomAttribute(string name)
        {
            m_CustomAttributes.Remove(name);
        }

        public RenameStatus TryRename(string oldName, string newName)
        {
            bool oldFound = m_CustomAttributes.TryGetValue(oldName, out VFXAttribute existingCustomAttribute);
            if (!oldFound)
            {
                return RenameStatus.NotFound;
            }

            if(ExistsBuiltInOnly(newName))
            {
                return RenameStatus.NameUsed;
            }

            bool newFound = m_CustomAttributes.TryGetValue(newName, out VFXAttribute existingCustomAttributeNewName);
            if (newFound && !existingCustomAttributeNewName.Equals(existingCustomAttribute))
            {
                return RenameStatus.NameUsed;
            }

            if (!CustomAttributeUtility.IsShaderCompilableName(newName))
            {
                Debug.LogError("Custom attribute could not be renamed, it does not start with a letter or underscore and/or contains non-alphanumeric characters. Previous name has been kept.");
                return RenameStatus.InvalidName;
            }

            m_CustomAttributes.Remove(oldName);
            existingCustomAttribute.Rename(newName);
            m_CustomAttributes.Add(newName, existingCustomAttribute);
            return RenameStatus.Success;
        }

        public VFXAttribute Duplicate(string name)
        {
            if (TryFind(name, out var newAttribute))
            {
                // Do not register, let the graph do it
                newAttribute.name = FindUniqueName(newAttribute.name);
                return newAttribute;
            }

            throw new InvalidOperationException($"Trying to duplicate a custom attribute that is not found {name}");
        }

        public string FindUniqueName(string name)
        {
            var existingNames = new HashSet<string>(GetAllNamesOrCombination(true, true, true, true));
            return VFXParameterController.MakeNameUnique(name, existingNames, false);
        }


        private bool TryFindExistingAttributeOrCreate(string name, VFXValueType type, out VFXAttribute attribute)
        {
            if (TryFind(name, out attribute))
            {
                if (attribute.type == type)
                {
                    return true;
                }

                var existingNames = new HashSet<string>(GetAllNamesOrCombination(true, true, true, true));
                var rejectedCandidateNames = new List<string>();
                name = VFXParameterController.MakeNameUnique(name, existingNames, false, rejectedCandidateNames);
                foreach (var candidateName in rejectedCandidateNames)
                {
                    if (TryFind(candidateName, out attribute) && attribute.type == type)
                    {
                        return true;
                    }
                }
            }

            attribute = new VFXAttribute(name, type, null);
            return false;
        }

        private string MakeValidName(string name)
        {
            return CustomAttributeUtility.IsShaderCompilableName(name)
                ? name
                : CustomAttributeUtility.MakeShaderCompatibleName(name);
        }
    }
}
