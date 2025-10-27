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
    public GameObject startUI;
    public TMP_InputField usernameInput;
    public TMP_Text errorText;
    
    [Header("Dependencies")]
    public InteractableObjects interactableObjects;
    public GameObject loadingCircle;
    public GameObject darkOverlay;

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

        UIFader.ShowImmediate(startUI);
        UIFader.HideImmediate(loadingCircle);
        UIFader.ShowImmediate(darkOverlay);
        UIFader.HideImmediate(errorText.gameObject);

        bridge.onConnected.AddListener(OnConnected);
        bridge.onConnectionError.AddListener(OnConnectionError);
        
        HideError();
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

    private void SetButtonEnabled(bool enabled)
    {
        if (interactableObjects != null)
        {
            interactableObjects.SetInteractableEnabledByAction(InteractableObjects.ActionType.JoinWorld, enabled);
        }
    }

    private IEnumerator InitializeWorlds()
    {
        SetButtonEnabled(false);
        
        // Check for local world first
        var task = ReplicationServerUtils.PingHttpServerAsync(RuntimeSettings.Instance.LocalHost,
            RuntimeSettings.Instance.WorldsAPIPort);
        yield return new WaitUntil(() => task.IsCompleted);

        if (task.Result)
        {
            localWorld = WorldData.GetLocalWorld(RuntimeSettings.Instance.LocalHost);
            if (localWorld.WorldId != default(WorldData).WorldId)
            {
                SetButtonEnabled(true);
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
                    SetButtonEnabled(true);
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

    public void OnStartClicked()
    {
        if (isJoining) return;
        UIFader.HideImmediate(startUI);
        SetButtonEnabled(false);
        UIFader.ShowImmediate(loadingCircle);
        StartCoroutine(JoinWorldAsync());
    }
    
    private IEnumerator JoinWorldAsync()
    {
        HideError();
        isJoining = true;

        // Capture username from input field
        if (usernameInput != null && !string.IsNullOrEmpty(usernameInput.text))
        {
            PlayerUsername = usernameInput.text;
        }

        // Wait one frame to let UI update
        yield return null;

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
            UIFader.HideImmediate(loadingCircle);
            UIFader.ShowImmediate(startUI);
            SetButtonEnabled(true);
            isJoining = false;
        }
    }

    private void OnConnected(CoherenceBridge _)
    {
        Debug.Log("Connected! Starting fade out...");
        OnGameJoined?.Invoke();
        SetButtonEnabled(false);
        
        UIFader.FadeOut(loadingCircle, 0.3f);
        UIFader.FadeOut(darkOverlay, 0.3f);
    }
    
    private void OnConnectionError(CoherenceBridge _, ConnectionException exception)
    {
        var (title, message) = exception.GetPrettyMessage();
        UIFader.ShowImmediate(startUI);
        ShowError($"{title}: {message}");
        UIFader.HideImmediate(loadingCircle);
        
        SetButtonEnabled(true);
        isJoining = false;
    }

    private void ShowError(string message)
    {
        errorText.text = message;
        UIFader.FadeIn(errorText.gameObject);
    }

    private void HideError()
    {
        errorText.text = "";
        UIFader.FadeOut(errorText.gameObject);
    }
}

