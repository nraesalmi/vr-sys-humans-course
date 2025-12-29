using UnityEngine;
using Photon.Pun;
using System.Collections.Generic;
using TMPro;

public class GameIntroManager : MonoBehaviourPunCallbacks
{
    [Header("Intro Settings")]
    [SerializeField] private List<IntroTarget> introTargets = new List<IntroTarget>();
    [SerializeField] private bool requireAllTargetsDestroyed = true;
    [SerializeField] private float delayAfterIntro = 3f;
    
    [Header("References")]
    [SerializeField] private EnemySpawner enemySpawner;
    [SerializeField] private GameObject introUI;
    [SerializeField] private TMP_Text introText;
    [SerializeField] private TMP_Text targetsRemainingText;
    
    [Header("TextMeshPro Settings")]
    [SerializeField] private Color introTextColor = Color.white;
    [SerializeField] private Color completeTextColor = Color.green;
    [SerializeField] private Color targetsTextColor = Color.yellow;
    
    private bool introComplete = false;
    private int targetsDestroyed = 0;
    private int totalTargets = 0;
    
    // Store original text values
    private string originalIntroText = "";
    private string originalTargetsText = "";
    
    private void Start()
    {
        // Find all targets if not assigned
        if (introTargets.Count == 0)
        {
            IntroTarget[] foundTargets = FindObjectsByType<IntroTarget>(FindObjectsSortMode.None);
            introTargets.AddRange(foundTargets);
        }
        
        totalTargets = introTargets.Count;
        
        // Disable enemy spawner initially
        if (enemySpawner != null)
        {
            enemySpawner.enabled = false;
        }
        
        // Store original text from TextMeshPro components
        StoreOriginalText();
        
        // Show intro UI
        if (introUI != null)
        {
            introUI.SetActive(true);
        }
        
        UpdateUI();
        
        // Only master client controls game start in multiplayer
        if (PhotonNetwork.IsConnected && !PhotonNetwork.IsMasterClient)
        {
            // Non-master clients just observe
            return;
        }
    }
    
    private void StoreOriginalText()
    {
        // Store original text from TextMeshPro components if they exist
        if (introText != null)
        {
            originalIntroText = introText.text;
        }
        
        if (targetsRemainingText != null)
        {
            originalTargetsText = targetsRemainingText.text;
        }
    }
    
    public void RegisterTarget(IntroTarget target)
    {
        if (!introTargets.Contains(target))
        {
            introTargets.Add(target);
            totalTargets = introTargets.Count;
            UpdateUI();
        }
    }
    
    public void OnTargetDestroyed(IntroTarget target)
    {
        targetsDestroyed++;
        
        if (PhotonNetwork.IsConnected)
        {
            photonView.RPC("RPC_UpdateTargetCount", RpcTarget.All, targetsDestroyed);
        }
        
        UpdateUI();
        
        // Check if intro is complete
        if (!introComplete && CheckIntroComplete())
        {
            CompleteIntro();
        }
    }
    
    [PunRPC]
    private void RPC_UpdateTargetCount(int destroyedCount)
    {
        targetsDestroyed = destroyedCount;
        UpdateUI();
        
        // Check if intro is complete (all clients should check)
        if (!introComplete && CheckIntroComplete())
        {
            CompleteIntro();
        }
    }
    
    private bool CheckIntroComplete()
    {
        if (requireAllTargetsDestroyed)
        {
            return targetsDestroyed >= totalTargets;
        }
        else
        {
            // Option: require at least one target destroyed
            return targetsDestroyed > 0;
        }
    }
    
    private void CompleteIntro()
    {
        introComplete = true;
        
        if (introText != null)
        {
            // Don't change the text, just update the color
            introText.color = completeTextColor;
        }
        
        // Start countdown to begin waves
        StartCoroutine(StartGameAfterDelay());
        
        // Sync with other players
        if (PhotonNetwork.IsConnected)
        {
            photonView.RPC("RPC_CompleteIntro", RpcTarget.All);
        }
    }
    
    [PunRPC]
    private void RPC_CompleteIntro()
    {
        introComplete = true;
        
        if (introText != null)
        {
            introText.color = completeTextColor;
        }
    }
    
