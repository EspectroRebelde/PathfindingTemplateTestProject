using UnityEngine;

/// <summary>
/// Sets the targetted frame rate to the chosen one.
/// </summary>
public class FPSLimit : MonoBehaviour
{
    public int targetFrameRate = 60;
    
    void Awake()
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = targetFrameRate;
    }
}
