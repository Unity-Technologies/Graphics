using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

[ExecuteInEditMode]
public class SamplesShowcase : MonoBehaviour
{
    public string headline = "Headline Goes Here";
    public Color headlineColor = Color.white;
    public Color linkColor = Color.white;
    public TextAsset SamplesDescriptions;
    public GameObject[] samplesPrefabs;

    public int currentIndex;
    Object currentPrefab;
    int prefabIndex;
    public GameObject instantiatedPrefab;

    bool needUpdate;


    void Start()
    {
#if UNITY_EDITOR
        Selection.activeGameObject = gameObject; //So users see the inspector at scene load
#endif
    }

    void OnValidate()
    {
        needUpdate = true;
    }

    void Update()
    {
        //Controls in GameMode
        if (Application.isFocused)
        {


            if (Input.GetKeyDown(KeyCode.Space))
            {
                SwitchEffect();
            }

        }

        if (needUpdate && currentIndex != prefabIndex)
        {
            CleanChildren();
            InstantiateSample(currentIndex);
            needUpdate = false;
        }
    }

    void SwitchEffect()
    {
        currentIndex += 1;
        currentIndex = currentIndex > samplesPrefabs.Length - 1 ? 0 : currentIndex;
        needUpdate = true;
    }


    void InstantiateSample(int index)
    {
        if (currentIndex <= samplesPrefabs.Length && samplesPrefabs.Length > 0)
        {
            currentPrefab = samplesPrefabs[index];
        }

        //instantiate the prefab as child
        if (currentPrefab != null)
        {
            instantiatedPrefab = Instantiate(currentPrefab, transform.position, Quaternion.identity) as GameObject;
            instantiatedPrefab.transform.parent = gameObject.transform;
            prefabIndex = currentIndex;
        }
    }


    void CleanChildren()
    {
        //We just remove anything that has been spawn as child
        if (transform.childCount > 0)
        {
            foreach (Transform child in transform)
            {
                GameObject.DestroyImmediate(child.gameObject);
            }
        }
    }
}

#if UNITY_EDITOR
[InitializeOnLoad]
[CustomEditor(typeof(SamplesShowcase))]
public class SamplesShowcaseEditor : Editor
{
    private static readonly string UXMLPath = "SamplesSelectionUXML";
    SerializedProperty currentIndex;

    private void OnEnable()
    {
        currentIndex = serializedObject.FindProperty("currentIndex");
    }

    public override VisualElement CreateInspectorGUI()
    {
        var root = new VisualElement();
        var visualTree = Resources.Load<VisualTreeAsset>(UXMLPath);
        VisualElement inspectorUI = visualTree.CloneTree();
        root.Add(inspectorUI);

        var self = (SamplesShowcase)target;
        root.Q<Label>("headline").style.color = self.headlineColor;

        //Create the different sample description from the SamplesDescription text asset
        string wholeText = self.SamplesDescriptions.text;
        //In the text asset, each descriptions is separated with ---
        string[] stringSeparators = new string[] { "---" };
        //Create List of all the different descriptions
        List<string> samplesText = new List<string>();
        foreach (string sampleText in wholeText.Split(stringSeparators, System.StringSplitOptions.None))
        {
            samplesText.Add(sampleText);
        }

        //Introduction, it's the first part of the Samples Description text asset
        var introElement = root.Q<VisualElement>("intro");
        string introText = samplesText.Count > 0 ? samplesText[0] : "";
        CreateMarkdown(introElement, introText, self.linkColor);

        //Create Samples Dropdown
        var dropdownField = root.Q<DropdownField>("SampleDropDown");
        List<string> choices = new List<string>();
        foreach (GameObject samples in self.samplesPrefabs)
        {
            choices.Add(samples.name);
        }
        dropdownField.choices = choices;
        dropdownField.value = choices[currentIndex.intValue];
        GoToSample(self, root, currentIndex.intValue, samplesText);
        dropdownField.RegisterValueChangedCallback(v => GoToSample(self, root, choices.IndexOf(dropdownField.value), samplesText));  //Dropdown function call

        root.Q<Button>("SelectSampleBtn").clicked += () => { SelectSample(self); };

        return root;
    }

    private void GoToSample(SamplesShowcase self, VisualElement root, int index, List<string> samplesText)
    {
        serializedObject.Update();
        currentIndex.intValue = index; //Send the new index value to the monobehaviour
        var sampleInfosElement = root.Q<VisualElement>("sampleInfosContainer");
        string currentSampleText = samplesText.Count > index + 1 ? samplesText[index + 1] : ""; //Update description text, we put +1 because first paragraph is used for introduction 
        CreateMarkdown(sampleInfosElement, currentSampleText, self.linkColor);
        serializedObject.ApplyModifiedProperties();
    }

    private void SelectSample(SamplesShowcase self)
    {
        Transform Sample = self.transform.Find(self.instantiatedPrefab.name);
        Selection.activeGameObject = Sample.gameObject;
    }

    private void CreateMarkdown(VisualElement element, string text, Color linkColor)
    {
        element.Clear();
        foreach (var paragraphText in text.Split('\n')) //Creating paragraph
        {
            var paragraph = new VisualElement();
            paragraph.style.flexDirection = UnityEngine.UIElements.FlexDirection.Row;
            paragraph.style.flexWrap = UnityEngine.UIElements.Wrap.Wrap;
            paragraph.style.justifyContent = UnityEngine.UIElements.Justify.Center;

            foreach (var word in paragraphText.Split(' '))
            {
                var displayText = word;

                // Markdown _ for italics, before each word
                if (word.StartsWith("_")) displayText = $"<i>{word.Replace("_", "")}</i>";

                // Markdown * for bold, before each word
                if (word.StartsWith("*")) displayText = $"<b>{word.Replace("*", "")}</b>";

                // Markdown-style link, but we have to use underscores as word separation:
                // [link_text_here](https:the-url.com)
                var linkText = "";
                if (word.StartsWith("["))
                {
                    var paren = word.IndexOf("(");
                    Debug.Assert(paren > -1, $"Incorrectly formatted link {word}");
                    displayText = word.Substring(1, paren - 2);
                    displayText = displayText.Replace("_", " ");
                    displayText = "<color=#" + ColorUtility.ToHtmlStringRGBA(linkColor) + ">" + "<b><u>" + displayText + "</u></b>" + "</color>";
                    linkText = word.Substring(paren + 1, word.Length - paren - 2);
                }

                var wordElement = new Label(displayText);
                if (linkText != "")
                {
                    wordElement.RegisterCallback<MouseDownEvent>(evt => OpenURL(linkText));
                    wordElement.tooltip = $"opens {linkText}";
                }
                paragraph.Add(wordElement);
            }
            element.Add(paragraph);
        }
    }

    private static void OpenURL(string link)
    {
        if (link.StartsWith("http"))
        {
            Application.OpenURL(link);
        }
        else
        {
            var currentSelection = Selection.objects;
            var linked = AssetDatabase.LoadAssetAtPath<Object>(link);//Not so possible as samples are being moved around when imported to the project
            EditorGUIUtility.PingObject(linked);
            AssetDatabase.OpenAsset(linked);
            // Restore the selection to make it less confusing when selection is changed...
            Selection.objects = currentSelection;
        }
    }

}
#endif






