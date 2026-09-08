using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal
{
    internal static class Light2DEditorUtility
    {
        private static class Styles
        {
            public static GUIContent lightTypeFreeform = new GUIContent("Freeform", Resources.Load("InspectorIcons/FreeformLight") as Texture);
            public static GUIContent lightTypeSprite = new GUIContent("Sprite", Resources.Load("InspectorIcons/SpriteLight") as Texture);
            public static GUIContent lightTypePoint = new GUIContent("Spot", Resources.Load("InspectorIcons/PointLight") as Texture);
            public static GUIContent lightTypeGlobal = new GUIContent("Global", Resources.Load("InspectorIcons/GlobalLight") as Texture);

            public static GUIContent lightTypeMissing = new GUIContent("Missing", "The Light Type this light is set to is not available on this GameObject.");
            public static GUIContent lightTypeParametricDeprecated = new GUIContent("Parametric (Deprecated)", "Parametric lights are deprecated. Upgrade this light to a Freeform light.");

            // Format argument: {0} the name of the provider type that is no longer in the project.
            public static readonly GUIContent lightTypeMissingProvider = new GUIContent("Missing ({0})", "The script that defined this light's provider has been removed from the project.");
        }

        static Material s_TexCapMaterial = CoreUtils.CreateEngineMaterial(Shader.Find("Hidden/Internal-GUITexture"));
        static Mesh k_MeshQuad_Cache;
        static Mesh k_MeshQuad => k_MeshQuad_Cache == null || k_MeshQuad_Cache.Equals(null) ? (k_MeshQuad_Cache = Resources.GetBuiltinResource<Mesh>("Quad.fbx")) : k_MeshQuad_Cache;

        /// <summary>
        /// Draws the Light Type dropdown for a Light2D component using the provider system.
        /// </summary>
        /// <remarks>
        /// This method provides a unified way to render the Light Type dropdown in both the Inspector and Light Explorer windows.
        /// It automatically scans for custom Light2DProvider components on the GameObject and includes them in the dropdown
        /// alongside built-in light types (Spot, Freeform, Sprite, Global).
        ///
        /// The method handles:
        /// - Scanning for custom providers on the GameObject
        /// - Synchronizing the dropdown selection with the actual light type value
        /// - Detecting and applying user changes only when necessary
        /// - Supporting both layout-based (Inspector) and rect-based (Light Explorer) rendering
        /// </remarks>
        /// <param name="position">The rect to draw the control in. This parameter is ignored when <paramref name="layoutMode"/> is true.</param>
        /// <param name="label">The label to display next to the dropdown. Pass <see cref="GUIContent.none"/> for no label.</param>
        /// <param name="serializedObject">The SerializedObject containing the Light2D component. Must not be null.</param>
        /// <param name="layoutMode">If true, uses EditorGUILayout for automatic layout. If false, uses EditorGUI with the provided <paramref name="position"/> rect.</param>
        public static void DrawLightTypePopup(Rect position, GUIContent label, SerializedObject serializedObject, bool layoutMode)
        {
            serializedObject.Update();

            SerializedProperty selectionSources = serializedObject.FindProperty("m_SelectionSources");
            SerializedProperty lightType = serializedObject.FindProperty("m_LightType");

            if (selectionSources == null || lightType == null)
                return;

            // Get the Light2D component and its GameObject
            Light2D light2D = serializedObject.targetObject as Light2D;
            if (light2D == null)
                return;

            Light2DProviderSources sources = selectionSources.boxedValue as Light2DProviderSources;
            if (sources == null)
                return;

            int selectedIndex = RefreshLightSources(light2D, sources);

            // Sync m_SelectionSources with the actual m_LightType value
            // This is needed because Light Explorer may have changed m_LightType directly
            if (lightType.intValue != (int)Light2D.LightType.Provider && sources.selectedHashCode != lightType.intValue)
            {
                sources.selectedHashCode = lightType.intValue;
                selectedIndex = Provider2DSources<Light2DProvider, Light2DProviderSource>.RefreshSources(sources, light2D.gameObject, 0);
            }

            // Get all source names for the dropdown, narrowed to what every selected light can offer.
            // Offering a source only some of them have would apply the pick to those and silently skip
            // the rest, since ApplyLightTypeSelection matches each light against its own list.
            GUIContent[] sourceNames = CollectCommonSourceNames(serializedObject.targetObjects, sources.GetSourceNames());
            if (selectedIndex >= 0)
                selectedIndex = IndexOfSourceName(sourceNames, sources.GetSourceNames()[selectedIndex].text);

            // The stored selection may name nothing that is listed: the light is still on the
            // deprecated Parametric type, or the script behind its provider is gone. Popup() draws a
            // blank row for index -1, which leaves the user with no idea what the light is set to, so
            // give that state an entry of its own.
            int firstSourceOption;
            sourceNames = BuildLightTypeOptions(light2D, lightType.intValue, sourceNames, ref selectedIndex, out firstSourceOption);

            // Track change
            EditorGUI.BeginChangeCheck();

            int newSelectedIndex;
            // Draw the dropdown using the appropriate mode
            if (layoutMode)
            {
                newSelectedIndex = EditorGUILayout.Popup(label, selectedIndex, sourceNames);
            }
            else
            {
                newSelectedIndex = EditorGUI.Popup(position, selectedIndex, sourceNames);
            }

            // Only apply changes if the user actually changed the value. Re-picking the entry that
            // stands for the unresolved selection changes nothing, so it is not an edit.
            if (EditorGUI.EndChangeCheck() && newSelectedIndex >= firstSourceOption)
            {
                ApplyLightTypeSelection(serializedObject, sourceNames[newSelectedIndex]);
            }
        }

        /// <summary>
        /// Scans <paramref name="light2D"/> for custom providers, adds the built-in light types, and returns
        /// the index of the currently selected source, or -1 when the stored selection matches none of them.
        /// </summary>
        static int RefreshLightSources(Light2D light2D, Light2DProviderSources sources)
        {
            // Refresh sources to scan for custom providers on the GameObject
            Provider2DSources<Light2DProvider, Light2DProviderSource>.RefreshSources(sources, light2D.gameObject, 0);

            // Add built-in types
            var additionalSourcesList = sources.GetAdditionalSources();
            if (additionalSourcesList != null)
            {
                additionalSourcesList.Clear();
                additionalSourcesList.Add(new Light2DSource_BuiltIn(Styles.lightTypePoint, Light2D.LightType.Point, 0));
                additionalSourcesList.Add(new Light2DSource_BuiltIn(Styles.lightTypeFreeform, Light2D.LightType.Freeform, 0));
                additionalSourcesList.Add(new Light2DSource_BuiltIn(Styles.lightTypeSprite, Light2D.LightType.Sprite, 0));
                additionalSourcesList.Add(new Light2DSource_BuiltIn(Styles.lightTypeGlobal, Light2D.LightType.Global, 0));
            }

            // Refresh again to include the built-in types
            return Provider2DSources<Light2DProvider, Light2DProviderSource>.RefreshSources(sources, light2D.gameObject, 0);
        }

        /// <summary>
        /// Returns the entries of <paramref name="sourceNames"/>, built from the first selected light, that
        /// every other selected light can offer too. With one light selected the list is returned unchanged.
        /// </summary>
        static internal GUIContent[] CollectCommonSourceNames(UnityEngine.Object[] targets, GUIContent[] sourceNames)
        {
            if (targets.Length < 2)
                return sourceNames;

            List<GUIContent> common = new List<GUIContent>(sourceNames);
            for (int i = 1; i < targets.Length && common.Count > 0; i++)
            {
                Light2D light = targets[i] as Light2D;
                if (light == null)
                    continue;

                Light2DProviderSources targetSources =
                    new SerializedObject(light).FindProperty("m_SelectionSources")?.boxedValue as Light2DProviderSources;
                if (targetSources == null)
                {
                    common.Clear();
                    break;
                }

                RefreshLightSources(light, targetSources);
                GUIContent[] targetNames = targetSources.GetSourceNames();

                for (int j = common.Count - 1; j >= 0; j--)
                {
                    if (IndexOfSourceName(targetNames, common[j].text) < 0)
                        common.RemoveAt(j);
                }
            }

            return common.ToArray();
        }

        static int IndexOfSourceName(GUIContent[] names, string wanted)
        {
            for (int i = 0; i < names.Length; i++)
            {
                if (names[i].text == wanted)
                    return i;
            }

            return -1;
        }

        /// <summary>
        /// Applies the entry the user picked to every selected light.
        /// </summary>
        /// <remarks>
        /// The dropdown is built from the first selected light, but each light has its own sources -- its own
        /// components, and its own provider instances -- so each one is matched by name against its own list.
        /// Writing through the shared SerializedObject instead would only reach the first light for
        /// [SerializeReference] fields such as m_Light2DProvider, which leaves every other selected light on
        /// Light Type Provider with no provider at all.
        /// </remarks>
        static internal void ApplyLightTypeSelection(SerializedObject serializedObject, GUIContent chosen)
        {
            UnityEngine.Object[] targets = serializedObject.targetObjects;
            for (int i = 0; i < targets.Length; i++)
            {
                Light2D light = targets[i] as Light2D;
                if (light == null)
                    continue;

                SerializedObject targetObject = new SerializedObject(light);
                SerializedProperty targetSelectionSources = targetObject.FindProperty("m_SelectionSources");
                Light2DProviderSources targetSources = targetSelectionSources?.boxedValue as Light2DProviderSources;
                if (targetSources == null)
                    continue;

                RefreshLightSources(light, targetSources);

                GUIContent[] targetNames = targetSources.GetSourceNames();
                for (int j = 0; j < targetNames.Length; j++)
                {
                    if (targetNames[j].text != chosen.text)
                        continue;

                    Provider2DSources<Light2DProvider, Light2DProviderSource>.UpdateSelectionFromIndex(targetSources, j);
                    targetSelectionSources.boxedValue = targetSources;
                    Light2DProviderSources.SetSourceType(targetSelectionSources);
                    break;
                }
            }
        }

        /// <summary>
        /// Returns the entries to show in the Light Type dropdown. When <paramref name="selectedIndex"/> is
        /// negative -- the stored selection matches none of the sources on this GameObject -- an entry
        /// describing that state is prepended and selected, so the dropdown never renders blank.
        /// </summary>
        /// <param name="target">The Light2D the dropdown is being drawn for.</param>
        /// <param name="lightType">The light's current <see cref="Light2D.LightType"/> value.</param>
        /// <param name="sourceNames">The names of the sources available on this GameObject.</param>
        /// <param name="selectedIndex">The index into <paramref name="sourceNames"/> to preselect, updated to index into the returned array.</param>
        /// <param name="firstSourceOption">The index in the returned array at which <paramref name="sourceNames"/> starts.</param>
        static internal GUIContent[] BuildLightTypeOptions(UnityEngine.Object target, int lightType, GUIContent[] sourceNames, ref int selectedIndex, out int firstSourceOption)
        {
            if (selectedIndex >= 0)
            {
                firstSourceOption = 0;
                return sourceNames;
            }

            List<GUIContent> options = new List<GUIContent>(sourceNames.Length + 1);
            options.Add(DescribeUnresolvedLightType(target, lightType));
            options.AddRange(sourceNames);

            firstSourceOption = 1;
            selectedIndex = 0;
            return options.ToArray();
        }

        static GUIContent DescribeUnresolvedLightType(UnityEngine.Object target, int lightType)
        {
            if (lightType == (int)Light2D.DeprecatedLightType.Parametric)
                return Styles.lightTypeParametricDeprecated;

            string removedName = GetMissingProviderTypeName(target);
            if (removedName == null)
                return Styles.lightTypeMissing;

            return new GUIContent(string.Format(Styles.lightTypeMissingProvider.text, removedName), Styles.lightTypeMissingProvider.tooltip);
        }

        /// <summary>
        /// Returns the class name the light's provider field used to point at, or null when the editor can
        /// still resolve it. This is how a provider whose script was deleted can be named in the inspector:
        /// the reference itself deserializes to null, but the engine keeps a record of the type it held.
        /// </summary>
        /// <remarks>
        /// Taking the first unresolved reference is enough to name the provider. The field itself cannot
        /// be asked -- once its type fails to resolve, its managedReferenceId no longer matches the record
        /// -- and the only managed references a Light2D holds that a user can author are providers: the
        /// rest are URP's own types, which always resolve. References an older version of URP left in the
        /// asset are not a hazard either; once no live field points at one, it is not reported as missing.
        /// </remarks>
        static internal string GetMissingProviderTypeName(UnityEngine.Object target)
        {
            if (target == null)
                return null;

            var missingTypes = UnityEditor.SerializationUtility.GetManagedReferencesWithMissingTypes(target);
            for (int i = 0; i < missingTypes.Length; i++)
            {
                if (!string.IsNullOrEmpty(missingTypes[i].className))
                    return missingTypes[i].className;
            }

            return null;
        }

        /// <summary>
        /// Draws a Material field that refuses a dragged Material <paramref name="isCompatible"/> rejects,
        /// leaving whatever the field already holds in place.
        /// </summary>
        /// <remarks>
        /// The object picker is filtered by a Search provider, but dragging an asset onto the field goes
        /// nowhere near the picker, which is the hole this closes. The field is drawn into a rect we
        /// reserve ourselves so the drag can be judged before the object field ever sees the event.
        /// </remarks>
        /// <param name="property">The Material property to draw.</param>
        /// <param name="label">The label to draw beside it.</param>
        /// <param name="isCompatible">Whether a Material may be assigned to this field.</param>
        static internal void DrawFilteredMaterialField(SerializedProperty property, GUIContent label, Func<Material, bool> isCompatible)
        {
            Rect position = EditorGUILayout.GetControlRect(true, EditorGUI.GetPropertyHeight(property, label, true));
            RejectIncompatibleMaterialDrag(position, isCompatible);
            EditorGUI.PropertyField(position, property, label, true);
        }

        // Consumes a drag over `position` whose payload holds a Material this field cannot take.
        // ObjectField only assigns on the DragUpdated / DragPerform events, so using the event here is
        // what stops the assignment; the Rejected visual mode is what turns the drag cursor while the
        // pointer is over the field.
        static void RejectIncompatibleMaterialDrag(Rect position, Func<Material, bool> isCompatible)
        {
            Event evt = Event.current;
            if (evt == null || (evt.type != EventType.DragUpdated && evt.type != EventType.DragPerform))
                return;

            if (!GUI.enabled || !position.Contains(evt.mousePosition))
                return;

            if (IsMaterialDragAcceptable(DragAndDrop.objectReferences, isCompatible))
                return;

            DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
            evt.Use();
        }

        /// <summary>
        /// Whether a drag carrying <paramref name="draggedObjects"/> may be dropped on a Material field
        /// that accepts only the Materials <paramref name="isCompatible"/> approves.
        /// </summary>
        /// <remarks>
        /// A payload holding no Material at all is acceptable here: the object field rejects it on type,
        /// as it always has, and there is nothing for this filter to add. A payload holding several
        /// Materials is refused unless every one of them is compatible -- which of them a drop would
        /// land on is not something the user can see, so taking the drag only when the answer does not
        /// matter is the predictable rule.
        /// </remarks>
        static internal bool IsMaterialDragAcceptable(UnityEngine.Object[] draggedObjects, Func<Material, bool> isCompatible)
        {
            if (draggedObjects == null)
                return true;

            for (int i = 0; i < draggedObjects.Length; i++)
            {
                Material material = draggedObjects[i] as Material;
                if (material != null && !isCompatible(material))
                    return false;
            }

            return true;
        }

        static internal bool ContainsVisibleInspectorProperites(Provider2D provider)
        {
            Debug.Assert(provider != null);
            var type = provider.GetType();
            return type.GetFields(BindingFlags.Instance | BindingFlags.Public).Length > 0;
        }

        static internal void DrawHeaderFoldoutWithToggle(GUIContent title, SavedBool foldoutState, SerializedProperty toggleState, string documentationURL = "")
        {
            const float height = 17f;
            var backgroundRect = GUILayoutUtility.GetRect(0, 0);
            float xMin = backgroundRect.xMin;

            var labelRect = backgroundRect;
            labelRect.yMax += height;
            labelRect.xMin += 16f;
            labelRect.xMax -= 20f;

            bool newToggleState = GUI.Toggle(labelRect, toggleState.boolValue, " ");  // Needs a space because the checkbox won't have a proper outline if we don't make a space here
            bool newFoldoutState = CoreEditorUtils.DrawHeaderFoldout("", foldoutState.value);

            if (newToggleState != toggleState.boolValue)
                toggleState.boolValue = newToggleState;

            if (newFoldoutState != foldoutState.value)
                foldoutState.value = newFoldoutState;


            labelRect.xMin += 20;
            EditorGUI.LabelField(labelRect, title, EditorStyles.boldLabel);
        }


        static internal void GUITextureCap(int controlID, Texture texture, Vector3 position, Quaternion rotation, float size, EventType eventType, bool isAngleHandle)
        {
            switch (eventType)
            {
                case (EventType.Layout):
                {
                    Vector2 size2 = Vector2.one * size * 0.5f;
                    if (isAngleHandle)
                        size2.x = 0.0f;

                    HandleUtility.AddControl(controlID, DistanceToRectangle(position, rotation, size2));
                    break;
                }

                case (EventType.Repaint):
                {
                    s_TexCapMaterial.mainTexture = texture;
                    s_TexCapMaterial.SetPass(0);

                    float w = texture.width;
                    float h = texture.height;
                    float max = Mathf.Max(w, h);
                    Vector3 scale = new Vector2(w / max, h / max) * size * 0.5f;

                    if (Camera.current == null)
                        scale.y *= -1f;

                    Matrix4x4 matrix = new Matrix4x4();
                    matrix.SetTRS(position, rotation, scale);

                    Graphics.DrawMeshNow(k_MeshQuad, matrix);
                }
                break;
            }
        }

        static float DistanceToRectangle(Vector3 position, Quaternion rotation, Vector2 size)
        {
            Vector3[] points = { Vector3.zero, Vector3.zero, Vector3.zero, Vector3.zero, Vector3.zero };
            Vector3 sideways = rotation * new Vector3(size.x, 0, 0);
            Vector3 up = rotation * new Vector3(0, size.y, 0);

            points[0] = HandleUtility.WorldToGUIPoint(position + sideways + up);
            points[1] = HandleUtility.WorldToGUIPoint(position + sideways - up);
            points[2] = HandleUtility.WorldToGUIPoint(position - sideways - up);
            points[3] = HandleUtility.WorldToGUIPoint(position - sideways + up);
            points[4] = points[0];

            Vector2 pos = Event.current.mousePosition;
            bool oddNodes = false;
            int j = 4;

            for (int i = 0; i < 5; ++i)
            {
                if ((points[i].y > pos.y) != (points[j].y > pos.y))
                {
                    if (pos.x < (points[j].x - points[i].x) * (pos.y - points[i].y) / (points[j].y - points[i].y) + points[i].x)
                        oddNodes = !oddNodes;
                }

                j = i;
            }

            if (!oddNodes)
            {
                // Distance to closest edge (not so fast)
                float dist, closestDist = -1f;
                j = 1;

                for (int i = 0; i < 4; ++i)
                {
                    dist = HandleUtility.DistancePointToLineSegment(pos, points[i], points[j++]);
                    if (dist < closestDist || closestDist < 0)
                        closestDist = dist;
                }

                return closestDist;
            }
            else
                return 0;
        }

        internal static Renderer2DData GetRenderer2DData()
        {
            UniversalRenderPipelineAsset pipelineAsset = UniversalRenderPipeline.asset;
            if (pipelineAsset == null)
                return null;

            // try get the default
            Renderer2DData rendererData = pipelineAsset.scriptableRendererData as Renderer2DData;
            if (rendererData == null)
            {
                foreach (Camera camera in Camera.allCameras)
                {
                    UniversalAdditionalCameraData additionalCameraData = camera.GetComponent<UniversalAdditionalCameraData>();
                    ScriptableRenderer renderer = additionalCameraData?.scriptableRenderer;
                    Renderer2D renderer2D = renderer as Renderer2D;
                    if (renderer2D != null)
                        return renderer2D.GetRenderer2DData();
                }
            }


            return rendererData;
        }

        internal static bool IsUsing2DRenderer()
        {
            return GetRenderer2DData() != null;
        }
    }
}
