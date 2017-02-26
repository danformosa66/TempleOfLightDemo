using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightDetector : MonoBehaviour {

    public EventHandler<EventArgs<ProgressReport>> onProgressUpdate;

    public Rect normalizedReadRegion { get; private set; }

    [SerializeField]
    private Color targetColor = Color.white;

    private float currentProgress = 0;

    public bool isDone = false;

    internal void Initialize(float planeWidth, float planeHeight)
    {
        Vector2 normalizedCoords = new Vector2(
            transform.position.x.RetargetedTo_01(-planeWidth / 2, planeWidth / 2),
            transform.position.z.RetargetedTo_01(planeHeight / 2, -planeHeight / 2));

        Vector2 normalizedScale = new Vector2(
            transform.lossyScale.x / planeWidth,
            transform.lossyScale.z / planeHeight);

        isDone = false;

        UpdateReadRegion(normalizedCoords, normalizedScale);
    }

    /// <summary>
    /// Takes a [0, 1] rect and adds it to the color look-up table
    /// </summary>
    public void UpdateReadRegion(Vector2 normalizedCoords, Vector2 normalizedScale)
    {
        // [TODO]: Create a Dictionary of {LieDetector, Rect} (read region per detector)
        normalizedReadRegion = new Rect(
            normalizedCoords.x, normalizedCoords.y,
            normalizedScale.x, normalizedScale.y);

        this.Log("Updated Read Region to: " + normalizedReadRegion);
    }

    internal void UpdateCurrentColor(Color currentColor)
    {

        float progress = 1 - CalculateDifference_Percentile(currentColor, targetColor);

        if (onProgressUpdate != null && Mathf.Abs(currentProgress - progress) > 0.001f)
        {
            this.Log("Current Color: " + currentColor.ToHex());
            onProgressUpdate(this, new ProgressReport(progress));
        }

        currentProgress = progress;
    }

    private static float CalculateDifference_Percentile(Color colorA, Color colorB)
    {
        float difference = 0;

        difference += Mathf.Abs(colorA.r - colorB.r);
        difference += Mathf.Abs(colorA.g - colorB.g);
        difference += Mathf.Abs(colorA.b - colorB.b);

        return difference / 3;        
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
