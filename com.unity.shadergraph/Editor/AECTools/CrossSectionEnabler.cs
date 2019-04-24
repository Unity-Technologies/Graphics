using System;
using System.Reflection;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using Debug = UnityEngine.Debug;
using UnityEditor.Graphing;
using UnityEditor.Graphing.Util;
using UnityEditor.ShaderGraph.Drawing.Controls;

namespace UnityEditor.ShaderGraph
{
    // General names (semantic) for the different master node inputs we will override by splicing our
    // cross section subgraph node (up to enum = MasterNodesGenericInputNamesMaxIdx).
    // Also contains the names of some other slot "meanings" (semantic) that can be found on that cross
    // section node.
    // (SlotId field can be named differently depending on type of MasterNode, so we use this more
    // abstract representation of "what data is carried" in those slots.)
    // The cross section subgraph node responsible for modifying the behavior of the original ShaderGraph
    // by splicing in front of the master node has some of the semantics on both input and output slots.
    //
    // The mapping of name definitions to semantics is accessible through ICrossSectionEnablerConfig,
    // see the default declaration in CrossSectionEnablerConfig.
    public enum GenericSlotNames
    {
        BaseColor = 1,
        Normal,
        CoatNormal,
        SmoothnessA,
        SmoothnessB,
        Metallic,
        SpecularColor,
        CoatMask,
        Emission,
        AlphaThreshold,
        AlphaThresholdShadow,
        AlphaThresholdDepthPrepass,

        AlphaThresholdDepthPostpass,
        MasterNodesGenericInputNamesMaxIdx = AlphaThresholdDepthPostpass,

        // Past this point, these inputs are used only for the cross section subgraph and dont match any
        // master node slots

        AlphaTestSavedState,       // bookkeeping original toggle state to be able to unsplice and remove ourselves from the ShaderGraph
        DoubleSidedSavedState,

        // TODO: other input props to special case the cross section per shader graph:

        // The following are designed to be able to customize the behavior of the cross section
        // per material without having to change the cross section subgraph itself since it is handy
        // to have all materials share the same cross section subgraph.
        // Use the "CrossSectionPerMaterialInputConnector" subgraph node as a way to let the splicing
        // tool know where to get these local values in each local shader graph material.
        // The adapter itself should never be edited, it is just used as a name matcher.
        CrossSectionEnable,
        MeshIsNotClosedHint,
        CrossSectionPerMaterialA,
        CrossSectionPerMaterialB,
        CrossSectionPerMaterialC,
        CrossSectionPerMaterialD,
    }

    public interface ICrossSectionEnablerConfig
    {
        string crossSectionSubGraphGuid { get; }
        string crossSectionPerMaterialInputConnectorGuid { get;  }
        Dictionary<string, GenericSlotNames> crossSectionNodeSlotdNamesToGenericName { get; }
        Dictionary<string, GenericSlotNames> slotIdFieldNamesToGenericInput { get; }
        List<GenericSlotNames> alphaThresholdSlotsDependingOnAlphaClipEnable { get; }
        List<string> alphaTestTogglePropertyNames { get; }
        List<string> doubleSidedPropertyNames { get; }
        Dictionary<Type, int> doubleSidedEnabledAsIntValueByType { get; }
    }

    // This class informs on our default cross section subgraph:
    public class CrossSectionEnablerConfig : ICrossSectionEnablerConfig
    {
        public const string k_CrossSectionPerMaterialInputConnectorGuid = "16ca7af74b803d64daf888698806b7e8";
        public virtual string crossSectionPerMaterialInputConnectorGuid { get => k_CrossSectionPerMaterialInputConnectorGuid; }

        public const string k_CrossSectionSubGraphGuidDefault = "0c989fa2ca74d4145aa12a4eccc46bfc";
        public virtual string crossSectionSubGraphGuid { get => k_CrossSectionSubGraphGuidDefault; }

        // Our CrossSection SubGraphNode input and output slot names with corresponding generic name
        // (remember the generic slot names are like more abstract semantics and can be either
        // an input slot or an output slot)
        // TODOTODO: add the other input-only slots
        private static readonly Dictionary<string, GenericSlotNames> k_CrossSectionNodeSlotdNamesToGenericName =
            new Dictionary<string, GenericSlotNames>()
        {
            { "AlphaThreshold",              GenericSlotNames.AlphaThreshold },
            { "AlphaClipThreshold",          GenericSlotNames.AlphaThreshold },
            { "AlphaClipThresholdOverride",  GenericSlotNames.AlphaThreshold },
            { "AlphaThresholdShadow",        GenericSlotNames.AlphaThresholdShadow },
            { "AlphaThresholdDepthPrepass",  GenericSlotNames.AlphaThresholdDepthPrepass },
            { "AlphaThresholdDepthPostpass", GenericSlotNames.AlphaThresholdDepthPostpass },

            { "BaseColor",                   GenericSlotNames.BaseColor },
            { "SpecularColor",               GenericSlotNames.SpecularColor },
            { "Emission",                    GenericSlotNames.Emission },
            { "Metallic",                    GenericSlotNames.Metallic },
            { "Normal",                      GenericSlotNames.Normal },
            { "CoatNormal",                  GenericSlotNames.CoatNormal },
            { "CoatMask",                    GenericSlotNames.CoatMask },
            { "Smoothness",                  GenericSlotNames.SmoothnessA },
            { "SmoothnessB",                 GenericSlotNames.SmoothnessB },

            { "AlphaTestSavedState",         GenericSlotNames.AlphaTestSavedState },
            { "DoubleSidedSavedState",       GenericSlotNames.DoubleSidedSavedState },

            //
            { "CrossSectionEnable",          GenericSlotNames.CrossSectionEnable },
            { "MeshIsNotClosed",             GenericSlotNames.MeshIsNotClosedHint },

        };
        public Dictionary<string, GenericSlotNames> crossSectionNodeSlotdNamesToGenericName { get => k_CrossSectionNodeSlotdNamesToGenericName; }

        // All possible master node slotid field names we're interested in
        // (each is a possible name for one of the generic inputs we manipulate)
        private static readonly Dictionary<string, GenericSlotNames> k_SlotIdFieldNamesToGenericInput =
            new Dictionary<string, GenericSlotNames>()
        {
            { "AlbedoSlotId",                          GenericSlotNames.BaseColor },
            { "BaseColorSlotId",                       GenericSlotNames.BaseColor },
            { "ColorSlotId",                           GenericSlotNames.BaseColor },
            { "EmissionSlotId",                        GenericSlotNames.Emission },
            { "MetallicSlotId",                        GenericSlotNames.Metallic },
            { "SpecularColorSlotId",                   GenericSlotNames.SpecularColor },
            { "SpecularSlotId",                        GenericSlotNames.SpecularColor },
            { "NormalSlotId",                          GenericSlotNames.Normal },
            { "CoatNormalSlotId",                      GenericSlotNames.CoatNormal },
            { "CoatMaskSlotId",                        GenericSlotNames.CoatMask },
            { "SmoothnessSlotId",                      GenericSlotNames.SmoothnessA },
            { "SmoothnessASlotId",                     GenericSlotNames.SmoothnessA },
            { "SmoothnessBSlotId",                     GenericSlotNames.SmoothnessB },
            { "AlphaThresholdSlotId",                  GenericSlotNames.AlphaThreshold },
            { "AlphaClipThresholdSlotId",              GenericSlotNames.AlphaThreshold },
            { "AlphaThresholdShadowSlotId",            GenericSlotNames.AlphaThresholdShadow },
            { "AlphaClipThresholdShadowSlotId",        GenericSlotNames.AlphaThresholdShadow },
            { "AlphaThresholdDepthPrepassSlotId",      GenericSlotNames.AlphaThresholdDepthPrepass },
            { "AlphaClipThresholdDepthPrepassSlotId",  GenericSlotNames.AlphaThresholdDepthPrepass },
            { "AlphaThresholdDepthPostpassSlotId",     GenericSlotNames.AlphaThresholdDepthPostpass },
            { "AlphaClipThresholdDepthPostpassSlotId", GenericSlotNames.AlphaThresholdDepthPostpass },
        };
        public Dictionary<string, GenericSlotNames> slotIdFieldNamesToGenericInput { get => k_SlotIdFieldNamesToGenericInput; }

