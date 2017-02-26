using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{

    public Transform plane;
    public LightDetectorManager lDM;

    // Use this for initialization
    void Start()
    {
        lDM.Initialize(plane.lossyScale.x, plane.lossyScale.z);
        lDM.onProgressUpdate += LDM_OnProgressUpdate;
    }

    private void LDM_OnProgressUpdate(object sender, EventArgs<LightDetectorManager.ProgressReport> e)
    {
        LightDetectorManager lDM = (LightDetectorManager)sender;
        if (!lDM) return;

        float progress = e.value.percentileComplete;

        this.Log("Received pogress update from " + lDM.name + ": " + progress.PercentileToPercent());

        if (progress == 1)
        {
            // [TODO]: Show post-level results, 3 buttons, 1 for next level/concept, 1 for replaying the same concept i.e. 
            // keeping the same paramters and 1 for going back to main menu.
            this.Log("Congratulations, level passed!");
        }
    }

    // Update is called once per frame
    void Update()
    {

    }
}
