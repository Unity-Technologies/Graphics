using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class PlayModeTestsUI : MonoBehaviour
{
    static PlayModeTestsUI _instance;
    public static PlayModeTestsUI instance
    {
        get
        {
            if (_instance == null )
            {
                _instance = FindObjectOfType<PlayModeTestsUI>();
            }

            return _instance;
        }
    }
    public static bool Exists
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<PlayModeTestsUI>();
                return false;
            }

            return _instance != null;
        }
    }

    [SerializeField] int frameWait = 100;

    [SerializeField] GameObject resultsPanel, waitingPanel, scenePanel;

    [SerializeField] Text overallAvgText;
    [SerializeField] RectTransform overallAvgFill;
    [SerializeField] Text overallMaxText;
    [SerializeField] RectTransform overallMaxFill;

    [SerializeField] RectTransform testResultPrefab;
    [SerializeField] ScrollRect scrollView;

    [SerializeField] RawImage resultImage;
    Material resultComparerMaterial;

    [SerializeField] Gradient fillGradient = new Gradient() { colorKeys = new GradientColorKey[] {
        new GradientColorKey( Color.red, 0.5f),
        new GradientColorKey( Color.yellow, 0.75f),
        new GradientColorKey( Color.green, 0.97f),
            } };

    int numOfResults = 1;
    List<GameObject> testResults;
    int[] avgResults;
    int[] maxResults;
    Text[] resultsLabels;
    Text[] resultsAvgValue;
    RectTransform[] resultsAvgFill;
    Text[] resultsMaxValue;
    RectTransform[] resultsMaxFill;

    EventSystem eventSystem;
    GameObject lastSelected;

    Texture2D currentTemplate;
    RenderTexture currentRT;
    RenderTexture resultRT;

    Text scenePanelText;

    Image waitingImage;

    // Use this for initialization
    void Start ()
    {
        // kill itself if already in scene.
        if (Exists)
        {
            Debug.Log("Kill UIObject because it already exists.");
            Destroy(gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject);

        //Debug.Log("Need to create " + (UnityEngine.SceneManagement.SceneManager.sceneCountInBuildSettings - 1) + " test result object.");

        // Set scroll view content to fit all results
        scrollView.content.anchorMin = new Vector2(0f, 0f);
        scrollView.content.anchorMax = new Vector2(1f, 0f);
        scrollView.content.offsetMin = new Vector2(0f, - (UnityEngine.SceneManagement.SceneManager.sceneCountInBuildSettings - 1f) * 200f);
        scrollView.content.offsetMax = new Vector2(0f, 0f);

        // Init results arrays
        numOfResults = UnityEngine.SceneManagement.SceneManager.sceneCountInBuildSettings - 1;
        avgResults = new int[numOfResults];
        maxResults = new int[numOfResults];
        resultsLabels = new Text[numOfResults];
        resultsAvgValue = new Text[numOfResults];
        resultsAvgFill = new RectTransform[numOfResults];
        resultsMaxValue = new Text[numOfResults];
        resultsMaxFill = new RectTransform[numOfResults];
        testResults = new List<GameObject>(numOfResults);

        // Create results UI
        for (int i = 0; i < numOfResults; ++i)
        {
            RectTransform singleTestResult = Instantiate(testResultPrefab);

            testResults.Add(singleTestResult.gameObject);
            resultsLabels[i] = singleTestResult.Find("Label").GetComponent<Text>();
            resultsAvgValue[i] = singleTestResult.Find("Avg_Value/Text").GetComponent<Text>();
            resultsAvgFill[i] = singleTestResult.Find("Avg_Value/Fill").GetComponent<RectTransform>();
            resultsMaxValue[i] = singleTestResult.Find("Max_Value/Text").GetComponent<Text>();
            resultsMaxFill[i] = singleTestResult.Find("Max_Value/Fill").GetComponent<RectTransform>();

            singleTestResult.SetParent(scrollView.content);
            singleTestResult.anchorMin = new Vector2(0, 0);
            singleTestResult.anchorMax = new Vector2(1, 0);
            singleTestResult.offsetMin = new Vector2(0, scrollView.content.rect.height - (i + 1) * 200f);
            singleTestResult.offsetMax = new Vector2(0, singleTestResult.offsetMin.y + 200f);

            int sceneIndex = i;
            singleTestResult.GetComponent<Button>().onClick.AddListener(delegate () { LoadSceneResult(sceneIndex); });

            //* Test the values
            SetResult(i, 1.0f * i / (numOfResults - 1), 0.5f);
        }
        CalculateOverall();

        scrollView.Rebuild( UnityEngine.UI.CanvasUpdate.PostLayout );

        eventSystem = GetComponentInChildren<EventSystem>();
        eventSystem.SetSelectedGameObject(testResults[0]);

        resultsPanel.SetActive(false);
        waitingPanel.SetActive(false);
        scenePanel.SetActive(false);

        resultComparerMaterial = Instantiate( resultImage.material );
        resultImage.material = resultComparerMaterial;

        // Initialize render textures
        currentRT = new RenderTexture(1920, 1080, 0, RenderTextureFormat.ARGB32);
        resultRT = new RenderTexture(1920, 1080, 0, RenderTextureFormat.ARGB32);

        scenePanelText = scenePanel.transform.Find("Text").GetComponent<Text>();
        waitingImage = waitingPanel.transform.Find("Image").GetComponent<Image>();

        // Load scenes and calculate the results.
        StartCoroutine(CalculateAllResults());
    }

    void SetResult(int _index, float _avgValue, float _maxValue)
    {
        avgResults[_index] = Mathf.RoundToInt(_avgValue * 100f);

        resultsAvgValue[_index].text = avgResults[_index].ToString() + "%";
        resultsAvgFill[_index].localScale = new Vector3(_avgValue, 1f, 1f);
        resultsAvgFill[_index].GetComponent<Image>().color = fillGradient.Evaluate(_avgValue);

        maxResults[_index] = Mathf.RoundToInt(_maxValue * 100f);

        resultsMaxValue[_index].text = maxResults[_index].ToString() + "%";
        resultsMaxFill[_index].localScale = new Vector3(_maxValue, 1f, 1f);
        resultsAvgFill[_index].GetComponent<Image>().color = fillGradient.Evaluate(1f-_maxValue);
    }

    void CalculateOverall()
    {
        float overallAvgResult = 0f;
        float overallMaxResult = 0f;
        for (int i=0; i<numOfResults; ++i)
        {
            overallAvgResult += 1.0f * avgResults[i];
            overallMaxResult += 1.0f * maxResults[i];
        }
        overallAvgResult /= numOfResults;
        overallMaxResult /= numOfResults;

        overallAvgFill.localScale = new Vector3(overallAvgResult * 0.01f, 1f, 1f);
        overallAvgText.text = Mathf.RoundToInt(overallAvgResult).ToString() + "%";
        overallAvgFill.GetComponent<Image>().color = fillGradient.Evaluate(overallAvgResult * 0.01f);

        overallMaxFill.localScale = new Vector3(overallMaxResult * 0.01f, 1f, 1f);
        overallMaxText.text = Mathf.RoundToInt(overallMaxResult).ToString() + "%";
        overallMaxFill.GetComponent<Image>().color = fillGradient.Evaluate(1f- overallMaxResult * 0.01f);
    }

    IEnumerator CalculateAllResults()
    {
        resultsPanel.SetActive(false);
        waitingPanel.SetActive(true);
        scenePanel.SetActive(false);

        waitingImage.fillAmount = 0f;

        for (int i=0; i<numOfResults;++i)
        {
            yield return CalculateResult(i);
            
            waitingImage.fillAmount = 1f * (1f+i) / numOfResults;
            waitingImage.color = Color.Lerp(Color.blue, Color.green, 1f * i / (numOfResults - 1));
        }

        CalculateOverall();

        UnityEngine.SceneManagement.SceneManager.LoadScene(0);

        resultsPanel.SetActive(true);
        waitingPanel.SetActive(false);
        scenePanel.SetActive(false);

        yield return null;
    }

    IEnumerator CalculateResult(int _index)
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(_index + 1);

        yield return null;

        var testSetup = FindObjectOfType<UnityEngine.Experimental.Rendering.SetupSceneForRenderPipelineTest>();
        testSetup.Setup();

        for (int f = 0; f < frameWait; ++f)
        {
            yield return null;
        }

        resultsLabels[_index].text = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

        Debug.Log("Get " + UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);

        // find the reference image
        currentTemplate = Resources.Load<Texture2D>(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name + ".unity");
        resultImage.texture = currentTemplate;

        // Descriptor for the render textures
        RenderTextureDescriptor desc = new RenderTextureDescriptor()
        {
            width = currentTemplate.width,
            height = currentTemplate.height,
            autoGenerateMips = false,
            colorFormat = RenderTextureFormat.ARGB32,
            msaaSamples = testSetup.msaaSamples,
            sRGB = true,
            volumeDepth = 1,
            dimension = UnityEngine.Rendering.TextureDimension.Tex2D,
        };

        // re-create the render textures to correct size
        DestroyImmediate(currentRT);
        currentRT = new RenderTexture(desc);
        DestroyImmediate(resultRT);
        resultRT = new RenderTexture(desc);
        currentRT.filterMode = resultRT.filterMode = FilterMode.Bilinear;
        currentRT.wrapMode = resultRT.wrapMode = TextureWrapMode.Clamp;

        Camera testCamera = testSetup.cameraToUse;

        // render the scene
        var oldTarget = testSetup.cameraToUse.targetTexture;
        testSetup.cameraToUse.targetTexture = currentRT;
        testSetup.cameraToUse.Render();
        testSetup.cameraToUse.targetTexture = oldTarget;

        // render comparision RT
        resultComparerMaterial.SetFloat("_ResultSplit", 1f);
        resultComparerMaterial.SetInt("_Mode", 4);
        resultComparerMaterial.SetTexture("_MainTex", currentTemplate);
        resultComparerMaterial.SetTexture("_CompareTex", currentRT);

        Graphics.Blit(currentTemplate, resultRT, resultComparerMaterial);

        // Readback the rendered texture
        var oldActive = RenderTexture.active;
        RenderTexture.active = resultRT;
        var captured = new Texture2D(resultRT.width, resultRT.height, TextureFormat.RGB24, false);
        captured.ReadPixels(new Rect(0, 0, testSetup.width, testSetup.height), 0, 0);
        RenderTexture.active = oldActive;

        // compare
        Vector2 compareResult = ReadCompareTexture(captured);

        SetResult(_index, 1f - compareResult.x, compareResult.y);

        // Set the compare material values for display if needed
        resultComparerMaterial.SetFloat("_Split", 0.5f);
        resultComparerMaterial.SetFloat("_ResultSplit", 0f);
        resultComparerMaterial.SetInt("_Mode", 4);

        yield return null;
    }

    private Vector2 ReadCompareTexture(Texture2D captured)
    {
        if (captured == null)
            return Vector2.one;

        var pixels = captured.GetPixels();
        int numberOfPixels = pixels.Length;

        float sumOfDiff = 0;
        float maxDiff = 0f;
        for (int i = 0; i < numberOfPixels; i++)
        {
            maxDiff = Mathf.Max(maxDiff, pixels[i].r);
            sumOfDiff += pixels[i].r;
        }

        return new Vector2(Mathf.Sqrt(sumOfDiff / numberOfPixels), maxDiff);
    }

    private Vector2 CompareTextures(Texture2D fromDisk, Texture2D captured)
    {
        if (fromDisk == null || captured == null)
            return Vector2.one;

        if (fromDisk.width != captured.width
            || fromDisk.height != captured.height)
            return Vector2.one;

        var pixels1 = fromDisk.GetPixels();
        var pixels2 = captured.GetPixels();

        if (pixels1.Length != pixels2.Length)
            return Vector2.one;

        int numberOfPixels = pixels1.Length;

        float sumOfSquaredColorDistances = 0;
        float maxSquaredColorDistance = 0f;
        for (int i = 0; i < numberOfPixels; i++)
        {
            Color p1 = pixels1[i];
            Color p2 = pixels2[i];

            Color diff = p1 - p2;
            diff = diff * diff;
            maxSquaredColorDistance = Mathf.Max(maxSquaredColorDistance, (diff.r + diff.g + diff.b) / 3.0f);
            sumOfSquaredColorDistances += (diff.r + diff.g + diff.b) / 3.0f;
        }

        return new Vector2( Mathf.Sqrt(sumOfSquaredColorDistances / numberOfPixels), maxSquaredColorDistance );
    }

    void LoadSceneResult(int _index)
    {
        if ((_index <0 ) || (_index >= ( UnityEngine.SceneManagement.SceneManager.sceneCountInBuildSettings-1))) return;

        Debug.Log("Load Scene Results : " + _index);

        // CalculateResult(_index);
        StartCoroutine(CalculateResult(_index));

        scenePanelText.text = resultsLabels[_index].text;

        resultsPanel.SetActive(false);
        waitingPanel.SetActive(false);
        scenePanel.SetActive(true);
    }

    // Update is called once per frame
    void Update ()
    {
        if (resultsPanel.activeSelf)
        {
            if (eventSystem.currentSelectedGameObject != lastSelected)
            {
                lastSelected = eventSystem.currentSelectedGameObject;

                if (testResults.Contains(lastSelected)) scrollView.verticalNormalizedPosition = 1.0f - 1.0f * testResults.IndexOf(lastSelected) / (numOfResults - 1f);
            }
        }

        if (scenePanel.activeSelf)
        {
            // Scale the raw image to display at the correct aspect ratio
            Rect parentRect = resultImage.rectTransform.parent.GetComponent<RectTransform>().rect;
            float parentRatio = parentRect.width / parentRect.height;
            float templateRatio = resultImage.texture.width / resultImage.texture.height;
            Debug.Log("Parent : " + parentRect.width + " / " + parentRect.height + " |||  Child : " + resultImage.texture.width + " / " + resultImage.texture.height);
            float resultRatio = 2f * parentRatio / templateRatio;
            if (resultRatio > 1)
            {
                resultImage.rectTransform.anchorMin = new Vector2(0.5f / resultRatio, 0f);
                resultImage.rectTransform.anchorMax = new Vector2(1f - 0.5f / resultRatio, 1f);
            }
            else
            {
                resultImage.rectTransform.anchorMin = new Vector2(0f, 0.5f / resultRatio);
                resultImage.rectTransform.anchorMax = new Vector2(1f, 1f - 0.5f / resultRatio);
            }

            resultComparerMaterial.SetFloat("_Split", Mathf.Clamp01(resultComparerMaterial.GetFloat("_Split") + Input.GetAxis("Horizontal") * Time.deltaTime));
            resultComparerMaterial.SetFloat("_ResultSplit", Mathf.Clamp01(resultComparerMaterial.GetFloat("_ResultSplit") + Input.GetAxis("Vertical") * Time.deltaTime));

            if (Input.GetButtonDown("Fire3")) resultComparerMaterial.SetInt("_Mode", (resultComparerMaterial.GetInt("_Mode") + 1) % 6);

            if (Input.GetButtonDown("Fire2"))
            {
                resultsPanel.SetActive(true);
                waitingPanel.SetActive(false);
                scenePanel.SetActive(false);
            }
        }
	}
}
