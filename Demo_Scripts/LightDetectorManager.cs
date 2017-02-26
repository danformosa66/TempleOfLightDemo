using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class LightDetectorManager : MonoBehaviour {

    private Camera myCamera;
    private Texture2D temp = null;
    private Rect normalizedReadRegion;

    public Transform objectInQuestion;

    // Use this for initialization
    void Awake ()
    {
        myCamera = GetComponent<Camera>();
    }

    public void Initialize(float planeWidth, float planeHeight)
    {
        // [TODO]: Feed this to the little lie detectors :P

        // This would be calculated in the lie detectors
        Vector2 normalizedCoords = new Vector2(
            objectInQuestion.position.x.RetargetedTo_01(-planeWidth / 2, planeWidth / 2),
            objectInQuestion.position.z.RetargetedTo_01(planeHeight / 2, -planeHeight / 2));

        Vector2 normalizedScale = objectInQuestion.lossyScale;
        normalizedScale.x /= planeWidth;
        normalizedScale.y /= planeHeight;

        AddReadRegion(normalizedCoords, normalizedScale);
    }

    /// <summary>
    /// Takes a [0, 1] rect and adds it to the color look-up table
    /// </summary>
    public void AddReadRegion(Vector2 normalizedCoords, Vector2 normalizedScale)
    {
        // [TODO]: Create a Dictionary of {LieDetector, Rect} (read region per detector)
        normalizedReadRegion.x = normalizedCoords.x;
        normalizedReadRegion.y = normalizedCoords.y;
        normalizedReadRegion.width = normalizedScale.x;
        normalizedReadRegion.height = normalizedScale.y;

        Debug.Log(normalizedReadRegion);
    }
	
	// Update is called once per frame
	void Update ()
    {
        CopyFromMainCamera();

        ProcessRenderTexture(myCamera.targetTexture);
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

        Rect readRegion = new Rect(
            normalizedReadRegion.x * rT.width,
            normalizedReadRegion.y * rT.height,
            normalizedReadRegion.width * rT.width,
            normalizedReadRegion.height * rT.height);
        
        // Debug.Log(readRegion);

        temp.ReadPixels(readRegion, 0, 0);

        // Read texture to array
        // This shit was wrong.. we write to temp from a read region to its 0, 0
        // So we should read it from its 0, 0 not from the readreagion's x and y :P
        Color[] framebuffer = temp.GetPixels(0, 0,
            (int) readRegion.width, (int) readRegion.height);

        this.Log(GetAverage(framebuffer).ToString());
        // this.Log(framebuffer.ToReadableString());
        // Debug.Log("FrameBuffer:\n" + framebuffer.ToReadableString());
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
}
