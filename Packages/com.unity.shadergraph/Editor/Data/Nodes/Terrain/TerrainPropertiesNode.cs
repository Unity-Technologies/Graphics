using System.Reflection;
using UnityEditor.Graphing;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Title("Terrain", "Terrain Properties")]
    [SubTargetFilterAttribute(new[] { typeof(ITerrainSubTarget)})]
    class TerrainPropertiesNode : AbstractMaterialNode, IGeneratesFunction
    {
        enum SlotIDs
        {
            MaxLocalHeight,
            BasemapDistance,
            LayersCount,
        }

        public TerrainPropertiesNode()
        {
            name = "Terrain Properties";
            UpdateNodeAfterDeserialization();
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new Vector1MaterialSlot((int)SlotIDs.MaxLocalHeight, "Max Local Height", "Max Local Height", SlotType.Output, 600));
            AddSlot(new Vector1MaterialSlot((int)SlotIDs.BasemapDistance, "Basemap Distance", "Terrain Basemap Distance", SlotType.Output, 1));
            AddSlot(new Vector1MaterialSlot((int)SlotIDs.LayersCount, "Layers Count", "Terrain Layers Count", SlotType.Output, 4));
        }

        // maybe convert to function instead of static variables in case that data does not readily exist in uniform buffers
        public override string GetVariableNameForSlot(int slotId)
        {
            var id = (SlotIDs)slotId;
            return id switch
            {
                SlotIDs.MaxLocalHeight => "(_TerrainHeightmapScale.y * .5f)", // scale by .5f for packed signed heightmap range to convert to unsigned. only using half of available normalized range
                SlotIDs.BasemapDistance => "_TerrainBasemapDistance",
                SlotIDs.LayersCount => "_NumLayersCount",
                _ => base.GetVariableNameForSlot(slotId)
            };
        }

        public void GenerateNodeFunction(FunctionRegistry registry, GenerationMode generationMode)
        {
            registry.RequiresIncludePath("Packages/com.unity.shadergraph/Editor/Generation/Targets/Terrain/Includes/TerrainPropertiesVariables.hlsl");
        }
    }
}
