using System.Collections.Generic;
using System.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEditor.Rendering;

#if UNITY_EDITOR
[InitializeOnLoad]
[CustomEditor(typeof(ColorCheckerTool))]

/// <summary>
/// Inspector for the color checker.
/// </summary>
public class ColorCheckerToolEditor : Editor
{
    private static readonly string UXMLPath = "ColorCheckerUI";
   
    private void OnEnable()
    {
        var self = (ColorCheckerTool)target;
    }

    public override VisualElement CreateInspectorGUI()
    {
        //uxml setup
        var root = new VisualElement();
        var visualTree = Resources.Load<VisualTreeAsset>(UXMLPath);
        VisualElement inspectorUI = visualTree.CloneTree();
        root.Add(inspectorUI);
        var self = (ColorCheckerTool)target;

        //Mode Dropdown
        var dropdownMode = root.Q<DropdownField>("ModesDropdown");
        dropdownMode.RegisterValueChangedCallback(v => onChange(self, root));

        //Field counts sliders
        root.Q<SliderInt>("fieldCount").RegisterValueChangedCallback(v => onChange(self, root));
        root.Q<SliderInt>("fieldsPerRow").RegisterValueChangedCallback(v => onChange(self, root));
        root.Q<SliderInt>("materialFieldsCount").RegisterValueChangedCallback(v => onChange(self, root));

        //Sphere Mode Toggle needs to update geometry
        root.Q<Toggle>("sphereModeToggle").RegisterValueChangedCallback(v=> self.UpdateGeometry());
        //fields margin regenerates the spheres if sphere mode is used
        root.Q<Slider>("fieldsMargin").RegisterValueChangedCallback(v=> {if(self.sphereModeToDisplay == true) {self.UpdateGeometry();}});
        
        //callback for face view
        root.Q<Button>("moveToViewButton").clicked += () => 
        {
            SceneView view = SceneView.lastActiveSceneView;
            if (view != null)
            {
                GameObject currentSelection = Selection.activeGameObject;
                Selection.activeGameObject = self.gameObject;
                view.AlignWithView();
                view.MoveToView(self.transform);
                Selection.activeGameObject = currentSelection;
            }
        };


        //Prepare Color Fields, they have to be instanciated in advance to make sure bindings work
        for(int i=0; i<64;i++)
        {
            ColorField colorInput = new ColorField() { name = "Color" + i, tabIndex = i, showAlpha = false};
            colorInput.bindingPath="textureColors.Array.data["+i+"]"; 
            colorInput.style.display = UnityEngine.UIElements.DisplayStyle.None;//hidden in UI until used
            root.Add(colorInput);
        }

        //Prepare metallic toggles for material mode
        for(int i=0; i<12;i++)
        {
            Toggle metallicToggle = new Toggle() { name ="metallic" + i, label = "Metallic", tabIndex = i};
            metallicToggle.Q<Label>().style.minWidth = 60;
            metallicToggle.bindingPath="isMetalBools.Array.data["+i+"]"; 
            metallicToggle.style.display = UnityEngine.UIElements.DisplayStyle.None;//hidden in UI until used
            root.Add(metallicToggle);
        }
        
        //Gradient toggle
        Toggle gradientToggle = root.Q<Toggle>("gradientToggle");
       gradientToggle.RegisterValueChangedCallback(v => 
        {
            GradientField(root,gradientToggle);
            onChange(self, root);
        });
        GradientField(root,gradientToggle);

        //Button to reset the custom colors
        root.Q<Button>("resetBtn").clicked += () =>
             {   
                self.ResetColors();
                self.UpdateMaterial();
             };


        onChange(self, root); // Initialize
        return root;
    }

    void GradientField(VisualElement root, Toggle gradientToggle)
    {
        root.Q<ColorField>("gradientA").SetEnabled(gradientToggle.value);
        root.Q<ColorField>("gradientB").SetEnabled(gradientToggle.value);
        root.Q<FloatField>("gradientPower").SetEnabled(gradientToggle.value);
    }

