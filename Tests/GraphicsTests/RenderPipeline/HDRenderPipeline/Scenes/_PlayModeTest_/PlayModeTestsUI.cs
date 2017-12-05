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

            singleTestResult.parent = scrollView.content;
            singleTestResult.anchorMin = new Vector2(0, 0);
            singleTestResult.anchorMax = new Vector2(1, 0);
            singleTestResult.offsetMin = new Vector2(0, scrollView.content.rect.height - (i + 1) * 200f);
            singleTestResult.offsetMax = new Vector2(0, singleTestResult.offsetMin.y + 200f);

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

        resultComparerMaterial = resultImage.material;

        currentRT = new RenderTexture(1920, 1080, 0, RenderTextureFormat.ARGB32);

        // Load scenes and calculate the results.
        StartCoroutine(CalculateAllResults());
    }

    void SetResult(int _index, float _avgValue, float _maxValue)
    {
        avgResults[_index] = Mathf.RoundToInt(_avgValue * 100f);

        resultsAvgValue[_index].text = avgResults[_index].ToString() + "%";
        resultsAvgFill[_index].localScale = new Vector3(_avgValue, 1f, 1f);

        maxResults[_index] = Mathf.RoundToInt(_maxValue * 100f);

        resultsAvgValue[_index].text = maxResults[_index].ToString() + "%";
        resultsAvgFill[_index].localScale = new Vector3(_maxValue, 1f, 1f);
    }

    void CalculateOverall()
    {
        float overallAvgResult = 0f;
        float overallMaxResult = 0f;
        for (int i=0; i<numOfResults; ++i)
        {
            overallAvgResult += 1.0f*avgResults[i];
            overallMaxResult += 1.0f * maxResults[i];
        }
        overallAvgResult /= numOfResults;
        overallMaxResult /= numOfResults;

        overallAvgFill.localScale = new Vector3(overallAvgResult * 0.01f, 1f, 1f);
        overallAvgText.text = Mathf.RoundToInt(overallAvgResult).ToString() + "%";

        overallMaxFill.localScale = new Vector3(overallMaxResult * 0.01f, 1f, 1f);
        overallMaxText.text = Mathf.RoundToInt(overallMaxResult).ToString() + "%";
    }

    IEnumerator CalculateAllResults()
    {
        resultsPanel.SetActive(false);
        waitingPanel.SetActive(true);
        scenePanel.SetActive(false);

        for (int i=0; i<numOfResults;++i)
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(i + 1);

            yield return null;

            resultsLabels[i].text = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

            var testSetup = FindObjectOfType<UnityEngine.Experimental.Rendering.SetupSceneForRenderPipelineTest>();
            testSetup.Setup();

            for (int f = 0; f < frameWait; ++f)
            {
                yield return null;
            }

            Camera testCamera = testSetup.cameraToUse;
            var rtDesc = new RenderTextureDescriptor(
                             testSetup.width,
                             testSetup.height,
                             (testSetup.hdr && testCamera.allowHDR) ? RenderTextureFormat.ARGBHalf : RenderTextureFormat.ARGB32,
                             24);
            rtDesc.sRGB = false;
            rtDesc.msaaSamples = testSetup.msaaSamples;

            // render the scene
            var tempTarget = RenderTexture.GetTemporary(rtDesc);
            var oldTarget = testSetup.cameraToUse.targetTexture;
            testSetup.cameraToUse.targetTexture = tempTarget;
            testSetup.cameraToUse.Render();
            testSetup.cameraToUse.targetTexture = oldTarget;

            // Readback the rendered texture
            var oldActive = RenderTexture.active;
            RenderTexture.active = tempTarget;
            var captured = new Texture2D(tempTarget.width, tempTarget.height, TextureFormat.RGB24, false);
            captured.ReadPixels(new Rect(0, 0, testSetup.width, testSetup.height), 0, 0);
            RenderTexture.active = oldActive;

            // find the reference image
            var template = Resources.Load<Texture2D>(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name+".unity");

            // compare
            Vector2 compareResult = CompareTextures(template, captured);

            SetResult(i, 1f- compareResult.x, compareResult.y);

            yield return null;
        }

        CalculateOverall();

        UnityEngine.SceneManagement.SceneManager.LoadScene(0);

        resultsPanel.SetActive(true);
        waitingPanel.SetActive(false);
        scenePanel.SetActive(false);

        yield return null;
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
            resultComparerMaterial.SetFloat("_Split", Mathf.Clamp01(resultComparerMaterial.GetFloat("_Split") + Input.GetAxis("Horizontal") * Time.deltaTime * 0.3f));
            resultComparerMaterial.SetFloat("_ResultSplit", Mathf.Clamp01(resultComparerMaterial.GetFloat("_ResultSplit") + Input.GetAxis("Vertical") * Time.deltaTime * 0.3f));

            if (Input.GetButtonDown("Fire3")) resultComparerMaterial.SetInt("_Mode", (resultComparerMaterial.GetInt("_Mode") + 1) % 6);
        }
	}
}
