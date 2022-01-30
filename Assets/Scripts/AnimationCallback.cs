using UnityEngine;

public class AnimationCallback : MonoBehaviour
{
    public delegate void OnAnimationCompleteCallback(string animationName);
    public event OnAnimationCompleteCallback OnAnimationComplete;

    void AnimationComplete(string id) {
        OnAnimationComplete?.Invoke(id);
    }
}
