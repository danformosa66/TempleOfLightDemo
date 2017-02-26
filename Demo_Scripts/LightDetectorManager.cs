using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class LightDetectorManager : MonoBehaviour {

    public EventHandler<EventArgs<ProgressReport>> onProgressUpdate;

    private Camera myCamera;
    private Texture2D temp = null;

    public Transform objectInQuestion;

    [SerializeField]
    private float checkEverySeconds = 1;

    private List<LightDetector> lightDetectors;

    private float threshold = .75f;
    private float currentProgress = 0;

    private bool simulateInput = false;


    // Use this for initialization
    void Awake ()
    {
        myCamera = GetComponent<Camera>();

        // [PCG]: These would be generated
        lightDetectors = The_Helper.GetComponentsInScene<LightDetector>().ToList();
    }
    
    public void Initialize(float planeWidth, float planeLength)
    {
        // [TODO]: Feed this to the little lie detectors :P
        foreach (LightDetector lD in lightDetectors)
        {
            lD.Initialize(planeWidth, planeLength);
            lD.onProgressUpdate += LD_OnProgressUpdate;
        }

        StopAllCoroutines();
        StartCoroutine(CheckRenderTexture());
    }

    private IEnumerator CheckRenderTexture()
    {
        while (true)
        {
            while (simulateInput)
                yield return null;
            
            CopyFromMainCamera();

            ProcessRenderTexture(myCamera.targetTexture);

            yield return new WaitForSeconds(checkEverySeconds);
        }
    }

    private void LD_OnProgressUpdate(object sender, EventArgs<LightDetector.ProgressReport> e)
    {
        LightDetector lD = (LightDetector)sender;
        if (!lD) return;

        this.Log("Received pogress update from " + lD.name + ": " + e.value.percentileComplete.PercentileToPercent());

        // Not done => Done
        if (!lD.isDone && e.value.percentileComplete > threshold)
        {
            lD.isDone = true;
            currentProgress += 1f / lightDetectors.Count;
            currentProgress = currentProgress.Round(.001f);
            if (onProgressUpdate != null)
                onProgressUpdate(this, new ProgressReport(currentProgress));
        }
        // Done => Not Done
        else if (lD.isDone && e.value.percentileComplete < threshold)
        {
            lD.isDone = false;
            currentProgress -= 1f / lightDetectors.Count;
            currentProgress = currentProgress.Round(.001f);
            if (onProgressUpdate != null)
                onProgressUpdate(this, new ProgressReport(currentProgress));
        }
    }

	
	// Update is called once per frame
	void Update ()
    {
        if (Input.GetKey(KeyCode.Tab) && Input.GetKeyUp(KeyCode.D))
            simulateInput = !simulateInput;

        if (simulateInput)
            CheckForDebugInputs();
	}

    private void CheckForDebugInputs()
    {
        if (Input.GetKeyUp(KeyCode.Alpha1) && lightDetectors.Count > 0)
            lightDetectors[0].UpdateCurrentColor(The_Helper.GetRandomColor());
        else if (Input.GetKeyUp(KeyCode.Alpha2) && lightDetectors.Count > 1)
            lightDetectors[1].UpdateCurrentColor(The_Helper.GetRandomColor());
        else if (Input.GetKeyUp(KeyCode.Alpha3) && lightDetectors.Count > 2)
            lightDetectors[2].UpdateCurrentColor(The_Helper.GetRandomColor());
    }

    private void CopyFromMainCamera()
    {
        myCamera.transform.position = Camera.main.transform.position;
        myCamera.transform.rotation = Camera.main.transform.rotation;

        myCamera.orthographicSize = Camera.main.orthographicSize;
    }

    void ProcessRenderTexture (RenderTexture rT)
    {
        // Setup a camera, texture and render texture
        if (temp == null ||
            (temp.width != rT.width || temp.height != rT.height))
            temp = new Texture2D(rT.width, rT.height);
        
        // Read pixels to texture
        RenderTexture.active = rT;

        foreach(LightDetector lD in lightDetectors)
        {
            // lD.Log(lD.normalizedReadRegion.ToString());

            Rect readRegion = new Rect(
                lD.normalizedReadRegion.x * rT.width,
                lD.normalizedReadRegion.y * rT.height,
                lD.normalizedReadRegion.width * rT.width,
                lD.normalizedReadRegion.height * rT.height);

            // Debug.Log(readRegion);

            temp.ReadPixels(readRegion, 0, 0);

            // Read texture to array
            // This shit was wrong.. we write to temp from a read region to its 0, 0
            // So we should read it from its 0, 0 not from the readreagion's x and y :P
            Color[] framebuffer = temp.GetPixels(0, 0,
                (int)readRegion.width, (int)readRegion.height);

            Color currentColor = GetAverage(framebuffer);
            lD.UpdateCurrentColor(currentColor);
            // this.Log(framebuffer.ToReadableString());
            // Debug.Log("FrameBuffer:\n" + framebuffer.ToReadableString());
        }
    }

    Color GetAverage(IList<Color> colorList)
    {
        Vector4 v4 = Vector4.zero;

        foreach (Color c in colorList)
        {
            v4.x += c.r;
            v4.y += c.g;
            v4.z += c.b;
            v4.w += c.a;
        }

        v4 /= colorList.Count;

        return new Color(v4.x, v4.y, v4.z, v4.w);
    }

    public struct ProgressReport
    {
        public ProgressReport(float percentileComplete) { this.percentileComplete = percentileComplete; }

        /// <summary>
        /// [0, 1]
        /// </summary>
        public float percentileComplete;
    }
}
