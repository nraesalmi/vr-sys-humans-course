using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Interactive : MonoBehaviour
{
    public InteractMode interactModeOverride = InteractMode.None;
    public float twistMultiplierOverride = 0f;
    public float dwellTimerOverride = 0f;
    public void Interact()
    {
        Debug.Log("Interacted with " + transform.name);
    }
}