        // List of inputs that could be newly added to a master node by enabling its alpha clip property:
        // AlphaThresholdSlots normally are opened based on if AlphaTest is enabled for the whole node.
        // We need this to make sure the bypass values (when we don't detect the fragment being clipped)
        // (those present on the subgraph inputs) are set to k_AlphaThresholdDisabledEquivalent 
        private static readonly List<GenericSlotNames> k_AlphaThresholdSlotsDependingOnAlphaClipEnable = new List<GenericSlotNames>()
        {
            GenericSlotNames.AlphaThreshold,
            GenericSlotNames.AlphaThresholdShadow,
            GenericSlotNames.AlphaThresholdDepthPrepass,
            GenericSlotNames.AlphaThresholdDepthPostpass,
        };
        public List<GenericSlotNames> alphaThresholdSlotsDependingOnAlphaClipEnable { get => k_AlphaThresholdSlotsDependingOnAlphaClipEnable; }

        // List of possible property names for the ToggleData property to enable alpha clipping:
        private static readonly List<string> k_AlphaTestTogglePropertyNames = new List<string>()
        {
            "alphaTest",
        };
        public List<string> alphaTestTogglePropertyNames { get => k_AlphaTestTogglePropertyNames; }

        // List of possible property names for the ToggleData property to enable double sided mode:
        // (unfortunately, we can't inject an extra pass and a stencil bit usage for the lifetime of
        // our passes for now in HDRP)
        private static readonly List<string> k_DoubleSidedTogglePropertyNames = new List<string>()
        {
            "doubleSidedMode",
            "twoSided",
        };
        public List<string> doubleSidedPropertyNames { get => k_DoubleSidedTogglePropertyNames; }

        // Ugly way to refer, for multiple property types, namely ToggleData and enum for now,
        // what is the value corresponding to "enabled".
        private static readonly Dictionary<Type, int> k_DoubleSidedEnabledAsIntValueByType =
            new Dictionary<Type, int>()
        {
            { typeof(ToggleData), Convert.ToInt32(true)},
            { typeof(DoubleSidedMode), (int)DoubleSidedMode.Enabled},
        };
        public Dictionary<Type, int> doubleSidedEnabledAsIntValueByType { get => k_DoubleSidedEnabledAsIntValueByType; }
    }//CrossSectionEnablerConfig

    // This class discovers and configures some auxilliary data structures required to splice in a cross section subgraph
    // in a generic ShaderGraph.
    // It requires ICrossSectionEnablerConfig to give the subgraphGuid of the actual cross section subgraph and also describes
    // how it implements certain required ports. CrossSectionEnablerConfig above is the built-in class describing our built-in
    // cross section subgraph asset.
    // This class also contains the actual per ShaderGraph splicing code.
    class CrossSectionEnabler
    {
        // Note the config object contains the cross section subgraph's to use via its subgraphGuid
        public CrossSectionEnabler(ICrossSectionEnablerConfig config = null)
        {
            ResetConfig(config);
        }

        private MessageManager m_MessageManager;
        private MessageManager messageManager
        {
            get { return m_MessageManager ?? (m_MessageManager = new MessageManager()); }
        }

        private ICrossSectionEnablerConfig m_MainConfig;
        private ICrossSectionEnablerConfig mainConfig
        {
            get { return m_MainConfig ?? (m_MainConfig = new CrossSectionEnablerConfig()); }
            set
            {
                m_MainConfig = value;
                Reset();
            }
        }
        public void ResetConfig(ICrossSectionEnablerConfig config)
        {
            mainConfig = config;
        }

        public const float k_AlphaThresholdDisabledEquivalent = -1f;
        public const float k_SimplePropertyStateInvalidValue = -1f; // symbol for invalid / not found / no change for doublesided properties / toggle 

        // List of possible slotid field names for the alpha threshold input:
        // Will be populated automatically on init from the mapping info we've set in k_SlotIdFieldNamesToGenericInput
        private List<string> alphaThresholdSlotIdFieldNames = new List<string>();

        private void InitAlphaThresholdSlotIdFieldNames()
        {
            alphaThresholdSlotIdFieldNames =
                mainConfig.slotIdFieldNamesToGenericInput
                .Where(pair => pair.Value == GenericSlotNames.AlphaThreshold)
                .Select(pair => pair.Key).ToList();
        }

        //TODO removeme not really needed, could use the other dictionaries eg s_MasterNodeAlphaToggleAndSlotIds
        private HashSet<Type> compatibleMasterNodesSet = new HashSet<Type>();

        private class MasterNodeInfo
        {
            public PropertyInfo AlphaTestToggle;
            public PropertyInfo DoubleSidedProperty;
            public int DoubleSidedEnabledValue;
            public Dictionary<GenericSlotNames, int> GenericSlotNamesToSlotId;
        }
        // Mapping giving per master node type, all basic info to manipulate and connect to it (value is a 2-tuple):
        // -a property to toggle alpha clipping (alphatest)
        // -a mapping from generic input names to the SlotId int used by that specific master node for that input
        //private static Dictionary<Type, Tuple<PropertyInfo, Dictionary<GenericSlotNames, int>>> s_MasterNodeAlphaToggleAndSlotIds = new Dictionary<Type, Tuple<PropertyInfo, Dictionary<GenericSlotNames, int>>>();
        private Dictionary<Type, MasterNodeInfo> masterNodeTypeToNodeInfo = new Dictionary<Type, MasterNodeInfo>();

        // We will create a SubGraphNode of our CrossSectionSupport.shadersubgraph asset as that will load
        // its SubGraphData from the SubGraphDatabase and allow us to find the slotids that ShaderGraph generated
        // for the various input/output and properties we have created and need to acces programmatically here.
        // This allow us to discover everything by naming convention from the single GUID in the .meta of the subgraph.
        private SubGraphNode CreateCrossSectionSubGraphNode()
        {
            SubGraphNode crossSectionSubGraphNode = null;
            string path = AssetDatabase.GUIDToAssetPath(mainConfig.crossSectionSubGraphGuid);
            if (!string.IsNullOrEmpty(path))
            {
                // The imported shader subgraph "overall asset" contains an asset we create an import called SubGraphAsset,
                // a scriptable object descendent which is almost empty, just containing importedAt but the .shadersubgraph
                // importer relies on the SubGraphDatabaseImporter to properly store the SubGraphData constructed from the
                // .shadersubgraph text. The SubGraphAsset is the only actual asset that Unity sees as output from importing
                // the .shadersubgraph and its guid is also used to identify the subgraph in the SubGraphDatabase to get its
                // SubGraphData. 
                var asset = AssetDatabase.LoadAssetAtPath<SubGraphAsset>(path);
                crossSectionSubGraphNode = new SubGraphNode { subGraphAsset = asset };
            }
            return crossSectionSubGraphNode;
        }

