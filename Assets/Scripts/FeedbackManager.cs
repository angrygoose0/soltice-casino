using UnityEngine;
using MoreMountains.Feedbacks;

public class FeedbackManager : MonoBehaviour
{
    [SerializeField] private MMF_Player screenshakeFeedback;

    public void PlayScreenshake()
    {
        screenshakeFeedback?.PlayFeedbacks();
    }
}

