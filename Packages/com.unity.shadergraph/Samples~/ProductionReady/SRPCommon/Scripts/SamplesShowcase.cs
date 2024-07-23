using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
#endif
using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.Rendering;
using System.Text.RegularExpressions;
#if ENABLE_INPUT_SYSTEM 
using UnityEngine.InputSystem;
#endif


[ExecuteInEditMode]
public class PRSSamplesShowcase : MonoBehaviour
{
    
    public string headline = "Headline Goes Here";
    // Color of the headline. light and dark theme variant.
    public Color headlineLightColor = new Color(0.066f,0.066f,0.066f,1f);
    public Color headlineDarkColor = new Color(0.82f,0.82f,0.82f,1f);
    // Color of the text when a link is used to open an asset. light and dark theme variant.
    public Color openLightColor = new Color(0.086f,0.427f,0.792f,1f);
    public Color openDarkColor = new Color(0.478f,0.658f,0.933f,1f);  
    // Color of the text when a link is used to highlight an asset in the scene. light and dark theme variant.
    public Color highlightLightColor = new Color(0.6f,0.4f,0f,1f);  
    public Color highlightDarkColor = new Color(1f,0.89f,0.45f,1f); 
    // Color for code markdown. light and dark theme variant.      
    public Color codeLightColor = new Color(0.76f,0.41f,0f,1f);
    public Color codeDarkColor = new Color(0.91f,0.57f,0.17f,1f);               
    public TextAsset SamplesDescriptionsJson;
    public enum Mode {Instantiation, Focus, TextOnly};
    public GameObject[] samplesPrefabs;
    public Mode PresentationMode = Mode.TextOnly;
    public bool enableSelectButton = true;
    public int currentIndex;
    public GameObject currentPrefab;
    int prefabIndex;
    private Coroutine cameraCoroutine;

    #if UNITY_EDITOR
    [SerializeField]
    public PRSRequiredSettingsSO requiredSettingsSO;
    #endif

    //Variable containing TMPPro compatible sanitized text
    public static string SanitizedIntroduction;
    public static Dictionary<string, string> SanitizedDescriptions;  //Key should be the prefabName, value is the description;
    public static Dictionary<string, string> SanitizedTitles;        //Key should be the prefabName, value is the title;

    public TMP_Text gameobjectSamplesName;  //if we want to have the samples name on a text mesh pro asset.
    public TMP_Text gameobjectSamplesDescription;  //if we want to have the samples name on a text mesh pro asset.

    //Camera used to update view of samples focus in gameview
    public Camera gameViewCamera;

    private bool needUpdate;
    private Vector3 savedPrefabPosition;
    
    //Delegate to update every instance of samples showcase inspector UI
    #if UNITY_EDITOR
    public delegate void UpdateSamplesInspectorDelegate();
    public static UpdateSamplesInspectorDelegate OnUpdateSamplesInspector;
    void UpdateSamplesInspector()
    {
        if (OnUpdateSamplesInspector != null)
        {
            OnUpdateSamplesInspector();
        }
    }
    #endif

	void OnEnable()
	{
		// JSon data of the samples
        if (SamplesDescriptionsJson != null)
        {
            string jsonText = CleanupJson(SamplesDescriptionsJson.text);

            PRSSamples sampleJsonObject = PRSSamples.CreateFromJSON(jsonText, samplesPrefabs);

            //Introduction, it's the first part of the Samples Description text asset
            string introText = sampleJsonObject.introduction;
            PRSSamplesShowcase.SanitizedIntroduction = SanitizeText(introText);

            PRSSamplesShowcase.SanitizedDescriptions = new Dictionary<string, string>();
            PRSSamplesShowcase.SanitizedTitles = new Dictionary<string, string>();
            foreach(GameObject prefab in samplesPrefabs)
            {
                PRSSample currentSample = sampleJsonObject.FindSampleWithPrefab(prefab);
                if (currentSample == null)
                    continue;

                string description = SanitizeText(currentSample.description);
                PRSSamplesShowcase.SanitizedDescriptions.Add(prefab.name, description);
                PRSSamplesShowcase.SanitizedTitles.Add(prefab.name, currentSample.title);
            }
		}
	}

    void OnValidate()
    {
        needUpdate = true;
    }

    void Update()
    {
        if (PresentationMode != Mode.TextOnly)
        {
            //Controls in GameMode
            if (Application.isFocused && Application.isPlaying)
            {
#if ENABLE_LEGACY_INPUT_MANAGER
                if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.UpArrow) )
                {
                    SwitchEffect(currentIndex+1);
                }
                if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.DownArrow) )
                {
                    SwitchEffect(currentIndex-1);
                }
#endif

#if ENABLE_INPUT_SYSTEM
                if(Keyboard.current.rightArrowKey.wasPressedThisFrame || Keyboard.current.upArrowKey.wasPressedThisFrame)
                {
                    SwitchEffect(currentIndex+1);
                } 
                
                if(Keyboard.current.leftArrowKey.wasPressedThisFrame ||Keyboard.current.downArrowKey.wasPressedThisFrame)
                {
                    SwitchEffect(currentIndex-1);
                }           
