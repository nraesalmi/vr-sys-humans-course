using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum InteractMode
{
    None = 0,
    Dwell,
    Twist
}

public class CameraInteract : MonoBehaviour
{
    [SerializeField] InteractMode defaultInteractionMode = InteractMode.Twist;
    [SerializeField] float defaultDwellTimer = 1.5f;
    [SerializeField] float defaultTwistMultiplier = 6f;
    [SerializeField] float maxInteractDistance = 30f;
    [SerializeField] LayerMask interactLayer = ~0;
    [SerializeField] GameObject dwellCircle;
    [SerializeField] GameObject twistCrosshair;
    [SerializeField] GameObject reticle;
    [SerializeField] Material otherCrosshair;
    public static Interactive selected = null;
    InteractMode lastMode = InteractMode.Twist;
    public static float dwellTimer = 1.5f;
    public static float twistMultiplier = 6f;
    float timer = 0;
    float startAngle = 0;
    int twistVisual = 0;
    bool rollReset = true;

    void Update()
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position, transform.forward, out hit, maxInteractDistance, interactLayer) && hit.transform.GetComponent<Interactive>())
        {
            if (!selected || hit.transform.gameObject != selected.gameObject)
            {
                selected = hit.transform.GetComponent<Interactive>();
                lastMode = selected.interactModeOverride;
                dwellTimer = selected.dwellTimerOverride;
                twistMultiplier = selected.twistMultiplierOverride;
                if(lastMode == InteractMode.None)
                    lastMode = defaultInteractionMode;
                if(dwellTimer <= 0)
                    dwellTimer = defaultDwellTimer;
                if(twistMultiplier <= 0)
                    twistMultiplier = defaultTwistMultiplier;
                timer = Time.time;
                startAngle = transform.rotation.eulerAngles.z > 180 ? transform.rotation.eulerAngles.z - 360 : transform.rotation.eulerAngles.z;
            }
        }
        else if (selected)
        {
            selected = null;
        }

        reticle.SetActive(selected && lastMode == InteractMode.Dwell);

        dwellCircle.SetActive(selected && lastMode == InteractMode.Dwell && Time.time - timer < dwellTimer);
        if(selected && lastMode == InteractMode.Dwell)
        {
            if(Time.time-timer > dwellTimer && timer > 0)
            {
                selected.SendMessage("Interact");
                timer = 0;
            }
            dwellCircle.GetComponent<Renderer>().sharedMaterial.SetFloat("_Cutoff", Mathf.Clamp(1.3f - (Time.time - timer) / dwellTimer * 1.3f, 0.0f, 1.0f));
        }

        dwellCircle.GetComponent<Renderer>().sharedMaterial.SetTextureOffset("_MainTex", Vector2.up * 0.27f);
        dwellCircle.GetComponent<Renderer>().sharedMaterial.SetTextureScale("_MainTex", Vector2.one);

        twistCrosshair.SetActive(selected && lastMode == InteractMode.Twist && twistVisual == 0);
        if(lastMode == InteractMode.Twist)
        {
            Quaternion straightRot = Quaternion.LookRotation(transform.forward, Vector3.left);
            Vector3 crosshairEuler = new Vector3(0, -90, 90);
            crosshairEuler.x = transform.rotation.eulerAngles.z;
            crosshairEuler.x = crosshairEuler.x > 180 ? crosshairEuler.x-360 : crosshairEuler.x;
            float angdiff = (crosshairEuler.x-startAngle)*twistMultiplier;
            crosshairEuler.x = Mathf.Clamp(angdiff, -45f, 45f);

            otherCrosshair.color = rollReset ? Color.green : Color.red;

            if(angdiff < 0 && selected)
            {
                dwellCircle.GetComponent<Renderer>().sharedMaterial.SetTextureOffset("_MainTex", Vector2.up * 0.72f);
                dwellCircle.GetComponent<Renderer>().sharedMaterial.SetTextureScale("_MainTex", new Vector2(1, -1));
            }

            if (Mathf.Abs(angdiff) > 5f && selected)
            {
                dwellCircle.SetActive(twistVisual == 1 && rollReset);
                dwellCircle.GetComponent<Renderer>().sharedMaterial.SetFloat("_Cutoff", 1f - (Mathf.Abs(angdiff)-5) / 40f);
            }

            twistCrosshair.transform.GetChild(0).localRotation = Quaternion.Euler(crosshairEuler);
            if ((angdiff <= -45f || angdiff >= 45f) && rollReset && selected)
            {
                selected.SendMessage("Interact");
                timer = 0;
                rollReset = false;
            }
            if (angdiff > -30f && angdiff < 30f)
                rollReset = true;
        }
    }
}
