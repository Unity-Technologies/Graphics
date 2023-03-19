using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

[ExecuteInEditMode]
public class SampleShowcase : MonoBehaviour
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
[CustomEditor(typeof(SampleShowcase))]
public class SampleShowcaseEditor : Editor
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

        var self = (SampleShowcase)target;
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
        if(self.samplesPrefabs.Length > 0)
        {
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
        }
        return root;
    }

    private void GoToSample(SampleShowcase self, VisualElement root, int index, List<string> samplesText)
    {
        serializedObject.Update();
        currentIndex.intValue = index; //Send the new index value to the monobehaviour
        var sampleInfosElement = root.Q<VisualElement>("sampleInfosContainer");
        string currentSampleText = samplesText.Count > index + 1 ? samplesText[index + 1] : ""; //Update description text, we put +1 because first paragraph is used for introduction
        CreateMarkdown(sampleInfosElement, currentSampleText, self.linkColor);
        serializedObject.ApplyModifiedProperties();
    }

    private void SelectSample(SampleShowcase self)
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
            paragraph.style.justifyContent = UnityEngine.UIElements.Justify.FlexStart;

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
                var highlightText = "";
                if (word.StartsWith("["))
                {
                    var paren = word.IndexOf("(");
                    Debug.Assert(paren > -1, $"Incorrectly formatted link {word}");
                    int charsCountToSubtractDisplay = word.EndsWith(")") ? 2 : 1;  
                    displayText = word.Substring(1, paren - 2);
                    displayText = displayText.Replace("_", " ");
                    int charsCountToSubtractLink = word.EndsWith(")") ? 2 : 3;  //This is because if there's a coma or a period after the parenthesis, it's going to be included in the word and will break the link.
                    displayText = "<color=#" + ColorUtility.ToHtmlStringRGBA(linkColor) + ">" + "<b><u>" + displayText + "</u></b>" + "</color>";
                    
                    //If there's a period or coma after the parenthesis, it's included in the word, so we need to add it back. 
                    if(!word.EndsWith(")"))
                        displayText += word[word.Length-1];
                    
                    linkText = word.Substring(paren + 1, word.Length - paren - charsCountToSubtractLink);
                    
                    var wordElementLink = new Label(displayText);
                    if (linkText != "")
                    {
                        wordElementLink.RegisterCallback<MouseDownEvent>(evt => OpenURL(linkText));
                        wordElementLink.tooltip = $"opens '{linkText}'";
                    }
                    paragraph.Add(wordElementLink);
                }
                // Markdown-style link to highlight objects in hierachy, but we have to use underscores as word separation:
                // {highlight_text_here}(name_of_the_gameobject_in_hierarchy)
                else if(word.StartsWith("{"))
                {
                    var paren = word.IndexOf("(");
                    Debug.Assert(paren > -1, $"Incorrectly formatted link {word}");
                    displayText = word.Substring(1, paren - 2);
                    displayText = displayText.Replace("_", " ");
                    displayText = "<color=#" + ColorUtility.ToHtmlStringRGBA(linkColor) + ">" + "<b><u>" + displayText + "</u></b>" + "</color>";
                    
                    //If there's a period or coma after the parenthesis, it's included in the word, so we need to add it back. 
                    if(!word.EndsWith(")"))
                        displayText += word[word.Length-1];
                    
                    int charsCountToSubtractLink = word.EndsWith(")") ? 2 : 3;  //This is because if there's a coma or a period after the parenthesis, it's going to be included in the word and will break the link.
                    highlightText = word.Substring(paren + 1, word.Length - paren - charsCountToSubtractLink);
                    highlightText = highlightText.Replace("_", " ");
                    
                    var wordElementHighlight = new Label(displayText);
                    if (highlightText != "")
                    {
                        wordElementHighlight.RegisterCallback<MouseDownEvent>(evt => Ping(highlightText));
                        wordElementHighlight.tooltip = $"Highlight '{highlightText}'";
                    }
                    paragraph.Add(wordElementHighlight);
                }
                else
                {   
                    var wordLabel = new Label(displayText);
                    paragraph.Add(wordLabel);
                }

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
    
    private static void Ping(string gameObjectName)
    {
        GameObject go = GameObject.Find(gameObjectName);
        UnityEditor.EditorGUIUtility.PingObject(go);
    }
    

}
#endif
