using UnityEngine;
using MoreMountains.Feedbacks;
using MoreMountains.FeedbacksForThirdParty;

public class FeedbackManager : MonoBehaviour
{
    [SerializeField] private MMF_Player screenshakeFeedback;
    [SerializeField] private MMF_Player uiScaleSpringFeedback;

    private void Start()
    {
        screenshakeFeedback?.Initialization();
        uiScaleSpringFeedback?.Initialization();
    }

    public void PlayScreenshake()
    {
        screenshakeFeedback?.PlayFeedbacks();
    }

    public void PlayUIHoverEnter(Transform target)
    {
        if (uiScaleSpringFeedback != null && target != null)
        {
            var scaleSpring = uiScaleSpringFeedback.GetFeedbackOfType<MMF_ScaleSpring>();
            if (scaleSpring != null)
            {
                scaleSpring.AnimateScaleTarget = target;
                scaleSpring.MoveToScaleMin = Vector3.one * 1.1f;
                scaleSpring.MoveToScaleMax = Vector3.one * 1.1f;
            }
            uiScaleSpringFeedback.PlayFeedbacks();
        }
    }

    public void PlayUIHoverExit(Transform target)
    {
        if (uiScaleSpringFeedback != null && target != null)
        {
            var scaleSpring = uiScaleSpringFeedback.GetFeedbackOfType<MMF_ScaleSpring>();
            if (scaleSpring != null)
            {
                scaleSpring.AnimateScaleTarget = target;
                scaleSpring.MoveToScaleMin = Vector3.one * 1.0f;
                scaleSpring.MoveToScaleMax = Vector3.one * 1.0f;
            }
            uiScaleSpringFeedback.PlayFeedbacks();
        }
    }

    public void PlayUIClickDown(Transform target)
    {
        if (uiScaleSpringFeedback != null && target != null)
        {
            var scaleSpring = uiScaleSpringFeedback.GetFeedbackOfType<MMF_ScaleSpring>();
            if (scaleSpring != null)
            {
                scaleSpring.AnimateScaleTarget = target;
                scaleSpring.MoveToScaleMin = Vector3.one * 0.9f;
                scaleSpring.MoveToScaleMax = Vector3.one * 0.9f;
            }
            uiScaleSpringFeedback.PlayFeedbacks();
        }
    }

    public void PlayUIClickUp(Transform target)
    {
        if (uiScaleSpringFeedback != null && target != null)
        {
            var scaleSpring = uiScaleSpringFeedback.GetFeedbackOfType<MMF_ScaleSpring>();
            if (scaleSpring != null)
            {
                scaleSpring.AnimateScaleTarget = target;
                scaleSpring.MoveToScaleMin = Vector3.one * 1.1f;
                scaleSpring.MoveToScaleMax = Vector3.one * 1.1f;
            }
            uiScaleSpringFeedback.PlayFeedbacks();
        }
    }
}

