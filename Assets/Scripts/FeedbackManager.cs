using UnityEngine;
using MoreMountains.Feedbacks;
using MoreMountains.FeedbacksForThirdParty;
using System.Reflection;

public class FeedbackManager : MonoBehaviour
{
    [SerializeField] private MMF_Player screenshakeFeedback;
    [SerializeField] private MMF_Player uiHoverEnterFeedback;
    [SerializeField] private MMF_Player uiHoverExitFeedback;
    [SerializeField] private MMF_Player uiClickFeedback;
    [SerializeField] private MMF_Player uiClickDownFeedback;
    [SerializeField] private MMF_Player uiClickUpFeedback;

    private void Start()
    {
        // Ensure all feedbacks are initialized before first use
        screenshakeFeedback?.Initialization();
        uiHoverEnterFeedback?.Initialization();
        uiHoverExitFeedback?.Initialization();
        uiClickFeedback?.Initialization();
        uiClickDownFeedback?.Initialization();
        uiClickUpFeedback?.Initialization();
    }

    public void PlayScreenshake()
    {
        screenshakeFeedback?.PlayFeedbacks();
    }

public void PlayUIHoverEnter(Transform target)
    {
        if (uiHoverEnterFeedback != null && target != null)
        {
            Debug.Log($"[HoverEnter] {target.name} - Current Scale: {target.localScale}");
            uiHoverExitFeedback?.StopFeedbacks();
            var hoverEnterSpring = uiHoverEnterFeedback.GetFeedbackOfType<MMF_ScaleSpring>();
            if (hoverEnterSpring != null)
            {
                hoverEnterSpring.AnimateScaleTarget = target;
            }
            
            uiHoverEnterFeedback.StopFeedbacks();
            uiHoverEnterFeedback.RestoreInitialValues();
            uiHoverEnterFeedback.PlayFeedbacks();
        }
    }

    public void PlayUIHoverExit(Transform target)
    {
        if (uiHoverExitFeedback != null && target != null)
        {
            Debug.Log($"[HoverExit] {target.name} - Current Scale: {target.localScale}");
            uiHoverEnterFeedback?.StopFeedbacks();
            
            var scaleSpring = uiHoverExitFeedback.GetFeedbackOfType<MMF_ScaleSpring>();
            if (scaleSpring != null)
            {
                scaleSpring.AnimateScaleTarget = target;
                
                // Manually update the spring's internal state to match current scale
                var currentScale = target.localScale;
                var type = typeof(MMF_ScaleSpring);
                var currentValueField = type.GetField("_currentValue", BindingFlags.NonPublic | BindingFlags.Instance);
                var targetValueField = type.GetField("_targetValue", BindingFlags.NonPublic | BindingFlags.Instance);
                
                if (currentValueField != null)
                {
                    currentValueField.SetValue(scaleSpring, currentScale);
                }
                if (targetValueField != null)
                {
                    targetValueField.SetValue(scaleSpring, currentScale);
                }
            }
            uiHoverExitFeedback.PlayFeedbacks();
        }
    }


    public void PlayUIClickDown(Transform target)
    {
        if (uiClickDownFeedback != null && target != null)
        {
            Debug.Log($"[ClickDown] {target.name} - Current Scale: {target.localScale}");
            var clickDownSpring = uiClickDownFeedback.GetFeedbackOfType<MMF_ScaleSpring>();
            if (clickDownSpring != null)
            {
                clickDownSpring.AnimateScaleTarget = target;
            }
            uiClickDownFeedback.PlayFeedbacks();
        }
    }

    public void PlayUIClickUp(Transform target)
    {
        // Disabled for now
    }
}