        private SubGraphNode CreateCrossSectionInputConnectorSubGraphNode()
        {
            SubGraphNode node = null;
            string path = AssetDatabase.GUIDToAssetPath(mainConfig.crossSectionPerMaterialInputConnectorGuid);
            if (!string.IsNullOrEmpty(path))
            {
                var asset = AssetDatabase.LoadAssetAtPath<SubGraphAsset>(path);
                node = new SubGraphNode { subGraphAsset = asset };
            }
            return node;
        }

        // Mapping for the cross section subgraph node:
        // Generic input (and output for the subgraph since it splices edges and inserts itself between potentially other nodes
        // and the master nodes) to the input (left side) and output (right side) slotId number on the cross section subgraph node.
        // For certain generic names, the output might not exists, we use our constant here:
        private const int k_SlotIdNone = int.MinValue;
        private Dictionary<GenericSlotNames, Tuple<int, int>> genericSlotNamesToCrossSectionIOSlotIds = new Dictionary<GenericSlotNames, Tuple<int, int>>();

        // See above about the connector: this is done purely by name: the connector is opened, outputs are enumerated,
        // matching names searched in the cross section subgraph node inputs, and the mapping registered here.
        private Dictionary<int, int> crossSectionPerMaterialInputConnectorSlotMapping = new Dictionary<int, int>();

        public void Reset()
        {
            compatibleMasterNodesSet.Clear();
            masterNodeTypeToNodeInfo.Clear();
            genericSlotNamesToCrossSectionIOSlotIds.Clear();

            InitAlphaThresholdSlotIdFieldNames();
            FillCompatibleMasterNodesInfo();
            FillCrossSectionSubGraphInfo();
        }

        // todotodo totest
        private PropertyInfo FindPropertyInfoMatch(Type masterNodeType, List<string> potentialNames,
                                                   List<Type> potentialTypes = null)
        {
            potentialTypes = potentialTypes ?? new List<Type> { typeof(ToggleData), typeof(Enum) };

            var prop = masterNodeType.GetProperties()
                                     .Where(pi => potentialTypes.Contains(pi.PropertyType)
                                                  && potentialNames.Contains(pi.Name))
                                     .ToList();
            // There should really be only one matching the list of potential names (no ambiguity)
            return (prop.Count == 1) ? prop[0] : null;
        }

        private void FillCrossSectionSubGraphInfo()
        {
            SubGraphNode crossSectionSubGraphNode = CreateCrossSectionSubGraphNode();
            if (crossSectionSubGraphNode != null)
            {
                // crossSectionSubGraphNode.subGraphData.outputs : these are already MaterialSlot
                // crossSectionSubGraphNode.subGraphData.inputs : these are AbstractShaderProperty
                //
                // Since the SubGraphNode will create MaterialSlots from the SubGraphData inputs/outputs, we can query the
                // SubGraphNode slots from its inherited AbstractMaterialNode methods:
                Dictionary<GenericSlotNames, int> subGraphNodeInputs = new Dictionary<GenericSlotNames, int>();
                Dictionary<GenericSlotNames, int> subGraphNodeOutputs = new Dictionary<GenericSlotNames, int>();
                var crossSectionSubGraphInputs = crossSectionSubGraphNode.GetInputSlots<MaterialSlot>();
                var crossSectionSubGraphOutputs = crossSectionSubGraphNode.GetOutputSlots<MaterialSlot>();
                foreach (var slot in crossSectionSubGraphInputs)
                {
                    if (mainConfig.crossSectionNodeSlotdNamesToGenericName.TryGetValue(slot.RawDisplayName(), out var genericInputName))
                    {
                        Debug.Assert(slot.id != k_SlotIdNone, "A node with a slotId == k_SlotIdNone was found in a subgraph.");
                        subGraphNodeInputs.Add(genericInputName, slot.id);
                    }
                }
                foreach (var slot in crossSectionSubGraphOutputs)
                {
                    if (mainConfig.crossSectionNodeSlotdNamesToGenericName.TryGetValue(slot.RawDisplayName(), out var genericInputName))
                    {
                        Debug.Assert(slot.id != k_SlotIdNone, "A node with a slotId == k_SlotIdNone was found in a subgraph.");
                        subGraphNodeOutputs.Add(genericInputName, slot.id);
                    }
                }

                // Combine the results of subGraphNodeInputs and subGraphNodeOutputs into a single table, putting k_SlotIdNone
                // as a slotid when the property wanted (the "generic slot name") is only valid as an input:
                foreach (var genericSlotName in Enum.GetValues(typeof(GenericSlotNames)).Cast<GenericSlotNames>())
                {
                    int inSlotId;
                    int outSlotId;
                    bool inSlotHasThatGenericInput = subGraphNodeInputs.TryGetValue(genericSlotName, out inSlotId);
                    bool outSlotHasThatGenericOuput = subGraphNodeOutputs.TryGetValue(genericSlotName, out outSlotId);
                    inSlotId = inSlotHasThatGenericInput ? inSlotId : k_SlotIdNone;
                    outSlotId = outSlotHasThatGenericOuput ? outSlotId : k_SlotIdNone;

                    if (outSlotHasThatGenericOuput || inSlotHasThatGenericInput)
                    {
                        if (outSlotHasThatGenericOuput && !inSlotHasThatGenericInput)
                        {
                            Debug.LogWarning(string.Format("Warning: {0} type is found on the output of the cross section subgraph " +
                                                           "but not on the input (where's the override port to connect the original edge?)",
                                                           genericSlotName.ToString()));
                        }
                        genericSlotNamesToCrossSectionIOSlotIds.Add(genericSlotName, new Tuple<int, int>(inSlotId, outSlotId));
                    }
                }

                SubGraphNode inputConnectorSubGraphNode = CreateCrossSectionInputConnectorSubGraphNode();
                if (inputConnectorSubGraphNode != null)
                {
                    var connectorOutputs = inputConnectorSubGraphNode.GetOutputSlots<MaterialSlot>();
                    foreach(var outSlot in connectorOutputs)
                    {
                        var inSlotMatch = crossSectionSubGraphInputs.Where(matSlot => outSlot.RawDisplayName().Equals(matSlot.RawDisplayName())).FirstOrDefault();
                        if (inSlotMatch != null)
                        {
                            crossSectionPerMaterialInputConnectorSlotMapping.Add(outSlot.slotReference.slotId, inSlotMatch.slotReference.slotId);
                        }
                    }
                }
            }
        }

