using UnityEngine;
using MoreMountains.Feedbacks;
using MoreMountains.FeedbacksForThirdParty;
using System.Collections.Generic;

public class FeedbackManager : MonoBehaviour
{
    [System.Serializable]
    public struct MaterialGlowState
    {
        public float scale;
        public float intensity;
        public Color color;
    }

    [SerializeField] private MMF_Player balloonInflateFeedback;
    [SerializeField] private MMF_Player uiSound;
    [SerializeField] private MMF_Player musicFeedback;
    [SerializeField] private MMF_Player successSoundFeedback;
    [SerializeField] private MMF_Player errorSoundFeedback;
    [SerializeField] private MMF_Player cashFountainFeedback;

    public float uiBaseVolume = 0.2f;
    public float uiBasePitch = 1f;
    
    private SpringManager springManager;
    private MaterialManager materialManager;
    
    // Scale springs
    private readonly Dictionary<Transform, Vector3> _baseScales = new Dictionary<Transform, Vector3>();
    private readonly Dictionary<Transform, string> _scaleSpringKeys = new Dictionary<Transform, string>();
    
    // Material glow springs (per object, stores spring keys)
    private readonly Dictionary<GameObject, string> _glowScaleSpringKeys = new Dictionary<GameObject, string>();
    private readonly Dictionary<GameObject, string> _glowIntensitySpringKeys = new Dictionary<GameObject, string>();
    private readonly Dictionary<GameObject, string> _glowColorRSpringKeys = new Dictionary<GameObject, string>();
    private readonly Dictionary<GameObject, string> _glowColorGSpringKeys = new Dictionary<GameObject, string>();
    private readonly Dictionary<GameObject, string> _glowColorBSpringKeys = new Dictionary<GameObject, string>();
    
    private int springCounter = 0;

    private void Awake()
    {
        springManager = FindObjectOfType<SpringManager>();
        materialManager = FindObjectOfType<MaterialManager>();
    }

    private void Start()
    {
        balloonInflateFeedback?.Initialization();
        uiSound?.Initialization();
        musicFeedback?.Initialization();
        successSoundFeedback?.Initialization();
        errorSoundFeedback?.Initialization();
        musicFeedback?.PlayFeedbacks();
    }
    
    private Vector3 GetBaseScale(Transform target)
    {
        if (!_baseScales.TryGetValue(target, out Vector3 baseScale))
        {
            baseScale = target.localScale;
            _baseScales[target] = baseScale;
        }
        return baseScale;
    }

    private string GetOrCreateScaleSpringKey(Transform target)
    {
        if (!_scaleSpringKeys.TryGetValue(target, out string key))
        {
            key = $"scale_{springCounter++}_{target.GetInstanceID()}";
            _scaleSpringKeys[target] = key;
            springManager.GetSpring(key, damping: 0.3f, frequency: 5f, initialValue: 1f);
        }
        return key;
    }

    private void GetOrCreateGlowSpringKeys(GameObject obj, MaterialGlowState initialState)
    {
        if (!_glowScaleSpringKeys.ContainsKey(obj))
        {
            int id = obj.GetInstanceID();
            _glowScaleSpringKeys[obj] = $"glow_scale_{springCounter++}_{id}";
            _glowIntensitySpringKeys[obj] = $"glow_intensity_{springCounter++}_{id}";
            _glowColorRSpringKeys[obj] = $"glow_r_{springCounter++}_{id}";
            _glowColorGSpringKeys[obj] = $"glow_g_{springCounter++}_{id}";
            _glowColorBSpringKeys[obj] = $"glow_b_{springCounter++}_{id}";
            
            // Initialize springs with initial state and set them instantly
            var scaleSpring = springManager.GetSpring(_glowScaleSpringKeys[obj], damping: 0.3f, frequency: 5f, initialValue: initialState.scale);
            var intensitySpring = springManager.GetSpring(_glowIntensitySpringKeys[obj], damping: 0.3f, frequency: 5f, initialValue: initialState.intensity);
            var rSpring = springManager.GetSpring(_glowColorRSpringKeys[obj], damping: 0.3f, frequency: 5f, initialValue: initialState.color.r);
            var gSpring = springManager.GetSpring(_glowColorGSpringKeys[obj], damping: 0.3f, frequency: 5f, initialValue: initialState.color.g);
            var bSpring = springManager.GetSpring(_glowColorBSpringKeys[obj], damping: 0.3f, frequency: 5f, initialValue: initialState.color.b);
            
            // Set to initial state instantly to avoid flickering
            scaleSpring.MoveToInstant(initialState.scale);
            intensitySpring.MoveToInstant(initialState.intensity);
            rSpring.MoveToInstant(initialState.color.r);
            gSpring.MoveToInstant(initialState.color.g);
            bSpring.MoveToInstant(initialState.color.b);
        }
    }

    private void Update()
    {
        if (springManager == null) return;
        
        // Apply spring values to transform scales
        foreach (var kvp in _scaleSpringKeys)
        {
            Transform target = kvp.Key;
            string springKey = kvp.Value;
            
            if (target == null) continue;
            
            float springValue = springManager.GetValue(springKey);
            Vector3 baseScale = GetBaseScale(target);
            target.localScale = baseScale * springValue;
        }
        
        // Apply spring values to material glow properties
        if (materialManager != null)
        {
            foreach (var kvp in _glowScaleSpringKeys)
            {
                GameObject obj = kvp.Key;
                if (obj == null) continue;
                
                float scale = springManager.GetValue(_glowScaleSpringKeys[obj]);
                float intensity = springManager.GetValue(_glowIntensitySpringKeys[obj]);
                float r = springManager.GetValue(_glowColorRSpringKeys[obj]);
                float g = springManager.GetValue(_glowColorGSpringKeys[obj]);
                float b = springManager.GetValue(_glowColorBSpringKeys[obj]);
                
                materialManager.SetGlowMaterial(
                    obj,
                    scale: scale,
                    glowColor: new Color(r, g, b, 1f),
                    glowIntensity: intensity
                );
            }
        }
    }

