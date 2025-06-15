using UnityEngine;

public class OnIdleNoTrayEntered : StateMachineBehaviour
{
    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        var mp = animator.GetComponentInParent<Game.Platforms.Scripts.MovingPlatform>();
        if (mp != null)
        {
            mp.OnAfraidAnimationDone();
        }
    }
}