#endif

            }

            if (needUpdate)
            {
                PresentSample(currentIndex);
                needUpdate = false;
            }
        }


    }

    void SwitchEffect(int value)
    {
        currentIndex = value;
        currentIndex = currentIndex > samplesPrefabs.Length - 1 ? 0 : currentIndex;
        currentIndex = currentIndex < 0 ? samplesPrefabs.Length - 1 : currentIndex;
        needUpdate = true;
    }


    void PresentSample(int index)
    {
        switch (PresentationMode)
        {
            case Mode.Instantiation:
                if (index != prefabIndex)
                {
                    CleanChildren();
                    if (index <= samplesPrefabs.Length && samplesPrefabs.Length > 0)
                    {
                        currentPrefab = samplesPrefabs[index];
                    }

                    // Instantiate the prefab as child
                    if (currentPrefab != null)
                    {
                        GameObject instantiatedPrefab = Instantiate(currentPrefab, transform.position, Quaternion.identity) as GameObject;
                        instantiatedPrefab.transform.parent = gameObject.transform;
                        instantiatedPrefab.transform.localRotation = Quaternion.identity;
                        currentPrefab = instantiatedPrefab;
                        prefabIndex = index;
                        
                        // This is for keeping the prefab at the same position as before
                        instantiatedPrefab.transform.position = savedPrefabPosition;
                            
                    }
                }    
            break;

            case Mode.Focus:
                if (index <= samplesPrefabs.Length && samplesPrefabs.Length > 0)
                    {
                        currentPrefab = samplesPrefabs[index];
                    }

                if (currentPrefab != null)
                {
                   Transform viewPoint = currentPrefab.transform.Find("ViewPoint");
                    if (viewPoint != null)
                    {
                        // Align Scene View Camera
                        #if UNITY_EDITOR
                        SceneView view = SceneView.lastActiveSceneView;
                        if (view != null)
                        {
                            view.AlignViewToObject(viewPoint.transform);
                        }
                        #endif

                        // Align game view if a gameViewCamera is set
                        if (gameViewCamera != null)
                        {
                            if(cameraCoroutine !=null)
                            {
                                StopCoroutine(cameraCoroutine);
                            }
                            cameraCoroutine = StartCoroutine(lerpTransform(gameViewCamera.transform, viewPoint));
                        }
                    }
                }
            break;
        }
    
        #if UNITY_EDITOR
        UpdateSamplesInspector();
        #endif
    }

    private IEnumerator lerpTransform(Transform transformA, Transform transformB)
    {
        float startTime=Time.time; 
        while(Time.time-startTime<=1)//one second
        { 
            transformA.position=Vector3.Lerp(transformA.position,transformB.position,Time.time-startTime); 
            transformA.rotation = Quaternion.Lerp(transformA.rotation,transformB.rotation, Time.time-startTime);
            yield return 1; // wait for next frame
        }
    }


    void CleanChildren()
    {
        //We just remove anything that has been spawn as child
        if (transform.childCount > 0)
        {
            // This is for keeping the prefab at the same position as before
            savedPrefabPosition = transform.GetChild(0).position;
            
            foreach (Transform child in transform)
            {
                GameObject.DestroyImmediate(child.gameObject);
            }
        }
    }
    
    public static string GetSanitizedDescription(string prefabName)
    {
        if(SanitizedDescriptions == null) return "";
        
        if(SanitizedDescriptions.ContainsKey(prefabName))
            return SanitizedDescriptions[prefabName];
            
        return "";
    }
    
    public static string GetSanitizedTitle(string prefabName)
    {
        if(SanitizedTitles == null) return "";
        
        if(SanitizedTitles.ContainsKey(prefabName))
            return SanitizedTitles[prefabName];
          
        return "";
    }
    
    public static string GetSanitizedIntroduction()
    {
        return SanitizedIntroduction;
    }
	
	public static string SanitizeText(string text)
    {
        // Convert <br> to line break characters
        text = text.Replace("<br>", "\n");

        // Format <link> and <a>
        text = Regex.Replace(text, @"<a[\s\S]*?>([\s\S]*?)<\/a>", $"$1");
        text = Regex.Replace(text, @"<link[\s\S]*?>([\s\S]*?)<\/link>", $"$1");

        // Add some offset to lists
        text = text.Replace("�", "  �");

        // Remove text between <ignore> tags
        text = Regex.Replace(text, @"<ignore>[\s\S]*?</ignore>", "");

        return text;
    }
	
	public static string CleanupJson(string jsonString)
    {
        // Reformat json
        // For text between triple quotes, remove \r \n \t characters

        // Select text between triple quotes
        string pattern = @"(\""\""\"")([\s\S]*?)(\""\""\"")";

        // Clean it
        jsonString = Regex.Replace(jsonString, pattern, m =>
        {
            string tripleQuotedText = m.Groups[2].Value;
            // Replace newline by <br>
            tripleQuotedText = tripleQuotedText.Replace("\n", "<br>");
            // Escape quotes
            tripleQuotedText = tripleQuotedText.Replace("\"", "\\\"");
            // Remove carriage return and tabs
            tripleQuotedText = Regex.Replace(tripleQuotedText, "[\r\t]", "");

            return $"\"{tripleQuotedText}\"";
        });

        return jsonString;
    }

}