    void CreateColorFields(ColorCheckerTool target, VisualElement root, Color32[] colors, int fieldCount, int fieldsperRow, bool editable)
    {
        //Initialize the Rows for the colorfields
        int rows = (int)Mathf.Ceil((float)fieldCount / (float)fieldsperRow);
        VisualElement colorfieldsRoot = root.Q<VisualElement>("colorfields");
        for(int i=0; i<64;i++)
        {   
            ColorField colorInput = root.Q<ColorField>("Color"+i);
            root.Add(colorInput);
            colorInput.style.display = UnityEngine.UIElements.DisplayStyle.None;
        }
        for(int i=0; i<12;i++)
        {   
            Toggle metallicToggle = root.Q<Toggle>("metallic"+i);
            root.Add(metallicToggle);
            metallicToggle .style.display = UnityEngine.UIElements.DisplayStyle.None;
        }
        colorfieldsRoot.Clear();

        //Creates the Rows Containers
        for (int i = 0; i < rows; i++)
        {
            VisualElement newRow = new()
            {
                name = "colorfieldsRow" + i,
                style =
                {
                    flexDirection = UnityEngine.UIElements.FlexDirection.Row,
                    alignItems = UnityEngine.UIElements.Align.FlexStart,
                    justifyContent = UnityEngine.UIElements.Justify.SpaceAround,
                    alignSelf = UnityEngine.UIElements.Align.Stretch,
                    maxHeight = 22,
                    flexWrap = Wrap.Wrap
                }
            };
            colorfieldsRoot.Add(newRow);
        }

        //Update colors for texture
        for (int i = 0; i < colors.Length; i++)
        {
            if (i<64)
            {
                target.textureColors[i]=colors[i];
            }
        }


        //Create the color fields
        for (int i = 0; i < fieldCount; i++)
        {
            ColorField colorInput = root.Q<ColorField>("Color"+i);
            colorInput.style.display = UnityEngine.UIElements.DisplayStyle.Flex;
            colorInput.showEyeDropper = editable;
            colorInput.pickingMode = editable ? UnityEngine.UIElements.PickingMode.Position : UnityEngine.UIElements.PickingMode.Ignore;
            //colorInput.SetEnabled(editable); //better to let this editable so that users can check out the presets color values. those values won't be saved.


            //Resize the colorfields nicely when they fall short to fill the last row
            int fieldsInFullRowCount = fieldsperRow * ((int)fieldCount / fieldsperRow);
            int fieldsInCurrentRow;
            if (fieldsInFullRowCount > 0)
            {
                int fieldsInLastRowCount = fieldCount % fieldsInFullRowCount;
                bool isLastRow = i + 1 - fieldsInFullRowCount > 0;
                fieldsInCurrentRow = isLastRow ? fieldsInLastRowCount : fieldsperRow;
            }
            else
            {
                fieldsInCurrentRow = fieldCount;
            }

            colorInput.style.width = new Length(100 / fieldsInCurrentRow, LengthUnit.Percent);

            VisualElement colorfieldsRow = colorfieldsRoot.Q<VisualElement>("colorfieldsRow" + (int)i / fieldsperRow);
            colorfieldsRow.Add(colorInput);

             //Add the Metallic Toggle for Material Mode
             if (target.Mode == ColorCheckerTool.ColorCheckerModes.Materials)
             {  
                Toggle metallicToggle  = root.Q<Toggle>("metallic"+i); 
                metallicToggle.style.display = UnityEngine.UIElements.DisplayStyle.Flex;
                metallicToggle.RegisterValueChangedCallback (v =>
                {
                    target.textureColors[metallicToggle.tabIndex].a = metallicToggle.value? (byte)255 : (byte)0;
                    target.UpdateMaterial();
                });
                colorInput.style.width = new Length(50, LengthUnit.Percent);
                colorfieldsRow.Add(metallicToggle);
             }

        }
    }