    private System.Collections.IEnumerator StartGameAfterDelay()
    {
        // Wait without modifying text
        yield return new WaitForSeconds(delayAfterIntro);
        
        StartGame();
    }
    
    private void StartGame()
    {
        // Enable enemy spawner
        if (enemySpawner != null)
        {
            enemySpawner.enabled = true;
            enemySpawner.EnableSpawning();
        }
        
        // Hide intro UI
        if (introUI != null)
        {
            StartCoroutine(FadeOutUI());
        }
        
        // Notify all players
        if (PhotonNetwork.IsConnected)
        {
            photonView.RPC("RPC_StartGame", RpcTarget.All);
        }
    }
    
    [PunRPC]
    private void RPC_StartGame()
    {
        if (enemySpawner != null && !enemySpawner.enabled)
        {
            enemySpawner.enabled = true;
        }
        
        if (introUI != null)
        {
            StartCoroutine(FadeOutUI());
        }
    }
    
    private System.Collections.IEnumerator FadeOutUI()
    {
        CanvasGroup canvasGroup = introUI.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = introUI.AddComponent<CanvasGroup>();
        }
        
        float fadeDuration = 1f;
        float elapsedTime = 0f;
        
        while (elapsedTime < fadeDuration)
        {
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsedTime / fadeDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        introUI.SetActive(false);
    }
    
    private void UpdateUI()
    {
        // Don't modify intro text - keep whatever is set in the editor
        if (introText != null && !introComplete)
        {
            introText.color = introTextColor;
        }
        
        // Only update targets remaining text with dynamic values
        if (targetsRemainingText != null)
        {
            if (requireAllTargetsDestroyed)
            {
                // Preserve original text formatting and only update numbers
                UpdateTargetsTextWithDynamicValues();
                
                // Optional: Add visual feedback when targets are destroyed
                if (targetsDestroyed > 0)
                {
                    StartCoroutine(FlashTargetsText());
                }
            }
            else
            {
                // For non-requireAll mode, keep original text
                targetsRemainingText.text = originalTargetsText;
            }
        }
    }
    
    private void UpdateTargetsTextWithDynamicValues()
    {
        if (string.IsNullOrEmpty(originalTargetsText))
        {
            // If no original text, use a default format
            targetsRemainingText.text = $"TARGETS: {targetsDestroyed}/{totalTargets}";
        }
        else
        {
            // Replace placeholders in original text with dynamic values
            string updatedText = originalTargetsText
                .Replace("{destroyed}", targetsDestroyed.ToString())
                .Replace("{total}", totalTargets.ToString())
                .Replace("{current}", targetsDestroyed.ToString())
                .Replace("{max}", totalTargets.ToString());
            
            // Also handle numbered placeholders for more flexibility
            updatedText = updatedText
                .Replace("{0}", targetsDestroyed.ToString())
                .Replace("{1}", totalTargets.ToString());
            
            targetsRemainingText.text = updatedText;
        }
    }
    
    private System.Collections.IEnumerator FlashTargetsText()
    {
        if (targetsRemainingText == null) yield break;
        
        Color originalColor = targetsRemainingText.color;
        targetsRemainingText.color = Color.white;
        
        yield return new WaitForSeconds(0.1f);
        
        if (targetsRemainingText != null)
        {
            targetsRemainingText.color = originalColor;
        }
    }
    
    public bool IsIntroComplete()
    {
        return introComplete;
    }
    
    // For debugging/cheat codes
    public void SkipIntro()
    {
        if (!introComplete)
        {
            CompleteIntro();
        }
    }
    
    // Reset to original text (useful for restarting game)
    public void ResetToOriginalText()
    {
        if (introText != null)
        {
            introText.text = originalIntroText;
            introText.color = introTextColor;
        }
        
        if (targetsRemainingText != null)
        {
            targetsRemainingText.text = originalTargetsText;
            targetsRemainingText.color = targetsTextColor;
        }
        
        introComplete = false;
        targetsDestroyed = 0;
        UpdateUI();
    }
}