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
    }

    // Update is called once per frame
    void Update()
    {

    }
}