        private void FillCompatibleMasterNodesInfo()
        {
            IEnumerable<Type> masterNodesTypeList = AppDomain.CurrentDomain.GetAssemblies().SelectMany(a => a.GetTypesOrNothing())
                .Where(myType => myType.IsClass && !myType.IsAbstract && typeof(IMasterNode).IsAssignableFrom(myType));

            foreach (Type type in masterNodesTypeList)
            {
                Debug.Log(string.Format("{0} type name found.", type.Name));

                // We try to fill a Dictionary<input name, slot id int> right away, and at the end if we don't
                // have a mapping entry for GenericSlotNames.AlphaThreshold, it means the node is not compatible.
                Dictionary<GenericSlotNames, int> genericSlotNamesToSlotId = new Dictionary<GenericSlotNames, int>();
                foreach (FieldInfo fi in type.GetFields(BindingFlags.Public | BindingFlags.Static).Where(fi => fi.FieldType == typeof(int)))
                {
                    if (mainConfig.slotIdFieldNamesToGenericInput.TryGetValue(fi.Name, out var genericInputName))
                    {
                        int staticSlotId = (int)fi.GetValue(null);
                        Debug.Assert(staticSlotId != k_SlotIdNone, "A masternode with a slotId == k_SlotIdNone was found.");
                        genericSlotNamesToSlotId.Add(genericInputName, staticSlotId);
                    }
                }

                if (genericSlotNamesToSlotId.TryGetValue(GenericSlotNames.AlphaThreshold, out int slotId))
                {
                    Debug.Log(string.Format("{0} type compatible with Alpha Clipping.", type.Name));
                    compatibleMasterNodesSet.Add(type); // todo: removeme not really needed

                    PropertyInfo alphaTogglePropertyInfo =
                        FindPropertyInfoMatch(type, mainConfig.alphaTestTogglePropertyNames, new List<Type>{ typeof(ToggleData) });
                    PropertyInfo doubleSidedPropertyInfo =
                        FindPropertyInfoMatch(type, mainConfig.doubleSidedPropertyNames, new List<Type> { typeof(ToggleData), typeof(DoubleSidedMode) });

                    // Store this master node info: the props (if any)
                    // and the mapping of generic input names that we know of to the slot ids used by it
                    int doubleSidedEnabledValue = (int) k_SimplePropertyStateInvalidValue;
                    if (doubleSidedPropertyInfo != null)
                    {
                        if (false == mainConfig.doubleSidedEnabledAsIntValueByType.TryGetValue(doubleSidedPropertyInfo.PropertyType, out int doubleSidedEnabledIntValue))
                        {
                            Debug.LogWarning(string.Format("Found a matching DoubleSided property on master node but CrossSectionEnablerConfig has no enabled value" +
                                " listed for that type of property: {0} type. IE I can't understand that type, can't set it to enable.", doubleSidedPropertyInfo.PropertyType.Name));
                        }
                        else
                        {
                            doubleSidedEnabledValue = doubleSidedEnabledIntValue;
                        }
                    }

                    masterNodeTypeToNodeInfo.Add( type,
                        new MasterNodeInfo
                        {
                            AlphaTestToggle = alphaTogglePropertyInfo,
                            DoubleSidedProperty = doubleSidedPropertyInfo,
                            DoubleSidedEnabledValue = doubleSidedEnabledValue,
                            GenericSlotNamesToSlotId = genericSlotNamesToSlotId,
                        });
                }
            } // foreach master node type
        }

        //
        // Actual per ShaderGraph surgery section
        //

        private static bool GetMasterNodeAlphaTestToggle(MasterNodeInfo masterNodeInfo, IMasterNode masterNode)
        {
            // We will need to save the alphaTest enabled state for the master node, we will need it.
            // If the node doesn't have one but we've reached this point in the code (it is listed in the masterNodeAlphaToggleAndSlotIds
            // table, thus an AlphaTreshold slot was found to be declared on it), it means the master node is something like PBRMasterNode,
            // and we infer the alphaTest is always on.
            bool alphaTestWasEnabled;
            if (masterNodeInfo.AlphaTestToggle == null)
            {
                alphaTestWasEnabled = true;
            }
            else
            {
                ToggleData alphaTestToggle = (ToggleData)masterNodeInfo.AlphaTestToggle.GetValue(masterNode);
                alphaTestWasEnabled = alphaTestToggle.isOn;
            }
            return alphaTestWasEnabled;
        }

        private static void SetMasterNodeAlphaTestToggle(MasterNodeInfo masterNodeInfo, IMasterNode masterNode, bool toggleValue)
        {
            if (masterNodeInfo.AlphaTestToggle == null)
                return;
            ToggleData alphaTestToggleOn = new ToggleData(toggleValue);
            masterNodeInfo.AlphaTestToggle.SetValue(masterNode, alphaTestToggleOn);
        }

        //remove me
        private static float GetMasterNodeDoubleSidedStateAsFloat(MasterNodeInfo masterNodeInfo, IMasterNode masterNode)
        {
            float doubleSidedState = k_SimplePropertyStateInvalidValue;
            if (masterNodeInfo.DoubleSidedProperty != null)
            {
                var doubleSidedStateValueObject = masterNodeInfo.DoubleSidedProperty.GetValue(masterNode);
                if (doubleSidedStateValueObject is ToggleData toggle)
                {
                    doubleSidedState = Convert.ToSingle(toggle.isOn);
                }
                else if (doubleSidedStateValueObject is DoubleSidedMode doubleSidedEnum)
                {
                    doubleSidedState = (float)doubleSidedEnum;
                }
            }
            return doubleSidedState;
        }
        //remove me
        private static void SetMasterNodeDoubleSidedStateFromFloat(MasterNodeInfo masterNodeInfo, IMasterNode masterNode, float doubleSidedState)
        {
            if (doubleSidedState == k_SimplePropertyStateInvalidValue || masterNodeInfo.DoubleSidedProperty == null)
                return;

            if (masterNodeInfo.DoubleSidedProperty.PropertyType == typeof(ToggleData))
            {
                masterNodeInfo.DoubleSidedProperty.SetValue(masterNode, new ToggleData(Convert.ToBoolean(doubleSidedState)));
            }
            else if (masterNodeInfo.DoubleSidedProperty.PropertyType == typeof(DoubleSidedMode))
            {
                DoubleSidedMode doubleSidedSavedState = (DoubleSidedMode)doubleSidedState;
                masterNodeInfo.DoubleSidedProperty.SetValue(masterNode, doubleSidedSavedState);
            }
        }

        private static float GetSimplePropertyAsFloat(System.Object obj, PropertyInfo propInfo)
        {
            float value = k_SimplePropertyStateInvalidValue;
            if (propInfo != null)
            {
                var valueObject = propInfo.GetValue(obj);
                if (valueObject is ToggleData toggle)
                {
                    value = Convert.ToSingle(toggle.isOn);
                }
                else if (valueObject is Enum enumValue)
                {
                    value = (int)Convert.ChangeType( enumValue, Enum.GetUnderlyingType(enumValue.GetType()) );
                }
            }
            return value;
        }

        private static void SetSimplePropertyFromFloat(System.Object obj, PropertyInfo propInfo, float value)
        {
            if (value < 0 || propInfo == null)
                return;

            if (propInfo.PropertyType == typeof(ToggleData))
            {
                propInfo.SetValue(obj, new ToggleData(Convert.ToBoolean(value)));
            }
            else if (propInfo.PropertyType.IsEnum)
            {
                int enumValue = (int)value;
                propInfo.SetValue(obj, enumValue);
            }
        }

        private static bool GetSlotIdFromGenericName(GenericSlotNames slotName, Dictionary<GenericSlotNames, int> translator, out int slotId)
        {
            slotId = k_SlotIdNone;
            if (translator.TryGetValue(slotName, out int foundSlotId))
                slotId = foundSlotId;

            return slotId != k_SlotIdNone;
        }

        private static bool GetSlotIdFromGenericName(GenericSlotNames slotName, Dictionary<GenericSlotNames, Tuple<int,int>> translator, out int slotId, bool wantInputSlot = true)
        {
            slotId = k_SlotIdNone;
            if (translator.TryGetValue(slotName, out Tuple<int, int> ioSlotIds))
                slotId = wantInputSlot ? ioSlotIds.Item1 : ioSlotIds.Item2;

            return slotId != k_SlotIdNone;
        }

        // Slot value copy: slot ref (node, slotid) to slot ref (node, slotid)
        private static void CopySlotValue(AbstractMaterialNode dstNode, int dstSlotId, AbstractMaterialNode srcNode, int srcSlotId)
        {
            var dstMatSlot = dstNode.FindSlot<MaterialSlot>(dstSlotId);
            var srcMatSlot = srcNode.FindSlot<MaterialSlot>(srcSlotId);
            if (dstMatSlot != null)
                dstMatSlot.CopyValuesFrom(srcMatSlot); // handles null param by itself
        }

