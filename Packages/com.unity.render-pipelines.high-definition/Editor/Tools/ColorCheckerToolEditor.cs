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
    List<string> Modes = new List<string>() { "Color Palette", "Cross Polarized Grayscale", "Middle Gray","Reflection","Stepped Luminance", "Material Palette", "External Texture" };//Displayed names for the color checker modes

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
        dropdownMode.choices = Modes;
        int currentModeIndex = (int)self.Mode;
        if ( currentModeIndex >= 0 && currentModeIndex <= Modes.Count )
        {
            dropdownMode.index = currentModeIndex;
            dropdownMode.RegisterValueChangedCallback(v => 
            {
                self.Mode = (ColorCheckerTool.ColorCheckerModes)dropdownMode.index;
                onChange(self, root);
            });
        }

        //Field counts sliders
        root.Q<SliderInt>("fieldCount").RegisterValueChangedCallback(v => onChange(self, root));
        root.Q<SliderInt>("fieldsPerRow").RegisterValueChangedCallback(v => onChange(self, root));
        root.Q<SliderInt>("materialFieldsCount").RegisterValueChangedCallback(v => onChange(self, root));
        
        //Prepare Color Fields
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
            Toggle metallicToggle = new Toggle() { name ="metallic" + i, label = "is Metallic", tabIndex = i};
            metallicToggle.bindingPath="isMetalBools.Array.data["+i+"]"; 
            metallicToggle.style.display = UnityEngine.UIElements.DisplayStyle.None;//hidden in UI until used
            root.Add(metallicToggle);
        }
        
        //Gradient toggle
        Toggle gradientToggle = root.Q<Toggle>("gradientToggle");
       gradientToggle.RegisterValueChangedCallback(v => 
        {
            GradientField(root,gradientToggle);
        });
        GradientField(root,gradientToggle);

        //Button to reset the custom colors
        root.Q<Button>("resetBtn").clicked += () =>
             {   
                self.ResetColors();
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
            VisualElement newRow = new VisualElement() { name = "colorfieldsRow" + i };
            newRow.style.flexDirection = UnityEngine.UIElements.FlexDirection.Row;
            newRow.style.alignItems = UnityEngine.UIElements.Align.FlexStart;
            newRow.style.justifyContent = UnityEngine.UIElements.Justify.SpaceAround;
            newRow.style.alignSelf = UnityEngine.UIElements.Align.Stretch;
            newRow.style.maxHeight = 22; 
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
            colorInput.SetEnabled(editable);


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
        string[] UINames =new string[]{"colorfields","materialFieldsCount","fieldCount","fieldsPerRow","textureMode","fieldsMargin","gradientElement","sphericalToggle","unlit","resetBtn"};
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
                UINamesToShow = new string[]{"colorfields","fieldCount","fieldsPerRow","fieldsMargin","gradientElement","sphericalToggle","unlit","resetBtn"};
                break;
            case ColorCheckerTool.ColorCheckerModes.Grayscale:
                CreateColorFields(target, root, target.CrossPolarizedGrayscale, 6, 6, false);
                UINamesToShow = new string[]{"colorfields","fieldsMargin","gradientElement","sphericalToggle","unlit"}; 
                break;
            case ColorCheckerTool.ColorCheckerModes.MiddleGray:
                CreateColorFields(target, root, target.MiddleGray, 1, 1, false);
                UINamesToShow = new string[]{"colorfields","fieldsMargin","sphericalToggle","unlit"}; 
                break;
            case ColorCheckerTool.ColorCheckerModes.Reflection:
                UINamesToShow = new string[]{"fieldsMargin"}; 
                break;
            case ColorCheckerTool.ColorCheckerModes.SteppedLuminance:
                CreateColorFields(target, root, target.steppedLuminance, 16, 16, false);
                UINamesToShow = new string[]{"colorfields","gradientElement","unlit"}; 
                break;
            case ColorCheckerTool.ColorCheckerModes.Materials:
                CreateColorFields(target,root,target.customMaterials,root.Q<SliderInt>("materialFieldsCount").value,1,true);
                UINamesToShow = new string[]{"colorfields","materialFieldsCount","fieldsMargin","resetBtn"}; 
                break;
            case ColorCheckerTool.ColorCheckerModes.Texture:
                UINamesToShow = new string[]{"textureMode"}; 
                break;
        }

        //Make chosen settings visible
        for (int i = 0; i <UINamesToShow.Length; i++)
        {
            root.Q<VisualElement>(UINamesToShow[i]).style.display = UnityEngine.UIElements.DisplayStyle.Flex;
        } 

        if(!target.isHDRP)
        {
            root.Q<VisualElement>("unlit").style.display = UnityEngine.UIElements.DisplayStyle.None;
            root.Q<VisualElement>("unlitTextureExposure").style.display = UnityEngine.UIElements.DisplayStyle.None;
            root.Q<VisualElement>("rawTexture").style.display = UnityEngine.UIElements.DisplayStyle.None;
            root.Q<VisualElement>("textureSlice").style.display = UnityEngine.UIElements.DisplayStyle.None;
             root.Q<VisualElement>("faceCam").style.display = UnityEngine.UIElements.DisplayStyle.None;
        }
    
        target.UpdateMaterial();

    }

//Creation
   [MenuItem("GameObject/Rendering/Color Checker Tool", false, 999)]
    public static void CreateColorChecker(MenuCommand menuCommand)
    {   
        var checkerTransform = CoreEditorUtils.CreateGameObject("Color Checker",menuCommand.context);
        var checkerComponent = checkerTransform.AddComponent<ColorCheckerTool>();
        checkerTransform.tag = "EditorOnly";
        Selection.activeObject = checkerTransform;
        Undo.RegisterCreatedObjectUndo(checkerTransform, "Color Checker");
        Undo.RegisterCompleteObjectUndo(checkerTransform, "ColorChecker");
    }

}
#endif