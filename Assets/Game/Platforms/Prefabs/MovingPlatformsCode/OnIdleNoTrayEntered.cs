using UnityEngine;

public class OnIdleNoTrayEntered : StateMachineBehaviour
{
    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        Debug.Log("OnIdleNoTrayEntered: OnStateEnter called for " + animator.name);
        var mp = animator.GetComponentInParent<Game.Platforms.Scripts.MovingPlatform>();
        if (mp != null)
        {
            mp.OnAfraidAnimationDone();
        }
    }
}