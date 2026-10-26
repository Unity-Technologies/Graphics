// Tabbed gallery app for the UI Toolkit Shaders sample.
//
// Modeled on the UGUI sample: a left side panel of tab buttons and a content pane that
// shows exactly one page at a time. The scene's PanelRenderer loads UITKShaderSamples.uxml;
// once its visual tree is cloned this controller wires the side-panel tabs (radio
// selection) and builds every page:
//
//   - All Samples : a responsive grid of every catalogued example, each rendered with its
//                   own material, plus a done/total header (ported from the showcase board).
//   - Buttons     : Simple/Aqua/SciFi/SciFi2 buttons, larger, with size + hover/press state.
//   - Indicators  : Aqua/Dial/Fantasy/SciFi meters, size + an externally animated _MeterValue.
//   - Progress    : Gradient Bar + Progress Circle (meter+timer); Fancy + Simple Loading (size).
//   - Interactive : a row of Tab Button toggles + a Slider that drives the Gradient Bar material.
//
// Every example Material is loaded by its conventional asset path — via the AssetDatabase in
// the editor, and via scene-serialized references (recorded automatically by the editor) in
// player builds. The bindings funnel every shader-property write through UIShaderBinding,
// which clones the material once per element and mutates it in place, so the shared .mat
// assets are never touched and per-frame animation is allocation-free.

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Unity.UI.Shaders.UITK.Sample
{
    /// <summary>
    /// Drives the tabbed "UITK Shader Samples" gallery scene. Attach to the same GameObject as
    /// the <see cref="PanelRenderer"/> that loads UITKShaderSamples.uxml.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(PanelRenderer))]
    public sealed class UITKShaderSampleScene : MonoBehaviour
    {
        // Conventional root of every example Material once the sample is imported.

        // The Simple Button material is the placeholder look for grid cells whose own
        // material has not been authored yet (matches the showcase board's behavior).
        const string k_PlaceholderMaterialPath = "Buttons/Simple Button.mat";

        // One full sweep of an animated meter / progress bar, in seconds. A slow, external
        // sawtooth ramp mirroring the UGUI sample's Timer.cs (the value is driven externally,
        // never baked into the shader).
        const float k_FillPeriodSeconds = 6f;

        // Editor-only validation hook: while this SessionState key holds a value in [0, 1],
        // every meter renders that constant fill instead of animating, so automated captures
        // can compare exact percentages. Erase the key (or restart the editor) to re-enable
        // the animated ramp.
        const string k_ForcedMeterValueKey = "UITKShaderSamples.ForcedMeterValue";

        // Number of columns in the All Samples grid. The catalogue need not be a multiple of
        // this: a partial last row is padded with invisible spacers so cells keep one width.
        const int k_GridColumns = 6;

        PanelRenderer m_PanelRenderer;
        int m_LastHandledVersion = -1;

        // Materials in the sample render through per-element clones the bindings attach. Cache
        // each loaded material by asset path so we only hit the AssetDatabase once.
        readonly Dictionary<string, Material> m_MaterialCache = new Dictionary<string, Material>();

        // FilterFunctionDefinitions backing the backdrop-filter examples, cached by asset path.
        readonly Dictionary<string, FilterFunctionDefinition> m_FilterCache = new Dictionary<string, FilterFunctionDefinition>();

        // Still images drawn behind the backdrop-filter swatches (the content the filter processes).
        readonly Dictionary<string, Texture2D> m_TextureCache = new Dictionary<string, Texture2D>();

        // Load warnings already emitted, so a missing asset logs once instead of once per rebuild.
        readonly HashSet<string> m_WarnedOnce = new HashSet<string>();

        // The default page shown on load. Keep "tab-all" for the shipping sample; flip this to a
        // category's tab name to spot-check that page in isolation while iterating.
        const string k_DefaultTabName = "tab-all";

        // The side-panel tabs, paired with the content page each one reveals. Built in OnUIReloaded.
        readonly List<TabEntry> m_Tabs = new List<TabEntry>();

        // Click-to-zoom lightbox for the Backgrounds page. Rebuilt with each UI reload.
        VisualElement m_ZoomOverlay;

        // Editor-resolved asset references, serialized into the scene so player builds resolve
        // the same Examples-relative paths without the AssetDatabase (the runtime equivalent of
        // the UGUI sample's serialized prefab references). Populated automatically whenever the
        // editor resolves a path; read as the lookup table in players.
        [System.Serializable]
        struct SerializedAsset
        {
            public string path;
            public Object asset;
        }

        [SerializeField, HideInInspector]
        List<SerializedAsset> m_SerializedAssets = new List<SerializedAsset>();

        struct TabEntry
        {
            public Toggle toggle;
            public VisualElement page;
            public string name;
        }

        // ---- Grid catalogue (ported from the showcase board) ------------------------------------

        struct GridEntry
        {
            public string category;
            public string name;
        }

        // Examples whose fill is driven by _MeterValue. Used by the grid cells so the live board
        // animates exactly like the dedicated Indicators / Progress pages.
        static bool IsMeterDriven(string name) => name switch
        {
            "Aqua Meter" or "Dial Meter" or "Progress Circle" or "SciFi Meter" or "Gradient Bar" or "Fantasy Meter" => true,
            _ => false,
        };

        // Backdrop-filter examples: authored as custom UITK filters (post-process shaders) rather
        // than element materials. Each maps a catalogue name to its FilterFunctionDefinition asset
        // (path relative to the Examples root) plus the default parameter values, applied to the
        // grid swatch as a backdrop-filter. Unlike the Shader Graph examples these sample and
        // process whatever is rendered behind the swatch (the animated panel backdrop).
        struct BackdropFilterExample
        {
            public string definitionPath;
            public float[] parameters;
            public string backgroundPath;   // still image drawn behind the swatch for the filter to process
            // Optional animation: while playing, pulse one parameter between min and max over
            // animatePeriodSeconds (a smooth cosine ease). Leave animatePeriodSeconds at 0 for a
            // static filter.
            public int animatedParameter;
            public float animatedMin;
            public float animatedMax;
            public float animatePeriodSeconds;
        }

        static bool TryGetBackdropFilter(string name, out BackdropFilterExample example)
        {
            switch (name)
            {
                case "Pixelation":
                    example = new BackdropFilterExample { definitionPath = "Backgrounds/PixelationFilter.asset", parameters = new[] { 16f }, backgroundPath = "Backgrounds/KirbyCove.png", animatedParameter = 0, animatedMin = 6f, animatedMax = 44f, animatePeriodSeconds = 5f };
                    return true;
                // tiles, blur (spiral radius as a percentage of the element size), dissolve,
                // saturation — mirroring the UGUI demo; the blur pulses so the frost reads clearly.
                case "Blurred Hexagon":
                    example = new BackdropFilterExample { definitionPath = "Backgrounds/BlurredHexagonFilter.asset", parameters = new[] { 6f, 3.5f, 0.98f, 1f }, backgroundPath = "Backgrounds/KirbyCove.png", animatedParameter = 1, animatedMin = 2f, animatedMax = 6f, animatePeriodSeconds = 6f };
                    return true;
                default:
                    example = default;
                    return false;
            }
        }

        // The catalogue, in row-major order for the grid (matches the showcase).
        static GridEntry[] CreateCatalogue()
        {
            var entries = new List<GridEntry>();
            void Add(string category, params string[] names)
            {
                foreach (var n in names)
                    entries.Add(new GridEntry { category = category, name = n });
            }

            Add("Buttons", "Simple Button", "Aqua Button", "SciFi Button", "SciFi2 Button");
            Add("Indicators", "Aqua Meter", "Dial Meter");
            Add("Indicators", "Fantasy Meter", "SciFi Meter");
            Add("ProgressBars", "Fancy Loading", "Gradient Bar", "Progress Circle", "Simple Loading");
            Add("Backgrounds", "80s Sunset", "Animated Clouds", "Blurred Hexagon", "Halftone",
                "Lava Lamp", "Pixelation");
            Add("Backgrounds", "Rounded Rectangle Bubble", "Tech Grid", "Warped Gradient");
            // Tab Button is a real interactive effect and lives at the top level of Examples/.
            Add("", "Tab Button");

            return entries.ToArray();
        }

        void OnEnable()
        {
            m_PanelRenderer = GetComponent<PanelRenderer>();
            m_PanelRenderer.RegisterUIReloadCallback(OnUIReloaded);
        }

        void OnDisable()
        {
            if (m_PanelRenderer != null)
                m_PanelRenderer.UnregisterUIReloadCallback(OnUIReloaded);
        }

        void OnUIReloaded(PanelRenderer pr, VisualElement root, int version)
        {
            // PanelRenderer raises the reload callback whenever the tree is (re)built; guard
            // against rebuilding our bindings for a version we have already wired.
            if (version == m_LastHandledVersion)
                return;
            m_LastHandledVersion = version;
            BuildApp(root);
        }

        // Resolve an example Material from a path relative to the Examples root: through the
        // AssetDatabase in the editor (recording the reference into m_SerializedAssets so the
        // saved scene carries it), and through that serialized registry in players.
        Material LoadMaterial(string relativePath)
        {
            // Unity's overloaded == also rejects a destroyed (fake-null) cached asset, so both
            // misses and stale entries fall through to a fresh resolve.
            if (m_MaterialCache.TryGetValue(relativePath, out var cached) && cached != null)
                return cached;

            var material = ResolveAsset<Material>(relativePath);
            if (material != null)
                m_MaterialCache[relativePath] = material;
            else
                WarnOnce($"Could not load Material 'Examples/{relativePath}'.");
            return material;
        }

        // Resolve a FilterFunctionDefinition (the authored content of a backdrop-filter example)
        // from a path relative to the Examples root. Mirrors LoadMaterial.
        FilterFunctionDefinition LoadFilter(string relativePath)
        {
            if (m_FilterCache.TryGetValue(relativePath, out var cached) && cached != null)
                return cached;

            var definition = ResolveAsset<FilterFunctionDefinition>(relativePath);
            if (definition != null)
                m_FilterCache[relativePath] = definition;
            else
                WarnOnce($"Could not load FilterFunctionDefinition 'Examples/{relativePath}'.");
            return definition;
        }

        // Resolve a still image (the content shown behind a backdrop-filter swatch) from a path
        // relative to the Examples root. Mirrors LoadMaterial.
        Texture2D LoadTexture(string relativePath)
        {
            if (string.IsNullOrEmpty(relativePath))
                return null;
            if (m_TextureCache.TryGetValue(relativePath, out var cached) && cached != null)
                return cached;

            var texture = ResolveAsset<Texture2D>(relativePath);
            if (texture != null)
                m_TextureCache[relativePath] = texture;
            else
                WarnOnce($"Could not load Texture 'Examples/{relativePath}'.");
            return texture;
        }

        // Misses are not cached (so an asset that appears after a late sample import is picked
        // up by the next rebuild); this keeps the log from repeating the same warning each time.
        void WarnOnce(string message)
        {
            if (m_WarnedOnce.Add(message))
                Debug.LogWarning($"[UITKShaderSampleScene] {message}");
        }

        // Editor: AssetDatabase lookup + record the reference for player builds.
        // Player: look the path up in the serialized registry the editor recorded.
        T ResolveAsset<T>(string relativePath) where T : Object
        {
#if UNITY_EDITOR
            var asset = AssetDatabase.LoadAssetAtPath<T>($"{examplesRoot}/{relativePath}");
            if (asset != null)
                RecordSerializedAsset(relativePath, asset);
            return asset;
#else
            for (int i = 0; i < m_SerializedAssets.Count; i++)
            {
                if (m_SerializedAssets[i].path == relativePath)
                    return m_SerializedAssets[i].asset as T;
            }
            return null;
#endif
        }

#if UNITY_EDITOR
        // The Examples folder of THIS imported sample, derived lazily from the controller
        // script's own asset path (…/<Sample>/Scripts/Runtime/UITKShaderSampleScene.cs →
        // …/<Sample>/Examples). The import folder under Assets/Samples embeds the package
        // version, which bumps with every release — deriving the root keeps asset resolution
        // working across version bumps (and sample folder renames) with no hard-coded path.
        string m_ExamplesRoot;

        string examplesRoot
        {
            get
            {
                if (string.IsNullOrEmpty(m_ExamplesRoot))
                {
                    var scriptPath = AssetDatabase.GetAssetPath(MonoScript.FromMonoBehaviour(this));
                    var sampleRoot = System.IO.Path.GetDirectoryName(System.IO.Path.GetDirectoryName(System.IO.Path.GetDirectoryName(scriptPath)));
                    m_ExamplesRoot = $"{sampleRoot}/Examples".Replace('\\', '/');
                }
                return m_ExamplesRoot;
            }
        }

        // Resolve every asset the sample uses so the serialized registry is complete even when
        // the scene is saved before the panel ever builds (fresh checkout, batch build). The
        // catalogue is the single source of truth for the paths; the pages load the same ones.
        void OnValidate()
        {
            if (Application.isPlaying)
                return;

            LoadMaterial(k_PlaceholderMaterialPath);
            foreach (var entry in CreateCatalogue())
            {
                if (TryGetBackdropFilter(entry.name, out var backdropFilter))
                {
                    LoadFilter(backdropFilter.definitionPath);
                    LoadTexture(backdropFilter.backgroundPath);
                }
                else
                {
                    LoadMaterial(GridMaterialPath(entry));
                }
            }
        }

        // Keep the serialized registry in sync with what the editor resolves. Dirties the
        // component (edit mode only) so saving the scene persists the references.
        void RecordSerializedAsset(string relativePath, Object asset)
        {
            for (int i = 0; i < m_SerializedAssets.Count; i++)
            {
                if (m_SerializedAssets[i].path != relativePath)
                    continue;
                if (m_SerializedAssets[i].asset == asset)
                    return;
                m_SerializedAssets[i] = new SerializedAsset { path = relativePath, asset = asset };
                if (!Application.isPlaying)
                    EditorUtility.SetDirty(this);
                return;
            }

            m_SerializedAssets.Add(new SerializedAsset { path = relativePath, asset = asset });
            if (!Application.isPlaying)
                EditorUtility.SetDirty(this);
        }
#endif

        // Conventional asset path of a grid entry's OWN material, relative to the Examples root.
        // Entries with an empty category live directly under Examples/; everyone else is nested
        // under Examples/<Category>/.
        static string GridMaterialPath(in GridEntry e) =>
            string.IsNullOrEmpty(e.category) ? $"{e.name}.mat" : $"{e.category}/{e.name}.mat";

        void BuildApp(VisualElement root)
        {
            // Soft full-panel backdrop (Warped Gradient) behind the whole app. Size only; it animates
            // internally. A smooth gradient (not a busy grid) so it recedes; a dark USS scrim
            // (.background-scrim) is layered on top so the foreground cards/shaders stay crisp.
            BindSizeOnly(root, "background", "Backgrounds/Warped Gradient.mat");

            BuildAllSamplesPage(root);
            BuildButtonsPage(root);
            BuildIndicatorsPage(root);
            BuildProgressPage(root);
            BuildBackgroundsPage(root);
            BuildInteractivePage(root);

            WireSidePanel(root);

            // Click-to-zoom lightbox, added last so it draws above the whole app. Clicking
            // anywhere on the overlay closes it; clearing it detaches the zoomed swatch, which
            // releases its material clone and stops its timers.
            m_ZoomOverlay = new VisualElement();
            m_ZoomOverlay.AddToClassList("zoom-overlay");
            // Focusable so it can take keyboard focus while open and close on Escape.
            m_ZoomOverlay.focusable = true;
            m_ZoomOverlay.RegisterCallback<ClickEvent>(_ => CloseZoom());
            m_ZoomOverlay.RegisterCallback<KeyDownEvent>(evt =>
            {
                if (evt.keyCode == KeyCode.Escape)
                {
                    CloseZoom();
                    evt.StopPropagation();
                }
            });
            root.Add(m_ZoomOverlay);
        }

        void CloseZoom()
        {
            m_ZoomOverlay.style.display = DisplayStyle.None;
            m_ZoomOverlay.Clear();
        }

        // ---- Side panel: radio-selected tabs that swap the visible page -------------------------

        void WireSidePanel(VisualElement root)
        {
            m_Tabs.Clear();

            AddTab(root, "tab-all", "tab-all-face", "page-all");
            AddTab(root, "tab-buttons", "tab-buttons-face", "page-buttons");
            AddTab(root, "tab-indicators", "tab-indicators-face", "page-indicators");
            AddTab(root, "tab-progress", "tab-progress-face", "page-progress");
            AddTab(root, "tab-backgrounds", "tab-backgrounds-face", "page-backgrounds");
            AddTab(root, "tab-interactive", "tab-interactive-face", "page-interactive");

            // Select the default tab (and hide every other page).
            SelectTab(k_DefaultTabName);
        }

        // Register one side-panel tab: render the Tab Button shader on its face, drive the shader's
        // on/off + interaction state, and make a click select the tab (radio behavior).
        void AddTab(VisualElement root, string toggleName, string faceName, string pageName)
        {
            var toggle = root.Q<Toggle>(toggleName);
            var face = root.Q<VisualElement>(faceName);
            var page = root.Q<VisualElement>(pageName);
            if (toggle == null || page == null)
                return;

            m_Tabs.Add(new TabEntry { toggle = toggle, page = page, name = toggleName });

            var material = LoadMaterial("Tab Button.mat");
            if (face != null && material != null)
            {
                // Keep the element size property in sync (no-op for shaders that auto-read size).
                face.AddManipulator(new ElementSizeBinding(material));

                // Interaction: map the tab's hover/press to the UGUI-style _State enum on the face.
                void WriteState(InteractiveElementStateBinding.State state) =>
                    UIShaderBinding.SetFloat(face, material, "_State", (float)state);

                WriteState(InteractiveElementStateBinding.State.Normal);
                toggle.RegisterCallback<PointerEnterEvent>(_ => WriteState(InteractiveElementStateBinding.State.Hover));
                toggle.RegisterCallback<PointerLeaveEvent>(_ => WriteState(InteractiveElementStateBinding.State.Normal));
                toggle.RegisterCallback<PointerDownEvent>(_ => WriteState(InteractiveElementStateBinding.State.Pressed));
                toggle.RegisterCallback<PointerUpEvent>(_ => WriteState(InteractiveElementStateBinding.State.Hover));
            }

            // Clicking the tab selects it. A click flips the Toggle's value and fires this
            // callback for either direction; SelectTab then forces the radio state (re-asserting
            // the active tab as on if the user clicked the already-selected tab).
            toggle.RegisterValueChangedCallback(_ => SelectTab(toggleName));
        }

        // Radio selection: show the chosen tab's page, hide the rest, and reflect the active state
        // both in USS (the --active class, the source of truth for readability) and in the
        // Tab Button shader's on/off property on each face.
        void SelectTab(string toggleName)
        {
            foreach (var tab in m_Tabs)
            {
                bool active = tab.name == toggleName;

                // Page visibility.
                tab.page.style.display = active ? DisplayStyle.Flex : DisplayStyle.None;

                // USS active styling (accent bar + brighter/bigger label). This guarantees the
                // selected tab reads clearly regardless of how the Tab Button shader renders.
                tab.toggle.EnableInClassList("tab--active", active);

                // Keep the Toggle value consistent with the radio state without re-entrancy:
                // SetValueWithoutNotify avoids firing the change callback we registered.
                tab.toggle.SetValueWithoutNotify(active);

                // Shader on/off: the Tab Button graph promotes a hidden boolean named "isOn".
                // Write both the bare and underscore-prefixed names so the value lands whichever
                // one the graph compiled (a SetFloat on an absent property is a harmless no-op).
                var face = tab.toggle.Q<VisualElement>(className: "tab-face");
                if (face != null)
                {
                    var material = LoadMaterial("Tab Button.mat");
                    if (material != null)
                    {
                        UIShaderBinding.SetFloat(face, material, "isOn", active ? 1f : 0f);
                        UIShaderBinding.SetFloat(face, material, "_IsOn", active ? 1f : 0f);
                    }
                }
            }
        }

        // ---- All Samples page: the responsive grid of every example ----------------------------

        void BuildAllSamplesPage(VisualElement root)
        {
            var grid = root.Q<VisualElement>("grid");
            if (grid == null)
            {
                Debug.LogWarning("[UITKShaderSampleScene] No element named 'grid' in the UXML.");
                return;
            }

            var catalogue = CreateCatalogue();
            var placeholder = LoadMaterial(k_PlaceholderMaterialPath);

            int total = catalogue.Length;
            int done = 0;
            foreach (var e in catalogue)
            {
                bool authored = TryGetBackdropFilter(e.name, out var backdropFilter)
                    ? LoadFilter(backdropFilter.definitionPath) != null
                    : LoadMaterial(GridMaterialPath(e)) != null;
                if (authored)
                    done++;
            }
            int pct = total > 0 ? Mathf.RoundToInt(done / (float)total * 100f) : 0;

            var header = root.Q<Label>("header-label");
            if (header != null)
                header.text = $"UITK Shader Samples — {done}/{total} ({pct}%)";
            var progressFill = root.Q<VisualElement>("progress-fill");
            if (progressFill != null)
                progressFill.style.width = Length.Percent(pct);

            // Rebuild the grid as a column of explicit row containers; cells are distributed
            // row-major, k_GridColumns per row, giving a fixed 6-wide board with no flex-wrap math.
            grid.Clear();
            VisualElement currentRow = null;
            for (int i = 0; i < catalogue.Length; i++)
            {
                if (i % k_GridColumns == 0)
                {
                    currentRow = new VisualElement();
                    currentRow.AddToClassList("grid-row");
                    grid.Add(currentRow);
                }
                var entry = catalogue[i];
                var phase = GridPhase(i);
                var cell = MakeGridCell(entry, placeholder, phase);
                cell.RegisterCallback<ClickEvent>(_ => ShowZoom(entry, phase));
                currentRow.Add(cell);
            }

            // Pad a partial last row with invisible spacers: cells use flex-grow, so without
            // them the remainder cells would stretch wider than every other row's.
            for (int i = catalogue.Length; i % k_GridColumns != 0; i++)
            {
                var spacer = new VisualElement();
                spacer.AddToClassList("cell");
                spacer.style.visibility = Visibility.Hidden;
                currentRow.Add(spacer);
            }
        }

        // Deterministic stagger for the grid's meter tiles: a golden-ratio sequence spreads the
        // phases pleasingly (like the old Random.value) while keeping every capture of the
        // board a pure function of game time.
        static float GridPhase(int index) => index * 0.618034f % 1f;

        // One grid cell: a button-shaped swatch rendering the example's own material (or the
        // Simple Button placeholder if it is not authored yet) plus a status dot + name footer.
        VisualElement MakeGridCell(GridEntry entry, Material placeholder, float meterPhase)
        {
            // Backdrop-filter examples carry a custom filter on the swatch instead of a material.
            if (TryGetBackdropFilter(entry.name, out var backdropFilter))
                return MakeBackdropFilterCell(entry, backdropFilter);

            var ownMaterial = LoadMaterial(GridMaterialPath(entry));
            bool done = ownMaterial != null;
            Material material = done ? ownMaterial : placeholder;

            var cell = new VisualElement();
            cell.AddToClassList("cell");

            var swatch = new VisualElement();
            swatch.AddToClassList("swatch");
            swatch.AddToClassList(done ? "swatch--live" : "swatch--todo");
            if (material != null)
            {
                swatch.AddManipulator(new ElementSizeBinding(material));
                swatch.AddManipulator(new InteractiveElementStateBinding(material));

                // Value-driven examples animate their fill via the same meter+timer driver the
                // dedicated pages use, so the live board reads identically.
                if (done && IsMeterDriven(entry.name))
                {
                    var meter = new MeterBinding(material);
                    swatch.AddManipulator(meter);
                    DriveMeter(swatch, meter, meterPhase);
                }
            }

            // The shader name is NOT drawn over the swatch (white-on-shader is unreadable on
            // light/animated fills, and it duplicates the footer). The footer caption below sits
            // on the dark page background and stays legible against any swatch.
            cell.Add(swatch);

            // Status dot + name row beneath the button.
            var footer = new VisualElement();
            footer.AddToClassList("cell-footer");

            var dot = new VisualElement();
            dot.AddToClassList("dot");
            dot.AddToClassList(done ? "dot--done" : "dot--todo");
            footer.Add(dot);

            var name = new Label(entry.name);
            name.AddToClassList("cell-name");
            footer.Add(name);

            cell.Add(footer);
            return cell;
        }

        // One grid cell whose swatch carries a custom backdrop-filter: it samples and processes the
        // content rendered behind it (the animated panel backdrop) rather than rendering a material
        // of its own. The FilterFunctionDefinition and its post-process shader are the example's
        // authored content.
        VisualElement MakeBackdropFilterCell(GridEntry entry, BackdropFilterExample example)
        {
            var definition = LoadFilter(example.definitionPath);
            bool done = definition != null;

            var cell = new VisualElement();
            cell.AddToClassList("cell");

            var swatch = new VisualElement();
            swatch.AddToClassList("swatch");
            swatch.AddToClassList(done ? "swatch--live" : "swatch--todo");
            swatch.style.overflow = Overflow.Hidden;
            if (done)
            {
                // The content the filter processes: a still of the sample scene (Kirby Cove sky +
                // grass), drawn behind the filtered overlay so the backdrop-filter has real content
                // to sample. A backdrop-filter only sees what is rendered *behind* its element.
                var backdrop = LoadTexture(example.backgroundPath);
                if (backdrop != null)
                {
                    var scene = new VisualElement();
                    scene.style.position = Position.Absolute;
                    scene.style.left = 0; scene.style.right = 0; scene.style.top = 0; scene.style.bottom = 0;
                    scene.style.backgroundImage = new StyleBackground(backdrop);
                    scene.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Cover);
                    swatch.Add(scene);
                }

                var filtered = new VisualElement();
                filtered.style.position = Position.Absolute;
                filtered.style.left = 0; filtered.style.right = 0; filtered.style.top = 0; filtered.style.bottom = 0;
                ApplyBackdropFilter(filtered, definition, example.parameters);
                swatch.Add(filtered);

                // While playing, animate the filter (e.g. pulse the pixelation block size).
                if (example.animatePeriodSeconds > 0f)
                    DriveBackdropFilter(filtered, definition, example);
            }
            cell.Add(swatch);

            var footer = new VisualElement();
            footer.AddToClassList("cell-footer");

            var dot = new VisualElement();
            dot.AddToClassList("dot");
            dot.AddToClassList(done ? "dot--done" : "dot--todo");
            footer.Add(dot);

            var name = new Label(entry.name);
            name.AddToClassList("cell-name");
            footer.Add(name);

            cell.Add(footer);
            return cell;
        }

        // Apply a custom backdrop-filter (a FilterFunctionDefinition + its parameter values) to an
        // element. FilterFunction is a value type with inline parameter storage, so rebuilding it
        // per frame for animation allocates nothing native (unlike a per-frame MaterialDefinition).
        static void ApplyBackdropFilter(VisualElement element, FilterFunctionDefinition definition, IReadOnlyList<float> parameters)
        {
            var function = new FilterFunction(definition);
            for (int i = 0; i < parameters.Count; i++)
                function.AddParameter(new FilterParameter(parameters[i]));
            element.style.backdropFilter = new List<FilterFunction> { function };
        }

        // Pulse one filter parameter between min and max over time (a smooth fine<->coarse cycle for
        // pixelation), driven off game time so it animates in play and reproduces in deterministic
        // captures, exactly like the meter driver.
        void DriveBackdropFilter(VisualElement anchor, FilterFunctionDefinition definition, BackdropFilterExample example)
        {
            var values = (float[])example.parameters.Clone();
            float lastApplied = float.NaN;
            UITimer.Start(anchor, example.animatePeriodSeconds, t =>
            {
                float pulse = 0.5f - 0.5f * Mathf.Cos(t * 2f * Mathf.PI);
                // Snap to whole pixels and only re-apply when the value actually changes: the filter
                // re-assigns a handful of times per cycle instead of every frame, so the animation
                // reads as a clean stepped zoom with no per-frame repaint flicker.
                float snapped = Mathf.Round(Mathf.Lerp(example.animatedMin, example.animatedMax, pulse));
                if (snapped == lastApplied)
                    return;
                lastApplied = snapped;
                values[example.animatedParameter] = snapped;
                ApplyBackdropFilter(anchor, definition, values);
            });
        }

        // ---- Category pages ---------------------------------------------------------------------

        void BuildButtonsPage(VisualElement root)
        {
            BindButton(root, "button-simple", "Buttons/Simple Button.mat");
            BindButton(root, "button-aqua", "Buttons/Aqua Button.mat");
            BindButton(root, "button-scifi", "Buttons/SciFi Button.mat");
            BindButton(root, "button-scifi2", "Buttons/SciFi2 Button.mat");
        }

        void BuildIndicatorsPage(VisualElement root)
        {
            // Stagger the initial phase so the meters do not fill in lockstep.
            BindAnimatedMeter(root, "meter-aqua", "Indicators/Aqua Meter.mat", 0.00f);
            BindAnimatedMeter(root, "meter-dial", "Indicators/Dial Meter.mat", 0.25f);
            BindAnimatedMeter(root, "meter-fantasy", "Indicators/Fantasy Meter.mat", 0.50f);
            BindAnimatedMeter(root, "meter-scifi", "Indicators/SciFi Meter.mat", 0.75f);
        }

        void BuildProgressPage(VisualElement root)
        {
            // Gradient Bar + Progress Circle read _MeterValue, so they get the meter+timer driver.
            BindAnimatedMeter(root, "progress-gradient", "ProgressBars/Gradient Bar.mat", 0.10f);
            BindAnimatedMeter(root, "progress-circle", "ProgressBars/Progress Circle.mat", 0.60f);
            // Fancy Loading + Simple Loading animate via _Time internally; only size is needed.
            BindSizeOnly(root, "progress-fancy", "ProgressBars/Fancy Loading.mat");
            BindSizeOnly(root, "progress-simple", "ProgressBars/Simple Loading.mat");
        }

        // Backgrounds page: the category's examples at feature size, three per row. Reuses the
        // grid cell builder, so material swatches and the two backdrop-filter examples render
        // exactly like the All Samples board — just larger.
        void BuildBackgroundsPage(VisualElement root)
        {
            var grid = root.Q<VisualElement>("backgrounds-grid");
            if (grid == null)
                return;

            var placeholder = LoadMaterial(k_PlaceholderMaterialPath);
            var entries = new List<GridEntry>();
            foreach (var e in CreateCatalogue())
            {
                if (e.category == "Backgrounds")
                    entries.Add(e);
            }

            const int columns = 3;
            grid.Clear();
            VisualElement currentRow = null;
            for (int i = 0; i < entries.Count; i++)
            {
                if (i % columns == 0)
                {
                    currentRow = new VisualElement();
                    currentRow.AddToClassList("grid-row");
                    grid.Add(currentRow);
                }

                var entry = entries[i];
                var phase = GridPhase(i);
                var cell = MakeGridCell(entry, placeholder, phase);
                cell.RegisterCallback<ClickEvent>(_ => ShowZoom(entry, phase));
                currentRow.Add(cell);
            }

            // Pad a partial last row with invisible spacers (see BuildAllSamplesPage).
            for (int i = entries.Count; i % columns != 0; i++)
            {
                var spacer = new VisualElement();
                spacer.AddToClassList("cell");
                spacer.style.visibility = Visibility.Hidden;
                currentRow.Add(spacer);
            }
        }

        // Open the lightbox with one example at near-full size. The zoomed swatch is a fresh
        // element with its own bindings (and, for backdrop-filter examples, its own backdrop
        // image), so it animates and reacts exactly like the small cell.
        void ShowZoom(GridEntry entry, float meterPhase)
        {
            if (m_ZoomOverlay == null)
                return;

            m_ZoomOverlay.Clear();

            var frame = new VisualElement();
            frame.AddToClassList("zoom-frame");
            frame.pickingMode = PickingMode.Ignore;

            var swatch = new VisualElement();
            swatch.AddToClassList("swatch");
            swatch.AddToClassList("swatch--live");
            swatch.AddToClassList("zoom-swatch");
            swatch.pickingMode = PickingMode.Ignore;
            swatch.style.overflow = Overflow.Hidden;

            if (TryGetBackdropFilter(entry.name, out var example))
            {
                var definition = LoadFilter(example.definitionPath);
                if (definition != null)
                {
                    var backdrop = LoadTexture(example.backgroundPath);
                    if (backdrop != null)
                    {
                        var scene = new VisualElement();
                        scene.pickingMode = PickingMode.Ignore;
                        scene.style.position = Position.Absolute;
                        scene.style.left = 0; scene.style.right = 0; scene.style.top = 0; scene.style.bottom = 0;
                        scene.style.backgroundImage = new StyleBackground(backdrop);
                        scene.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Cover);
                        swatch.Add(scene);
                    }

                    var filtered = new VisualElement();
                    filtered.pickingMode = PickingMode.Ignore;
                    filtered.style.position = Position.Absolute;
                    filtered.style.left = 0; filtered.style.right = 0; filtered.style.top = 0; filtered.style.bottom = 0;
                    ApplyBackdropFilter(filtered, definition, example.parameters);
                    swatch.Add(filtered);

                    if (example.animatePeriodSeconds > 0f)
                        DriveBackdropFilter(filtered, definition, example);
                }
            }
            else
            {
                var material = LoadMaterial(GridMaterialPath(entry));
                if (material != null)
                {
                    swatch.AddManipulator(new ElementSizeBinding(material));
                    swatch.AddManipulator(new InteractiveElementStateBinding(material));

                    if (IsMeterDriven(entry.name))
                    {
                        var meter = new MeterBinding(material);
                        swatch.AddManipulator(meter);
                        DriveMeter(swatch, meter, meterPhase);
                    }
                }
            }

            frame.Add(swatch);

            var caption = new Label($"{entry.name} — click to close");
            caption.AddToClassList("zoom-caption");
            caption.pickingMode = PickingMode.Ignore;
            frame.Add(caption);

            m_ZoomOverlay.Add(frame);
            m_ZoomOverlay.style.display = DisplayStyle.Flex;
            // Take focus so Escape closes the lightbox for keyboard users.
            m_ZoomOverlay.schedule.Execute(m_ZoomOverlay.Focus);
        }

        // Interactive page: a row of Tab Button toggles + a Slider that drives the Gradient Bar.
        void BuildInteractivePage(VisualElement root)
        {
            // Demo Tab Button toggles (independent of the side-panel tabs): each renders the
            // Tab Button shader on its face and flips isOn/_State on click, so the page shows the
            // shader's on/off + hover/press response directly.
            BindDemoTab(root, "itab-1", "itab-1-face");
            BindDemoTab(root, "itab-2", "itab-2-face");
            BindDemoTab(root, "itab-3", "itab-3-face");

            // The Slider drives the fill of a Gradient Bar swatch, demonstrating interactive
            // control of a shader value. NOTE: this is the only place a Slider drives a meter, and
            // it intentionally drives the Gradient Bar (not an Aqua Meter) so the demo has exactly
            // one Aqua Meter (on the Indicators page). The slider itself carries no material:
            // unity-material is an inherited style, so assigning one to the Slider root would
            // repaint its tracker/dragger children through that shader.
            var slider = root.Q<Slider>("interactive-slider");
            var bar = root.Q<VisualElement>("interactive-bar");
            var barMaterial = LoadMaterial("ProgressBars/Gradient Bar.mat");

            if (bar != null && barMaterial != null)
            {
                bar.AddManipulator(new ElementSizeBinding(barMaterial));
                var drivenMeter = new MeterBinding(barMaterial);
                bar.AddManipulator(drivenMeter);

                if (slider != null)
                {
                    drivenMeter.Value = slider.value;
                    slider.RegisterValueChangedCallback(evt => drivenMeter.Value = evt.newValue);
                }
            }
        }

        // ---- Binding helpers --------------------------------------------------------------------

        // True while the forced-fill validation hook is active (see k_ForcedMeterValueKey).
        static bool TryGetForcedMeterValue(out float value)
        {
            value = -1f;
#if UNITY_EDITOR
            value = SessionState.GetFloat(k_ForcedMeterValueKey, -1f);
#endif
            if (value < 0f)
                return false;
            value = Mathf.Clamp01(value);
            return true;
        }

        // Drive a MeterBinding: the sawtooth ramp normally, or the forced validation value
        // when the SessionState key is set. The forced check lives inside the tick so a key
        // written mid-session (the UI tree survives play transitions when Enter Play Mode
        // Options skip the scene reload) takes effect immediately instead of one rebuild late.
        void DriveMeter(VisualElement anchor, MeterBinding meter, float initialPhase)
        {
            if (TryGetForcedMeterValue(out var forced))
                meter.Value = forced;
            UITimer.Start(anchor, k_FillPeriodSeconds,
                t => meter.Value = TryGetForcedMeterValue(out var f) ? f : t, initialPhase);
        }

        // A button swatch: size + hover/press state.
        void BindButton(VisualElement root, string elementName, string materialPath)
        {
            var element = root.Q<VisualElement>(elementName);
            var material = LoadMaterial(materialPath);
            if (element == null || material == null)
                return;

            element.AddManipulator(new ElementSizeBinding(material));
            element.AddManipulator(new InteractiveElementStateBinding(material));
        }

        // A swatch whose only requirement is a correct _RectSize (the shader animates itself).
        void BindSizeOnly(VisualElement root, string elementName, string materialPath)
        {
            var element = root.Q<VisualElement>(elementName);
            var material = LoadMaterial(materialPath);
            if (element == null || material == null)
                return;

            element.AddManipulator(new ElementSizeBinding(material));
        }

        // A meter/progress swatch driven by an external sawtooth ramp into _MeterValue.
        void BindAnimatedMeter(VisualElement root, string elementName, string materialPath, float initialPhase)
        {
            var element = root.Q<VisualElement>(elementName);
            var material = LoadMaterial(materialPath);
            if (element == null || material == null)
                return;

            element.AddManipulator(new ElementSizeBinding(material));
            var meter = new MeterBinding(material);
            element.AddManipulator(meter);

            // A recurring 0->1 ramp on the panel scheduler; the binding clamps + writes _MeterValue.
            DriveMeter(element, meter, initialPhase);
        }

        // A demo Tab Button (Interactive page) rendered on a Toggle. The shader renders onto a child
        // "face" element (a Toggle's own box does not host a custom-material draw). The face carries
        // an ElementSizeBinding; the toggle's value + pointer state are written onto that same face
        // so all properties share one material clone. Clicking flips the shader's on/off property.
        void BindDemoTab(VisualElement root, string toggleName, string faceName)
        {
            var toggle = root.Q<Toggle>(toggleName);
            var face = root.Q<VisualElement>(faceName);
            var material = LoadMaterial("Tab Button.mat");
            if (toggle == null || face == null || material == null)
                return;

            face.AddManipulator(new ElementSizeBinding(material));

            void WriteIsOn(bool on)
            {
                // The Tab Button shader exposes no settable on/off property in UITK, so the visible
                // selection cue lives in USS: toggle the --on class on the Toggle and let the
                // stylesheet drive the cyan border + accent bar + opacity (see .demo-tab--on).
                toggle.EnableInClassList("demo-tab--on", on);

                // Still write the shader's on/off names so the material stays correctly bound and
                // future shader revisions that DO expose the property light up automatically (a
                // SetFloat on an absent property is a harmless no-op).
                UIShaderBinding.SetFloat(face, material, "isOn", on ? 1f : 0f);
                UIShaderBinding.SetFloat(face, material, "_IsOn", on ? 1f : 0f);
            }

            WriteIsOn(toggle.value);
            toggle.RegisterValueChangedCallback(evt => WriteIsOn(evt.newValue));

            void WriteState(InteractiveElementStateBinding.State state) =>
                UIShaderBinding.SetFloat(face, material, "_State", (float)state);

            WriteState(InteractiveElementStateBinding.State.Normal);
            toggle.RegisterCallback<PointerEnterEvent>(_ => WriteState(InteractiveElementStateBinding.State.Hover));
            toggle.RegisterCallback<PointerLeaveEvent>(_ => WriteState(InteractiveElementStateBinding.State.Normal));
            toggle.RegisterCallback<PointerDownEvent>(_ => WriteState(InteractiveElementStateBinding.State.Pressed));
            toggle.RegisterCallback<PointerUpEvent>(_ => WriteState(InteractiveElementStateBinding.State.Hover));
        }
    }
}
