using UnityEngine;

public class PlayerAnimationController : MonoBehaviour
{
    [SerializeField] private PlayerMovement movement;
    [SerializeField] private Animator animator;
    [SerializeField] private AnimationClip deathClip;

    private enum Facing { Down, Up, Left, Right }

    private Facing facing;
    private int currentState;
    private bool isDying;
    private static readonly int DeathState = Animator.StringToHash("Base Layer.Death");

    private static readonly int IdleState = Animator.StringToHash("Base Layer.IdleDown");

    private static readonly int[] WalkStates =
    {
        Animator.StringToHash("Base Layer.WalkDown"),
        Animator.StringToHash("Base Layer.WalkUp"),
        Animator.StringToHash("Base Layer.WalkLeft"),
        Animator.StringToHash("Base Layer.WalkRight")
    };

    private void Update()
    {
        if (isDying)
            return;

        Vector2 direction = movement.isActiveAndEnabled
            ? movement.MoveDirection
            : Vector2.zero;

        bool moving = direction != Vector2.zero;
        if (moving)
        {
            if (Mathf.Abs(direction.x) >= Mathf.Abs(direction.y))
                facing = direction.x < 0f ? Facing.Left : Facing.Right;
            else
                facing = direction.y < 0f ? Facing.Down : Facing.Up;
        }

        int state = moving ? WalkStates[(int)facing] : IdleState;
        if (state == currentState)
            return;

        animator.Play(state, 0, 0f);
        currentState = state;
    }

    public bool TryPlayDeath(out float duration)
    {
        duration = 0f;
        if (deathClip == null || animator == null || !animator.HasState(0, DeathState))
            return false;

        isDying = true;
        animator.updateMode = AnimatorUpdateMode.UnscaledTime;
        animator.Play(DeathState, 0, 0f);
        duration = deathClip.length;
        return true;
    }
}