        // Slot value copy: MaterialSlot to slot ref (node, slotid)
        private static void CopySlotValue(AbstractMaterialNode dstNode, int dstSlotId, MaterialSlot srcMatSlot)
        {
            var dstMatSlot = dstNode.FindSlot<MaterialSlot>(dstSlotId);
            if (dstMatSlot != null)
                dstMatSlot.CopyValuesFrom(srcMatSlot); // handles null param by itself
        }

        // Slot value copy: slot ref (node, slotid) to MaterialSlot
        private static void CopySlotValue(MaterialSlot dstMatSlot, AbstractMaterialNode srcNode, int srcSlotId)
        {
            var srcMatSlot = srcNode.FindSlot<MaterialSlot>(srcSlotId);
            dstMatSlot.CopyValuesFrom(srcMatSlot); // handles null param by itself
        }

        // Slot value copy: MaterialSlot to MaterialSlot
        private static void CopySlotValue(MaterialSlot mslotTarget, MaterialSlot mslotSource)
        {
            if (mslotTarget.concreteValueType != mslotTarget.concreteValueType)
                return;
            mslotTarget.CopyValuesFrom(mslotSource);
        }

        private interface IGenericNameToSlotId
        {
            bool GetSlotIdFromGenericName(GenericSlotNames slotName, out int slotId, bool wantInputSlot = true);
        }

        private class MasterNodeTranslator : IGenericNameToSlotId
        {
            private Dictionary<GenericSlotNames, int> m_Mapping;
            public MasterNodeTranslator(Dictionary<GenericSlotNames, int> mapping)
            {
                m_Mapping = mapping;
            }
            public static implicit operator MasterNodeTranslator(Dictionary<GenericSlotNames, int> mapping) => new MasterNodeTranslator(mapping);

            public bool GetSlotIdFromGenericName(GenericSlotNames slotName, out int slotId, bool wantInputSlot = true)
            {
                if (wantInputSlot == false) // no output slot on a masternode
                {
                    slotId = k_SlotIdNone;
                    return false;
                }
                return CrossSectionEnabler.GetSlotIdFromGenericName(slotName, m_Mapping, out slotId);
            }
        }

        private class CrossSectionNodeTranslator : IGenericNameToSlotId
        {
            private Dictionary<GenericSlotNames, Tuple<int, int>> m_Mapping;
            public CrossSectionNodeTranslator(Dictionary<GenericSlotNames, Tuple<int, int>> mapping)
            {
                m_Mapping = mapping;
            }
            public static implicit operator CrossSectionNodeTranslator(Dictionary<GenericSlotNames, Tuple<int, int>> mapping) => new CrossSectionNodeTranslator(mapping);

            public bool GetSlotIdFromGenericName(GenericSlotNames slotName, out int slotId, bool wantInputSlot = true)
            {
                return CrossSectionEnabler.GetSlotIdFromGenericName(slotName, m_Mapping, out slotId, wantInputSlot);
            }
        }

        // These copy value function are more abstract and can use our GenericSlotNames semantics, and since these need to be qualified as an input
        // or output for the cross section node, some function names specify that they copy *input* slots only:

        // CopySlotValue: generic name to generic name
        private static void CopyInputSlotValueByGenericName(AbstractMaterialNode dstNode, GenericSlotNames dstSlotName, IGenericNameToSlotId dstTranslator,
                                                            AbstractMaterialNode srcNode, GenericSlotNames srcSlotName, IGenericNameToSlotId srcTranslator)
        {
            dstTranslator.GetSlotIdFromGenericName(dstSlotName, out int dstSlotId);
            srcTranslator.GetSlotIdFromGenericName(srcSlotName, out int srcSlotId);

            if (dstSlotId != k_SlotIdNone && srcSlotId != k_SlotIdNone)
                CopySlotValue(dstNode, dstSlotId, srcNode, srcSlotId);
        }
        // CopySlotValue: MaterialSlot ref to generic name
        private static void CopyInputSlotValueByGenericName(AbstractMaterialNode dstNode, GenericSlotNames dstSlotName, IGenericNameToSlotId dstTranslator,
                                                            MaterialSlot srcMaterialSlot)
        {
            dstTranslator.GetSlotIdFromGenericName(dstSlotName, out int dstSlotId);
            if (dstSlotId != k_SlotIdNone)
            {
                CopySlotValue(dstNode, dstSlotId, srcMaterialSlot);
            }
        }

        private static void SetVector1MaterialSlotValueByGenericName(AbstractMaterialNode node, GenericSlotNames slotName, IGenericNameToSlotId translator, float value)
        {
            if (!translator.GetSlotIdFromGenericName(slotName, out int slotId))
                return;

            Vector1MaterialSlot mslot = node.FindSlot<Vector1MaterialSlot>(slotId);
            if (mslot == null)
                return;

            mslot.value = value;
        }

