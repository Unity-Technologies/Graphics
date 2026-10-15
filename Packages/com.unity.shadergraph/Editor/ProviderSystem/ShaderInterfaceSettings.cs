using System.Collections.Generic;
using Unity.GraphAuthoring.Editor.ProviderSystem;
using UnityEditor.Searcher;
using UnityEditor.ShaderGraph.Drawing;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.ProviderSystem
{
    sealed class ShaderInterfaceSettings : VisualElement
    {
        readonly struct Scope
        {
            readonly string m_Interface;
            readonly int m_SubShader;
            readonly string m_Pass;

            internal Scope(string interfaceKey, int subShader, string pass = "")
            {
                m_Interface = interfaceKey;
                m_SubShader = subShader;
                m_Pass = pass;
            }

            internal string Key(string group) => $"{m_Interface}/subshader{m_SubShader}/{m_Pass}/{group}";

            internal bool Contains(IEnumerable<IShaderPass> scopes)
            {
                foreach (var scope in scopes)
                    if (scope.SubShaderIndex == m_SubShader && scope.Name == m_Pass)
                        return true;
                return false;
            }
        }

        const string k_Class = "sg-shader-interface";

        static readonly Dictionary<string, bool> s_ExpandedFoldouts = new();

        readonly GraphData m_Graph;
        readonly List<IVirtualFunction> m_Functions = new();
        readonly List<IPartialStruct> m_Structs = new();
        readonly List<IShaderPass> m_Passes = new();
        string m_InterfaceKey;

        internal ShaderInterfaceSettings(GraphData graph)
        {
            m_Graph = graph;
            name = "shaderInterfaceSettings";
            styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>("Packages/com.unity.shadergraph/Editor/UIResources/Styles/ShaderInterfaceSettings.uss"));
            Rebuild();
        }

        void Rebuild()
        {
            Clear();
            m_Functions.Clear();
            m_Structs.Clear();
            m_Passes.Clear();

            var title = new Label("Shader Interface");
            title.AddToClassList($"{k_Class}__title");
            Add(new PropertyRow(title));

            var provider = m_Graph.shaderInterface;
            var pickerLine = new VisualElement();
            pickerLine.AddToClassList($"{k_Class}__picker-line");
            pickerLine.Add(CreatePicker(provider?.ProviderKey));
            Add(CreateRow("Interface", pickerLine));

            if (provider == null)
            {
                Add(new HelpBoxRow("Select a shader interface to override its virtual functions.", MessageType.Info));
                return;
            }

            if (!provider.IsValid)
            {
                Add(new HelpBoxRow($"Shader interface '{provider.ProviderKey}' can't be found or failed to load. Select another interface or restore its source file.", MessageType.Warning));
                return;
            }

            m_InterfaceKey = provider.AssetID.ToString();
            pickerLine.Add(CreateShowInProjectButton(AssetDatabase.GUIDToAssetPath(provider.AssetID)));
            AddDefinition(provider.Definition);
        }

        void AddDefinition(IShaderInterface definition)
        {
            if (definition.BaseShaderID != default)
                Add(CreateRow("Base Shader", CreateReadOnlyAssetField(AssetDatabase.GUIDToAssetPath(definition.BaseShaderID))));

            AddProperties(definition.Fields);

            m_Functions.AddRange(definition.VirtualFunctions);
            m_Structs.AddRange(definition.PartialStructs);
            m_Passes.AddRange(definition.Passes);
            foreach (var subShader in SubShaderIndices())
                AddSubShader(subShader);
        }

        void AddProperties(IEnumerable<IShaderField> fields)
        {
            var properties = new List<IShaderField>(fields);
            if (properties.Count == 0)
                return;

            var foldout = AddFoldout(this, $"{m_InterfaceKey}/properties", $"Properties ({properties.Count})");
            foreach (var property in properties)
                foldout.Add(CreateEntry($"{ShaderObjectUtils.QualifyName(property.ShaderType)} {property.Name}"));
        }

        void AddSubShader(int subShader)
        {
            var scope = new Scope(m_InterfaceKey, subShader);
            var foldout = AddFoldout(this, scope.Key(""), $"SubShader {subShader}");
            AddDeclarations(foldout, scope);

            var passes = m_Passes.FindAll(p => p.SubShaderIndex == subShader);
            if (passes.Count == 0)
                return;

            var passesFoldout = AddFoldout(foldout, scope.Key("passes"), $"Passes ({passes.Count})");
            foreach (var pass in passes)
                AddPass(passesFoldout, new Scope(m_InterfaceKey, subShader, pass.Name), pass.Name);
        }

        void AddPass(VisualElement parent, Scope scope, string passName)
        {
            var foldout = AddFoldout(parent, scope.Key(""), passName);
            if (!AddDeclarations(foldout, scope))
                foldout.Add(CreateDetail("Nothing is declared in this pass."));
        }

        bool AddDeclarations(VisualElement parent, Scope scope)
        {
            bool hasFunctions = AddFunctions(parent, scope);
            bool hasStructs = AddStructs(parent, scope);
            return hasFunctions || hasStructs;
        }

        bool AddFunctions(VisualElement parent, Scope scope)
        {
            var functions = m_Functions.FindAll(f => scope.Contains(f.Scopes));
            if (functions.Count == 0)
                return false;

            var foldout = AddFoldout(parent, scope.Key("functions"), $"Virtual Functions ({functions.Count})");
            foreach (var function in functions)
                foldout.Add(CreateEntry(ShaderObjectUtils.QualifySignature(function)));
            return true;
        }

        bool AddStructs(VisualElement parent, Scope scope)
        {
            List<(IPartialStruct partialStruct, List<IShaderStructField> fields)> structs = new();
            foreach (var partialStruct in m_Structs)
            {
                var fields = FieldsIn(partialStruct, scope);
                if (fields.Count > 0 || scope.Contains(partialStruct.Scopes))
                    structs.Add((partialStruct, fields));
            }

            if (structs.Count == 0)
                return false;

            var foldout = AddFoldout(parent, scope.Key("structs"), $"Partial Structs ({structs.Count})");
            foreach (var (partialStruct, fields) in structs)
            {
                foldout.Add(CreateEntry(ShaderObjectUtils.QualifyName(partialStruct)));
                foreach (var field in fields)
                    foldout.Add(CreateFieldLine(field));
            }
            return true;
        }

        static List<IShaderStructField> FieldsIn(IPartialStruct partialStruct, Scope scope)
        {
            List<IShaderStructField> fields = new();
            foreach (var field in partialStruct.Fields)
                if (field is IShaderStructField structField && scope.Contains(structField.Scopes))
                    fields.Add(structField);
            return fields;
        }

        SortedSet<int> SubShaderIndices()
        {
            SortedSet<int> indices = new();
            void AddAll(IEnumerable<IShaderPass> scopes)
            {
                foreach (var scope in scopes)
                    indices.Add(scope.SubShaderIndex);
            }

            AddAll(m_Passes);
            foreach (var function in m_Functions)
                AddAll(function.Scopes);
            foreach (var partialStruct in m_Structs)
            {
                AddAll(partialStruct.Scopes);
                foreach (var field in partialStruct.Fields)
                    if (field is IShaderStructField structField)
                        AddAll(structField.Scopes);
            }
            return indices;
        }

        static Foldout AddFoldout(VisualElement parent, string key, string text)
        {
            s_ExpandedFoldouts.TryGetValue(key, out var expanded);

            var foldout = new Foldout { text = text, value = expanded };
            foldout.AddToClassList("MainFoldout");
            foldout.RegisterValueChangedCallback(evt =>
            {
                if (evt.target == foldout)
                    s_ExpandedFoldouts[key] = evt.newValue;
            });
            parent.Add(foldout);
            return foldout;
        }

        static Label CreateEntry(string text)
        {
            var label = new Label(text) { tooltip = text };
            label.AddToClassList($"{k_Class}__entry");
            return label;
        }

        static Label CreateDetail(string text)
        {
            var label = CreateEntry(text);
            label.AddToClassList($"{k_Class}__detail");
            return label;
        }

        static Label CreateFieldLine(IShaderStructField field)
        {
            var semantic = string.IsNullOrEmpty(field.Semantic) ? string.Empty : $" : {field.Semantic}";
            var line = CreateDetail($"{ShaderObjectUtils.QualifyName(field.ShaderType)} {field.Name}{semantic}");
            line.AddToClassList($"{k_Class}__field");
            return line;
        }

        static PropertyRow CreateRow(string label, VisualElement content)
        {
            var row = new PropertyRow(new Label(label));
            row.Add(content);
            return row;
        }

        static ObjectField CreateReadOnlyAssetField(string path)
        {
            var asset = AssetDatabase.LoadMainAssetAtPath(path);
            var field = new ObjectField { objectType = typeof(Object), allowSceneObjects = false, value = asset };
            field.RegisterValueChangedCallback(_ => field.SetValueWithoutNotify(asset));
            field.RegisterCallback<AttachToPanelEvent>(_ => HideObjectPicker(field));
            return field;
        }

        static void HideObjectPicker(ObjectField field)
        {
            var selector = field.Q(className: ObjectField.selectorUssClassName);
            if (selector != null)
                selector.style.display = DisplayStyle.None;
        }

        static Button CreateShowInProjectButton(string path)
        {
            var button = new Button(() => ShowInProject(path)) { tooltip = $"Show in Project\n{path}" };
            button.AddToClassList($"{k_Class}__show-in-project");
            button.Add(new Image { image = EditorGUIUtility.IconContent("Search Icon").image, scaleMode = ScaleMode.ScaleToFit });
            return button;
        }

        static void ShowInProject(string path)
        {
            var asset = AssetDatabase.LoadMainAssetAtPath(path);
            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
        }

        Button CreatePicker(string interfaceName)
        {
            var picker = new Button();
            picker.AddToClassList(BasePopupField<string, string>.inputUssClassName);
            picker.AddToClassList($"{k_Class}__picker");

            var text = new TextElement { text = interfaceName ?? "None" };
            text.AddToClassList(BasePopupField<string, string>.textUssClassName);
            text.AddToClassList($"{k_Class}__picker-text");
            if (interfaceName == null)
                text.AddToClassList($"{k_Class}__placeholder");
            picker.Add(text);

            var arrow = new VisualElement();
            arrow.AddToClassList(BasePopupField<string, string>.arrowUssClassName);
            picker.Add(arrow);

            picker.clicked += () => ShowSearcher(picker);
            return picker;
        }

        void ShowSearcher(VisualElement picker)
        {
            var host = OwningWindow(picker);
            if (host == null || !ProviderLibrary.TryGetInstance(out var library))
                return;

            List<SearcherItem> items = new();
            foreach (var provider in library.MainProviders<IShaderInterface>())
                items.Add(new SearcherItem(provider.ProviderKey, AssetDatabase.GUIDToAssetPath(provider.AssetID), userData: provider));

            var searcher = new Searcher.Searcher(SearcherDatabase.Create(items, string.Empty, false), new SearchWindowAdapter("Select Shader Interface"));
            SearcherWindow.Show(host, searcher, item => SelectInterface(item?.UserData as IProvider<IShaderInterface>), picker.worldBound.center, null);
        }

        static EditorWindow OwningWindow(VisualElement element)
        {
            foreach (var window in Resources.FindObjectsOfTypeAll<MaterialGraphEditWindow>())
                if (window.rootVisualElement.Contains(element))
                    return window;
            return null;
        }

        bool SelectInterface(IProvider<IShaderInterface> provider)
        {
            if (provider == null)
                return false;

            m_Graph.owner.RegisterCompleteObjectUndo("Change Shader Interface");
            m_Graph.SetShaderInterface(provider);
            Rebuild();
            return true;
        }
    }
}