    void onChange(ColorCheckerTool target, VisualElement root)
    {
        //reset UI settings visibility
        string[] UINames =new string[]{"colorfields","materialFieldsCount","fieldCount","fieldsPerRow","textureMode","fieldsMargin","gradientElement","sphereModeToggle","unlit","resetBtn"};
        for (int i = 0; i < UINames.Length; i++)
        {
            root.Q<VisualElement>(UINames[i]).style.display = UnityEngine.UIElements.DisplayStyle.None;
        } 

        string[] UINamesToShow = new string[]{};

        //Update the colorfields
        switch (target.Mode)
        {
            case ColorCheckerTool.ColorCheckerModes.Colors:

                CreateColorFields(target, root, target.customColors, root.Q<SliderInt>("fieldCount").value, target.fieldsPerRow, true);
                UINamesToShow = new string[]{"colorfields","fieldCount","fieldsPerRow","fieldsMargin","gradientElement","sphereModeToggle","unlit","resetBtn"};
                root.Q<Label>("Info").text="This procedural color checker can be used for color and lighting calibration. Color fields are customisable and persistent, with up to 64 values.";
                break;
            case ColorCheckerTool.ColorCheckerModes.Grayscale:
                CreateColorFields(target, root, target.CrossPolarizedGrayscale, 6, 6, false);
                UINamesToShow = new string[]{"colorfields","fieldsMargin","gradientElement","sphereModeToggle","unlit"}; 
                root.Q<Label>("Info").text="These values have been measured without specular lighting using a cross-polarized filter, making it more accurate for light calibration in PBR.";
                break;
            case ColorCheckerTool.ColorCheckerModes.MiddleGray:
                CreateColorFields(target, root, target.MiddleGray, 1, 1, false);
                UINamesToShow = new string[]{"colorfields","fieldsMargin","sphereModeToggle","unlit"}; 
                root.Q<Label>("Info").text="This is neutral 5, the mid-gray value.";
                break;
            case ColorCheckerTool.ColorCheckerModes.Reflection:
                UINamesToShow = new string[]{"fieldsMargin"}; 
                root.Q<Label>("Info").text="Useful for checking local reflections.";
                break;
            case ColorCheckerTool.ColorCheckerModes.SteppedLuminance:
                CreateColorFields(target, root, target.steppedLuminance, 16, 16, false);
                UINamesToShow = new string[]{"colorfields","gradientElement","unlit"}; 
                root.Q<Label>("Info").text="Stepped luminance is a good way to check gamma calibration.";
                break;
            case ColorCheckerTool.ColorCheckerModes.Materials:
                CreateColorFields(target,root,target.customMaterials,root.Q<SliderInt>("materialFieldsCount").value,1,true);
                UINamesToShow = new string[]{"colorfields","materialFieldsCount","fieldsMargin","resetBtn"}; 
                root.Q<Label>("Info").text="Each row represents a material with varying smoothness. Material fields are customizable and persistent, with up to 12 values.";
                break;
            case ColorCheckerTool.ColorCheckerModes.Texture:
                UINamesToShow = new string[]{"textureMode"}; 
                root.Q<Label>("Info").text="Useful for calibration using captured data. Use the slicer to compare lit values to unlit, raw values. Pre-exposure can be disabled.";
                break;
        }

        //Make chosen settings visible
        for (int i = 0; i <UINamesToShow.Length; i++)
        {
            root.Q<VisualElement>(UINamesToShow[i]).style.display = UnityEngine.UIElements.DisplayStyle.Flex;
        } 

        target.UpdateMaterial();
        target.UpdateGeometry();

    }


//Creation
   [MenuItem("GameObject/Rendering/Color Checker Tool", false, 999)]
    public static void CreateColorChecker(MenuCommand menuCommand)
    {   
        var newColorChecker = CoreEditorUtils.CreateGameObject("Color Checker",menuCommand.context);
        var checkerComponent = newColorChecker.AddComponent<ColorCheckerTool>();
        newColorChecker.tag = "EditorOnly";
        newColorChecker.hideFlags = HideFlags.DontSaveInBuild;
        Selection.activeObject = newColorChecker;
        //Place color checker in view
        SceneView view = SceneView.lastActiveSceneView;
        if (view != null)
        {
                view.AlignWithView();
                view.MoveToView(newColorChecker.transform);
                newColorChecker.transform.eulerAngles = new Vector3(0f,newColorChecker.transform.eulerAngles.y,newColorChecker.transform.eulerAngles.z); //so that it stays upright
        }

        Undo.RegisterCreatedObjectUndo(newColorChecker, "Color Checker");
        Undo.RegisterCompleteObjectUndo(newColorChecker, "ColorChecker");
    }

}
#endif