    public void PlayBalloonInflate()
    {
        balloonInflateFeedback?.PlayFeedbacks();
    }

    public void PlaySuccessSound()
    {
        successSoundFeedback?.PlayFeedbacks();
    }

    public void PlayErrorSound()
    {
        errorSoundFeedback?.PlayFeedbacks();
    }

    public void PlayCashFountainSound()
    {
        cashFountainFeedback?.PlayFeedbacks();
    }

    private void PlayUISoundWithParams(float volumeMultiplier, float pitchMultiplier)
    {
        if (uiSound == null || uiSound.FeedbacksList == null || uiSound.FeedbacksList.Count == 0) return;
        
        // Get the sound feedback directly (there's only one)
        if (uiSound.FeedbacksList[0] is MMF_MMSoundManagerSound soundFeedback)
        {
            soundFeedback.MinVolume = uiBaseVolume * volumeMultiplier;
            soundFeedback.MaxVolume = uiBaseVolume * volumeMultiplier;
            soundFeedback.MinPitch = uiBasePitch * pitchMultiplier;
            soundFeedback.MaxPitch = uiBasePitch * pitchMultiplier;
        }
        
        uiSound.PlayFeedbacks();
    }

    public void PlayUIHoverEnter(Transform target)
    {
        if (target == null || springManager == null) return;
        
        string springKey = GetOrCreateScaleSpringKey(target);
        springManager.MoveTo(springKey, 1.1f);
        
        // Volume: 0.3-0.5, Pitch: 1.05-1.15x
        PlayUISoundWithParams(volumeMultiplier: 0.4f, pitchMultiplier: 1.1f);
    }

    public void PlayUIHoverExit(Transform target)
    {
        if (target == null || springManager == null) return;
        
        string springKey = GetOrCreateScaleSpringKey(target);
        springManager.MoveTo(springKey, 1f);
        
        // Volume: 0.2-0.4, Pitch: 0.9-1.0x
        PlayUISoundWithParams(volumeMultiplier: 0.3f, pitchMultiplier: 0.95f);
    }

    public void PlayUIClickDown(Transform target)
    {
        if (target == null || springManager == null) return;
        
        string springKey = GetOrCreateScaleSpringKey(target);
        springManager.MoveTo(springKey, 0.9f);
        
        // Volume: 0.5-0.8, Pitch: 0.95-1.0x
        PlayUISoundWithParams(volumeMultiplier: 0.65f, pitchMultiplier: 0.975f);
    }

    public void PlayUIClickUp(Transform target)
    {
        if (target == null || springManager == null) return;
        
        string springKey = GetOrCreateScaleSpringKey(target);
        springManager.MoveTo(springKey, 1.1f);
        
        // Volume: 0.5-0.8, Pitch: 1.1-1.2x
        PlayUISoundWithParams(volumeMultiplier: 0.65f, pitchMultiplier: 1.15f);
    }

    public void AnimateGlowMaterial(GameObject obj, MaterialGlowState targetState)
    {
        if (obj == null || springManager == null) return;
        
        bool wasNewlyCreated = !_glowScaleSpringKeys.ContainsKey(obj);
        GetOrCreateGlowSpringKeys(obj, targetState);
        
        // If springs were just created, they're already at the target state (set instantly)
        // Otherwise, animate to the target state
        if (!wasNewlyCreated)
        {
            springManager.MoveTo(_glowScaleSpringKeys[obj], targetState.scale);
            springManager.MoveTo(_glowIntensitySpringKeys[obj], targetState.intensity);
            springManager.MoveTo(_glowColorRSpringKeys[obj], targetState.color.r);
            springManager.MoveTo(_glowColorGSpringKeys[obj], targetState.color.g);
            springManager.MoveTo(_glowColorBSpringKeys[obj], targetState.color.b);
        }
    }

    public void CleanupObject(Transform target)
    {
        if (target == null || springManager == null) return;
        
        if (_scaleSpringKeys.TryGetValue(target, out string springKey))
        {
            springManager.RemoveSpring(springKey);
            _scaleSpringKeys.Remove(target);
            _baseScales.Remove(target);
        }
    }

    public void CleanupGlowObject(GameObject obj)
    {
        if (obj == null || springManager == null) return;
        
        if (_glowScaleSpringKeys.TryGetValue(obj, out string _))
        {
            springManager.RemoveSpring(_glowScaleSpringKeys[obj]);
            springManager.RemoveSpring(_glowIntensitySpringKeys[obj]);
            springManager.RemoveSpring(_glowColorRSpringKeys[obj]);
            springManager.RemoveSpring(_glowColorGSpringKeys[obj]);
            springManager.RemoveSpring(_glowColorBSpringKeys[obj]);
            
            _glowScaleSpringKeys.Remove(obj);
            _glowIntensitySpringKeys.Remove(obj);
            _glowColorRSpringKeys.Remove(obj);
            _glowColorGSpringKeys.Remove(obj);
            _glowColorBSpringKeys.Remove(obj);
        }
    }
}

