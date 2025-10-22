using System.Collections;
using System.Collections.Generic;
using Coherence;
using Coherence.Cloud;
using Coherence.Connection;
using Coherence.Toolkit;
using Coherence.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;

public class SimpleWorldJoin : MonoBehaviour
{
    [Header("UI References")]
    public CanvasGroup uiCanvasGroup;
    public Button startButton;
    public TMP_InputField usernameInput;
    public TMP_Text errorText;
    
    [Header("Settings")]
    public float fadeOutDuration = 0.5f;

    private CoherenceBridge bridge;
    private CoherenceCloudLogin cloudLogin;
    private IReadOnlyList<WorldData> availableWorlds = new List<WorldData>();
    private WorldData localWorld;
    private bool isJoining;

    public static event Action OnGameJoined;
    public static string PlayerUsername { get; private set; } = "Player";

    private PlayerAccount PlayerAccount => cloudLogin ? cloudLogin.PlayerAccount : null;
    private WorldsService CloudWorlds => PlayerAccount?.Services?.Worlds;

    private void OnEnable()
    {
        if (!CoherenceBridgeStore.TryGetBridge(gameObject.scene, out bridge))
        {
            ShowError("CoherenceBridge required on the scene.");
            return;
        }

        if (uiCanvasGroup)
        {
            uiCanvasGroup.alpha = 1f;
        }

        bridge.onConnected.AddListener(OnConnected);
        bridge.onConnectionError.AddListener(OnConnectionError);
        
        HideError();
        startButton.onClick.AddListener(OnStartClicked);
        
        StartCoroutine(InitializeWorlds());
    }

    private void OnDisable()
    {
        if (bridge)
        {
            bridge.onConnected.RemoveListener(OnConnected);
            bridge.onConnectionError.RemoveListener(OnConnectionError);
        }
    }

    private IEnumerator InitializeWorlds()
    {
        startButton.interactable = false;
        
        // Check for local world first
        var task = ReplicationServerUtils.PingHttpServerAsync(RuntimeSettings.Instance.LocalHost,
            RuntimeSettings.Instance.WorldsAPIPort);
        yield return new WaitUntil(() => task.IsCompleted);

        if (task.Result)
        {
            localWorld = WorldData.GetLocalWorld(RuntimeSettings.Instance.LocalHost);
            if (localWorld.WorldId != default(WorldData).WorldId)
            {
                startButton.interactable = true;
                yield break;
            }
        }

        // If no local world, try cloud
        if (!string.IsNullOrEmpty(RuntimeSettings.Instance.ProjectID))
        {
            LogInToCloud();
            
            while (CloudWorlds is not { IsLoggedIn : true })
            {
                yield return null;
            }

            yield return StartCoroutine(FetchCloudWorlds());
        }
        else
        {
            ShowError("No local world found and no project ID configured.");
        }
    }

    private void LogInToCloud()
    {
        if (!cloudLogin && !TryGetComponent(out cloudLogin) && !(cloudLogin = FindAnyObjectByType<CoherenceCloudLogin>()))
        {
            cloudLogin = gameObject.AddComponent<CoherenceCloudLogin>();
        }

        cloudLogin.LogInAsync().OnFail(error =>
        {
            ShowError($"Login failed: {error.Type}");
        });
    }

    private IEnumerator FetchCloudWorlds()
    {
        bool fetchComplete = false;
        
        CloudWorlds.FetchWorlds(response =>
        {
            if (response.Status == RequestStatus.Success)
            {
                availableWorlds = response.Result;
                if (availableWorlds.Count > 0)
                {
                    startButton.interactable = true;
                }
                else
                {
                    ShowError("No worlds available.");
                }
            }
            else
            {
                ShowError("Failed to fetch worlds.");
            }
            fetchComplete = true;
        });

        yield return new WaitUntil(() => fetchComplete);
    }

    private void OnStartClicked()
    {
        if (isJoining) return;
        
        HideError();
        isJoining = true;
        startButton.interactable = false;

        // Capture username from input field
        if (usernameInput != null && !string.IsNullOrEmpty(usernameInput.text))
        {
            PlayerUsername = usernameInput.text;
        }

        WorldData worldToJoin = default;

        // Prefer local world, then first available cloud world
        if (localWorld.WorldId != default(WorldData).WorldId)
        {
            worldToJoin = localWorld;
        }
        else if (availableWorlds.Count > 0)
        {
            worldToJoin = availableWorlds[0];
        }

        if (worldToJoin.WorldId != default(WorldData).WorldId)
        {
            bridge.JoinWorld(worldToJoin);
        }
        else
        {
            ShowError("No world available to join.");
            startButton.interactable = true;
            isJoining = false;
        }
    }

    private void OnConnected(CoherenceBridge _)
    {
        Debug.Log("Connected! Starting fade out...");
        OnGameJoined?.Invoke();
        StartCoroutine(FadeOutOverlay());
    }
    
    private IEnumerator FadeOutOverlay()
    {
        if (!uiCanvasGroup)
        {
            Debug.LogWarning("No CanvasGroup assigned, disabling immediately");
            gameObject.SetActive(false);
            yield break;
        }
        
        startButton.interactable = false;
        float elapsed = 0f;
        
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            uiCanvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeOutDuration);
            yield return null;
        }
        
        uiCanvasGroup.alpha = 0f;
        gameObject.SetActive(false);
        Debug.Log("Fade out complete!");
    }

    private void OnConnectionError(CoherenceBridge _, ConnectionException exception)
    {
        var (title, message) = exception.GetPrettyMessage();
        ShowError($"{title}: {message}");
        startButton.interactable = true;
        isJoining = false;
    }

    private void ShowError(string message)
    {
        errorText.text = message;
        errorText.gameObject.SetActive(true);
    }

    private void HideError()
    {
        errorText.text = "";
        errorText.gameObject.SetActive(false);
    }
}