        private static void SetMaterialSlotValueByGenericName<T>(AbstractMaterialNode node, GenericSlotNames slotName, IGenericNameToSlotId translator, T value)
        {
            if (!translator.GetSlotIdFromGenericName(slotName, out int slotId))
                return;

            // todo
            if (typeof(T) == typeof(bool))
            {
                BooleanMaterialSlot mslot = node.FindSlot<BooleanMaterialSlot>(slotId);
                if (mslot == null)
                    return;
                mslot.value = (bool)(object)value;
            }
            else if (typeof(T) == typeof(float))
            {
                Vector1MaterialSlot mslot = node.FindSlot<Vector1MaterialSlot>(slotId);
                if (mslot == null)
                    return;
                mslot.value = (float)(object)value;
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        private static bool GetMaterialSlotValueByGenericName<T>(AbstractMaterialNode node, GenericSlotNames slotName, IGenericNameToSlotId translator, out T value)
        {
            value = default(T);
            if (!translator.GetSlotIdFromGenericName(slotName, out int slotId))
                return false;

            // todo
            if (typeof(T) == typeof(bool))
            {
                BooleanMaterialSlot mslot = node.FindSlot<BooleanMaterialSlot>(slotId);
                if (mslot == null)
                    return false;
                value = (T)(object)mslot.value;
            }
            else if (typeof(T) == typeof(float))
            {
                Vector1MaterialSlot mslot = node.FindSlot<Vector1MaterialSlot>(slotId);
                if (mslot == null)
                    return false;
                value = (T)(object)mslot.value;
            }
            else
            {
                throw new NotImplementedException();
            }
            return true;
        }


        private static MaterialSlot GetMaterialSlotByGenericName(AbstractMaterialNode node, GenericSlotNames slotName, IGenericNameToSlotId translator,
                                                                 bool wantInputSlot = true)
        {
            translator.GetSlotIdFromGenericName(slotName, out int slotId, wantInputSlot);
            if (slotId != k_SlotIdNone)
            {
                return node.FindSlot<MaterialSlot>(slotId);
            }
            return null;
        }

        // Find what feeds inputSlot
        private static bool GetUpStreamSlotRef(GraphData graphData, MaterialSlot inputSlot, out SlotReference fromOuputSlotReference)
        {
            bool success = false;
            fromOuputSlotReference = new SlotReference();
            Debug.Assert(inputSlot.owner.owner == graphData, "inputSlot.owner.owner == graphData");

            var edges = graphData.GetEdges(inputSlot.slotReference).ToArray();
            if (edges.Any())
            {
                // Normally this should be true as before calling GetUpStreamSlotRef we check if the slot is connected!
                var fromSlotRef = edges[0].outputSlot;
                var fromNode = graphData.GetNodeFromGuid<AbstractMaterialNode>(fromSlotRef.nodeGuid);

                // TODO: see AbstractMaterialNode.GetSlotValue: how are these possible anyway?
                // probably during a tangling of uninterrupted calls but before ValidateGraph().
                // So we shouldn't need those here:
                if (fromNode != null)
                {
                    var slot = fromNode.FindOutputSlot<MaterialSlot>(fromSlotRef.slotId);
                    if (slot != null)
                    {
                        fromOuputSlotReference = fromSlotRef;
                        success = true;
                    }
                }
            }
            return success;
        }

        // Connect: SlotReference (ie node guid, slotId) to generic name
        private static bool ConnectSlots(GraphData graphData,
                                         SlotReference fromOutSlotRef,
                                         AbstractMaterialNode dstNode, GenericSlotNames dstInputSlotName, IGenericNameToSlotId dstTranslator)
        {
            if (dstTranslator.GetSlotIdFromGenericName(dstInputSlotName, out int dstSlotId))
            {
                graphData.Connect(fromOutSlotRef, new SlotReference(dstNode.guid, dstSlotId));
                return true;
            }
            return false;
        }

        // Connect: generic name to SlotReference (ie node guid, slotId)
        private static bool ConnectSlots(GraphData graphData,
                                         AbstractMaterialNode srcNode, GenericSlotNames srcOutSlotName, IGenericNameToSlotId srcTranslator,
                                         SlotReference toInputSlotRef)
        {
            // Important: wantInputSlot:false : if we're getting called to connect the cross section subgraph node,
            // we need to connect its output slotid for the given srcOutSlotName semantic name 
            if (srcTranslator.GetSlotIdFromGenericName(srcOutSlotName, out int srcOutSlotId, wantInputSlot:false))
            {
                graphData.Connect(new SlotReference(srcNode.guid, srcOutSlotId), toInputSlotRef);
                return true;
            }
            return false;
        }

        private bool UnSpliceCrossSectionSubGraphModule(SubGraphNode alreadyPresentSubGraphNode, string assetPath, GraphData graphData,
                                                        MasterNodeInfo masterNodeInfo, IMasterNode masterNode, AbstractMaterialNode masterMatNode)
        {
            foreach (var slotNameAndSlotId in masterNodeInfo.GenericSlotNamesToSlotId)
            {
                // For the given semantic, get both the already present cross section subgraph node and master node
                // Material Slot. If there's a connection that we spliced and we need to restore, those will be present:
                var subgraphNodeInputSlot = GetMaterialSlotByGenericName(alreadyPresentSubGraphNode, slotNameAndSlotId.Key, (CrossSectionNodeTranslator)genericSlotNamesToCrossSectionIOSlotIds);
                var subgraphNodeOutputSlot = GetMaterialSlotByGenericName(alreadyPresentSubGraphNode, slotNameAndSlotId.Key, (CrossSectionNodeTranslator)genericSlotNamesToCrossSectionIOSlotIds,
                                                                          wantInputSlot: false);
                var masterInputSlot = masterMatNode.FindSlot<MaterialSlot>(slotNameAndSlotId.Value);

                // We need those three MaterialSlot present for a connection to be restored, and we never have to
                // restore any values since we avoid modifying slot values on the master node.
                if (masterInputSlot != null && subgraphNodeInputSlot != null && subgraphNodeOutputSlot != null)
                {
                    if (subgraphNodeInputSlot.isConnected)
                    {
                        //Debug.Assert(alreadyPresentSubGraphNode.owner == graphData, "alreadyPresentSubGraphNode.owner == graphData");
                        if (!GetUpStreamSlotRef(graphData, subgraphNodeInputSlot, out SlotReference fromSlotRef))
                        {
                            Debug.LogWarning(string.Format("{0}: serious connection inconsistency found.", assetPath));
                            return false;// skip this shadergraph
                        }

                        // Try to connect this remote slot to a matching semantic/generic input named slot back on our
                        // master node:
                        // Note we don't need to disconnect the existing edge from fromSlotRef to the masterInputSlot:
                        if (null == graphData.Connect(fromSlotRef, masterInputSlot.slotReference))
                        {
                            Debug.LogWarning(string.Format("{0}: Can't connect back to master node an original edge on cross section subgraph node for semantic {1}",
                                             assetPath, slotNameAndSlotId.Key));
                            // in that case we will just not care
                        }
                    }
                }
            }
            // TODOTODO: remove property nodes we might have added
            bool alphaTestSavedToggleValue;
            if (!GetMaterialSlotValueByGenericName<bool>(alreadyPresentSubGraphNode, GenericSlotNames.AlphaTestSavedState, (CrossSectionNodeTranslator)genericSlotNamesToCrossSectionIOSlotIds,
                                                          out alphaTestSavedToggleValue))
            {
                // This should have been found always, do nothing in that case
                alphaTestSavedToggleValue = true;
                Debug.LogWarning(string.Format("{0}: No AlphaTestSavedState input on existing cross section subgraph node.", assetPath));
            }
            // If alphaTestSavedToggleValue was already true, nothing to do, otherwise, we will disable alphaTest on the master node.
            if (!alphaTestSavedToggleValue)
            {
                SetMasterNodeAlphaTestToggle(masterNodeInfo, masterNode, toggleValue: alphaTestSavedToggleValue);
            }

            float doubleSidedSavedState;
            if (!GetMaterialSlotValueByGenericName<float>(alreadyPresentSubGraphNode, GenericSlotNames.DoubleSidedSavedState, (CrossSectionNodeTranslator)genericSlotNamesToCrossSectionIOSlotIds,
                                                          out doubleSidedSavedState))
            {
                // This should have been found always, do nothing in that case
                doubleSidedSavedState = k_SimplePropertyStateInvalidValue;
                Debug.LogWarning(string.Format("{0}: No DoubleSidedSavedState input on existing cross section subgraph node.", assetPath));
            }
            SetSimplePropertyFromFloat(masterNode, masterNodeInfo.DoubleSidedProperty, doubleSidedSavedState);

            // Note: nothing to do for the connector, we just remove our cross section subgraph node and it will disconnect
            // from the per shadergraph input connector.

            // Finally we can remove the node. We don't need to disconnect the edges, graph validation will remove them
            graphData.RemoveNode(alreadyPresentSubGraphNode);

            return true;
        }

        public bool SpliceShaderGraphForCrossSection(string assetPath, out GraphData outGraphData, bool removeCrossSection = false)
        {
            outGraphData = null;
            bool spliceOk = false;
            var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
            if (asset == null)
                return spliceOk;
            if (!EditorUtility.IsPersistent(asset))
                return spliceOk;

            var textGraph = File.ReadAllText(assetPath, Encoding.UTF8);
            GraphData graphData = JsonUtility.FromJson<GraphData>(textGraph);
            graphData.assetGuid = AssetDatabase.AssetPathToGUID(assetPath);
            graphData.isSubGraph = false;
            graphData.messageManager = messageManager;
            graphData.OnEnable();
            graphData.ValidateGraph();
            outGraphData = graphData;

            // Check if we support this master node:
            IMasterNode masterNode = ((IMasterNode)graphData.outputNode);
            AbstractMaterialNode masterMatNode = masterNode as AbstractMaterialNode;
            if (masterNode == null || masterMatNode == null)
            {
                Debug.Log(string.Format("{0}: can't find a masterNode, skipping.", assetPath));
                return spliceOk;
            }
            if (masterMatNode.owner != graphData)
            {
                Debug.LogWarning(string.Format("{0}: masterMatNode.owner != graphData", assetPath));
                return spliceOk;
            }
            if (false == masterNodeTypeToNodeInfo.TryGetValue(masterNode.GetType(), out MasterNodeInfo masterNodeInfo))
            {
                Debug.Log(string.Format("{0} doesn't have a supported MasterNode ({1}), skipping.", assetPath, masterNode.GetType()));
                return spliceOk;
            }

            // Create our cross section subgraph node:
            SubGraphNode crossSectionSubGraphNode = CreateCrossSectionSubGraphNode();
            if (crossSectionSubGraphNode == null)
            {
                Debug.LogWarning("Can't CreateCrossSectionSubGraphNode.");
                return spliceOk;
            }

            // Check if our cross section subgraph node is already there
            SubGraphNode alreadyPresentSubGraphNode = graphData.GetNodes<SubGraphNode>().Where(someSubGraphNode => someSubGraphNode.subGraphGuid == mainConfig.crossSectionSubGraphGuid).FirstOrDefault();
            if (alreadyPresentSubGraphNode != null)
            {
                // Remove our spliced module first
                Debug.Log(string.Format("{0}: found our cross section subgraph node, removing it.", assetPath));
                if (!UnSpliceCrossSectionSubGraphModule(alreadyPresentSubGraphNode, assetPath, graphData, masterNodeInfo, masterNode, masterMatNode))
                    return spliceOk;
                if (removeCrossSection)
                    return true;
            }

            // Save the alphaTest enabled state for the master node, we will need it.
            // If the node doesn't have one but we've reached this point in the code (it is listed in the masterNodeAlphaToggleAndSlotIds
            // table, thus an AlphaTreshold slot was found to be declared on it), it means the master node is something like PBRMasterNode,
            // and we infer the alphaTest is always on.
            bool alphaTestWasEnabled = GetMasterNodeAlphaTestToggle(masterNodeInfo, masterNode);

            // Save the alphaTest state on our crossSection node
            // todotodo
            //SetVector1MaterialSlotValueByGenericName(crossSectionSubGraphNode, GenericSlotNames.AlphaTestSavedState, (CrossSectionNodeTranslator)genericSlotNamesToCrossSectionIOSlotIds,
            //                                         k_AlphaThresholdDisabledEquivalent);
            SetMaterialSlotValueByGenericName<bool>(crossSectionSubGraphNode, GenericSlotNames.AlphaTestSavedState, (CrossSectionNodeTranslator)genericSlotNamesToCrossSectionIOSlotIds,
                                                    alphaTestWasEnabled);

            if (alphaTestWasEnabled == false)
            {
                // Our subgraph requires alphaTest enabled, enable it.
                SetMasterNodeAlphaTestToggle(masterNodeInfo, masterNode, toggleValue: true);
            }

            // Save and set the DoubleSidedMode / toggle
            // TODOTODO: depending on the style wanted (filled or transparent cut), toggle back face or not
            float doubleSidedSavedState = GetSimplePropertyAsFloat(masterNode, masterNodeInfo.DoubleSidedProperty);
            SetMaterialSlotValueByGenericName<float>(crossSectionSubGraphNode, GenericSlotNames.DoubleSidedSavedState, (CrossSectionNodeTranslator)genericSlotNamesToCrossSectionIOSlotIds,
                                                     doubleSidedSavedState);
            SetSimplePropertyFromFloat(masterNode, masterNodeInfo.DoubleSidedProperty, masterNodeInfo.DoubleSidedEnabledValue); // see TODOTODO

            // Splice in our subgraph node.
            graphData.AddNode(crossSectionSubGraphNode);

            // We iterate the slot name semantics we're interested in splicing on the master node.
            // If the slot is found and is connected, we disconnect the edge and connect it to our
            // subgraph node instead, in an "cross section bypass" input port while we connect a
            // corresponding subgraph ouput port ("overriding" the original master node input source)
            // to the master node port we disconnected.
            // If the slot wasn't connected, we simply copy values from the master node port to the
            // corresponding aforementioned "cross section bypass" value.
            //
            // Note that we need to find a match to *inputs* on our subgraph before disconnecting:
            // We consider these as overrides.
            // Also note that even if we found some generic named slotId *field* declared in the master
            // node class (by reflection), depending on its config, the port itself could be closed
            // (ie the MaterialSlot will not be there).
            // We dont do anything special for this case, as the port we need open is the AlphaThreshold.
            // The rest need overrides only if they are open once alphaTest is turned on.

            //foreach (var genericSlotName in Enum.GetValues(typeof(GenericSlotNames))
            //                                    .Cast<GenericSlotNames>()
            //                                    .Where(name => name < GenericSlotNames.MasterNodesGenericInputNamesMaxIdx))
            //{
            //}
            foreach (var slotNameAndSlotId in masterNodeInfo.GenericSlotNamesToSlotId)
            {
                var masterInputSlot = masterMatNode.FindSlot<MaterialSlot>(slotNameAndSlotId.Value);
                if (masterInputSlot != null)
                {
                    if (masterInputSlot.isConnected)
                    {
                        //Debug.Assert(masterMatNode.owner == graphData, "masterMatNode.owner == graphData");
                        if (!GetUpStreamSlotRef(graphData, masterInputSlot, out SlotReference fromSlotRef))
                        {
                            Debug.LogWarning(string.Format("{0}: serious connection inconsistency found.", assetPath));
                            return spliceOk;// skip it
                        }

                        // Try to connect this remote slot to a matching semantic/generic input named slot on our
                        // cross section subgraph node:
                        if (!ConnectSlots(graphData, fromSlotRef, crossSectionSubGraphNode, slotNameAndSlotId.Key, (CrossSectionNodeTranslator)genericSlotNamesToCrossSectionIOSlotIds))
                        {
                            Debug.LogWarning(string.Format("{0}: Can't find an input port to connect original edge on cross section subgraph node for semantic {1}",
                                             assetPath, slotNameAndSlotId.Key));

                            // in that case we will just not care and still try to connect our subgraph to the master node
                        }

                        // Note we don't disconnect the existing edge from fromSlotRef to the masterInputSlot:
                        // GraphData.Connect() will find it is already connected and will delete the edge before connecting
                        // a new one:
                        if (!ConnectSlots(graphData, crossSectionSubGraphNode, slotNameAndSlotId.Key, (CrossSectionNodeTranslator)genericSlotNamesToCrossSectionIOSlotIds,
                                          masterInputSlot.slotReference))
                        {
                            Debug.LogWarning(string.Format("{0}: Can't find an adequate output port on cross section subgraph node to connect to the master node for overriding semantic {1}",
                                             assetPath, slotNameAndSlotId.Key));
                        }
                    }
                    else
                    {
                        CopyInputSlotValueByGenericName(crossSectionSubGraphNode, slotNameAndSlotId.Key, (CrossSectionNodeTranslator)genericSlotNamesToCrossSectionIOSlotIds,
                                                        masterInputSlot);
                        if (!ConnectSlots(graphData, crossSectionSubGraphNode, slotNameAndSlotId.Key, (CrossSectionNodeTranslator)genericSlotNamesToCrossSectionIOSlotIds,
                                          masterInputSlot.slotReference))
                        {
                            Debug.LogWarning(string.Format("{0}: Can't find an adequate output port on cross section subgraph node to connect to the master node for overriding semantic {1}",
                                             assetPath, slotNameAndSlotId.Key));
                        }
                    }
                }
            }

            if (! alphaTestWasEnabled)
            {
                // Also, we need to clean up any value on slots that just opened because alphaTest changed:
                // We want to avoid feeding values for those "alphaTest state dependent port" that would modify
                // the original appearance when the cutting tool actually doesn't reject the fragment:
                //
                // In that case, our cross section subgraph will forward whatever was on slots that we override,
                // but in the case of any slots *that just opened because we enabled alphaTest*, those values won't do:
                //
                // We will set values (if possible) that are as if the slot was actually closed.
                // For now, the only thing we handle are AlphaThresholds slots so that is possible:
                // ie A negative threshold will always let any fragment pass the clipping test.
                //
                // To recap: We will set "alpha test disabled" equivalent values on all our cross section node input
                // that represent the original values (cross section bypass) to feed when cutting the fragment
                // should not happen for all alpha threshold semantics that weren't even opened on the master node
                // to begin with (before we toggled the alphaTest = on).
                foreach (var genericSlotName in mainConfig.alphaThresholdSlotsDependingOnAlphaClipEnable)
                {
                    SetVector1MaterialSlotValueByGenericName(crossSectionSubGraphNode, genericSlotName, (CrossSectionNodeTranslator)genericSlotNamesToCrossSectionIOSlotIds,
                                                             k_AlphaThresholdDisabledEquivalent);
                }
            }

            // Try to find our connector in the processed shader graph and if found, connect it to the cross section
            // node so that per shader graph material (local settings) can be forwarded to the cross section node and
            // change its behavior on a per shader graph / material basis.
            SubGraphNode connectorNode = graphData.GetNodes<SubGraphNode>().Where(someSubGraphNode => someSubGraphNode.subGraphGuid == mainConfig.crossSectionPerMaterialInputConnectorGuid).FirstOrDefault();
            if (connectorNode != null)
            {
                foreach(var connectionToDo in crossSectionPerMaterialInputConnectorSlotMapping)
                {
                    var fromSlot = new SlotReference(connectorNode.guid, connectionToDo.Key);
                    var toSlot = new SlotReference(crossSectionSubGraphNode.guid, connectionToDo.Value);
                    graphData.Connect(fromSlot, toSlot);
                }
            }

            // Finally, there are some local inputs we want to have defaults on them if the adapter didn't connect there
            // or wasn't found (eg the enable we want On as default). If the slot was connected, the value is harmless and
            // bypassed.
            SetMaterialSlotValueByGenericName<bool>(crossSectionSubGraphNode, GenericSlotNames.CrossSectionEnable, (CrossSectionNodeTranslator)genericSlotNamesToCrossSectionIOSlotIds,
                                                    true);
            SetMaterialSlotValueByGenericName<bool>(crossSectionSubGraphNode, GenericSlotNames.MeshIsNotClosedHint, (CrossSectionNodeTranslator)genericSlotNamesToCrossSectionIOSlotIds,
                                                    false);

            spliceOk = true;
            return spliceOk;
        }

    } // CrossSectionEnablerInfo

    // TODOTODO Make a true editor window for this with options, selector for ICrossSectionEnablerConfig, etc.
    public class CrossSectionShaderGraphsEditor
    {
        public static class DialogText
        {
            public static readonly string title = "Transform ShaderGraphs To Enable Cross Section";
            public static readonly string proceed = "Proceed";
            public static readonly string ok = "Ok";
            public static readonly string cancel = "Cancel";
            public static readonly string noSelectionMessage = "You must select at least one ShaderGraph shader.";
            public static readonly string projectBackMessage = "Make sure to have a project backup before proceeding.";
        }

        public static void SpliceShaderGraphsForCrossSectionOnSelected(string progressBarName, bool removeSplice = false, ICrossSectionEnablerConfig xseConfig = null)
        {
            var selection = Selection.objects;

            if (selection == null)
            {
                EditorUtility.DisplayDialog(DialogText.title, DialogText.noSelectionMessage, DialogText.ok);
                return;
            }

            List<Shader> selectedShaders = new List<Shader>(selection.Length);
            for (int i = 0; i < selection.Length; ++i)
            {
                Shader shader = selection[i] as Shader;
                ShaderGraphImporter importer = null;
                if (shader != null)
                    importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(shader)) as ShaderGraphImporter;
                else
                    Debug.Log(string.Format("{0} contains no Shader. Make sure to import it first.", AssetDatabase.GetAssetPath(selection[i])));
                if (importer != null)
                    selectedShaders.Add(shader);
                else
                    Debug.Log(string.Format("{0} doesn't seem to be a ShaderGraph Shader. Ignoring.", AssetDatabase.GetAssetPath(selection[i])));
            }

            int selectedShadersCount = selectedShaders.Count;
            if (selectedShadersCount == 0)
            {
                EditorUtility.DisplayDialog(DialogText.title, DialogText.noSelectionMessage, DialogText.ok);
                return;
            }

            if (!EditorUtility.DisplayDialog(DialogText.title, string.Format("This will overwrite {0} selected ShaderGraph{1}. ", selectedShadersCount, selectedShadersCount > 1 ? "s" : "") +
                    DialogText.projectBackMessage, DialogText.proceed, DialogText.cancel))
                return;

            // Prepare and grather all info we need to modify each ShaderGraph selected.
            // TODOTODO: discover all ICrossSectionEnablerConfig and list them so the user can select what to splice
            CrossSectionEnabler crossSectionEnabler = new CrossSectionEnabler(xseConfig ?? new CrossSectionEnablerConfig());

            string lastShaderName = "";
            for (int i = 0; i < selectedShadersCount; i++)
            {
                if (UnityEditor.EditorUtility.DisplayCancelableProgressBar(progressBarName, string.Format("({0} of {1}) {2}", i, selectedShadersCount, lastShaderName), (float)i / (float)selectedShadersCount))
                    break;

                var shader = selectedShaders[i];

                lastShaderName = shader.name;
                ShaderGraphImporter importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(shader)) as ShaderGraphImporter;
                Debug.Assert(importer != null, "importer != null");
                // TODO: Are these necessary checks ?
                var extension = Path.GetExtension(importer.assetPath);
                if (!string.IsNullOrEmpty(extension))
                {
                    extension = extension.Substring(1).ToLowerInvariant();
                }                
                if (string.IsNullOrEmpty(extension) || (extension != ShaderGraphImporter.Extension && extension != ShaderSubGraphImporter.Extension))
                {
                    Debug.LogWarning(string.Format("{0} doesnt have a ShaderGraph extension ? Skipping.", importer.assetPath));
                    continue;
                }

                // Process the selected ShaderGraph and save it
                // TODOTODO part of the UI again to switch *material users*, and instead create alternate shadergraphs, like
                // _xse.shadegraph (cross section enabled)
                if (crossSectionEnabler.SpliceShaderGraphForCrossSection(importer.assetPath, out GraphData outGraphData, removeCrossSection: removeSplice))
                {
                    bool VCSEnabled = (VersionControl.Provider.enabled && VersionControl.Provider.isActive);
                    CheckoutIfValid(importer.assetPath, VCSEnabled);
                    File.WriteAllText(importer.assetPath, EditorJsonUtility.ToJson(outGraphData, true));
                    AssetDatabase.ImportAsset(importer.assetPath);
                }
            }

            UnityEditor.EditorUtility.ClearProgressBar();
        }

        // TODO: Collab stuff copied for now from MaterialGraphEditWindow
        private static void CheckoutIfValid(string path, bool VCSEnabled)
        {
            if (VCSEnabled)
            {
                var asset = VersionControl.Provider.GetAssetByPath(path);
                if (asset != null)
                {
                    if (VersionControl.Provider.CheckoutIsValid(asset))
                    {
                        var task = VersionControl.Provider.Checkout(asset, VersionControl.CheckoutMode.Both);
                        task.Wait();

                        if (!task.success)
                            Debug.Log(task.text + " " + task.resultCode);
                    }
                }
            }
        }

    }
}
