using UnityEngine;

public class EnemyAnimationController : MonoBehaviour
{
    [SerializeField] private Rigidbody2D body;
    [SerializeField] private EnemyHealth health;
    [SerializeField] private Animator animator;
    [SerializeField] private AnimationClip deathClip;

    private enum Facing { Down, Up, Left, Right }

    private static readonly int IdleDown = Animator.StringToHash("Base Layer.IdleDown");
    private static readonly int IdleUp = Animator.StringToHash("Base Layer.IdleUp");
    private static readonly int Death = Animator.StringToHash("Base Layer.Death");

    private static readonly int[] WalkStates =
    {
        Animator.StringToHash("Base Layer.WalkDown"),
        Animator.StringToHash("Base Layer.WalkUp"),
        Animator.StringToHash("Base Layer.WalkLeft"),
        Animator.StringToHash("Base Layer.WalkRight")
    };

    private static readonly int[] AttackStates =
    {
        Animator.StringToHash("Base Layer.AttackDown"),
        Animator.StringToHash("Base Layer.AttackUp"),
        Animator.StringToHash("Base Layer.AttackLeft"),
        Animator.StringToHash("Base Layer.AttackRight")
    };

    private static readonly int[] MeleeAttackStates =
    {
        Animator.StringToHash("Base Layer.MeleeAttackDown"),
        Animator.StringToHash("Base Layer.MeleeAttackUp"),
        Animator.StringToHash("Base Layer.MeleeAttackLeft"),
        Animator.StringToHash("Base Layer.MeleeAttackRight")
    };

    private static readonly int[] ShootAttackStates =
    {
        Animator.StringToHash("Base Layer.ShootAttackDown"),
        Animator.StringToHash("Base Layer.ShootAttackUp"),
        Animator.StringToHash("Base Layer.ShootAttackLeft"),
        Animator.StringToHash("Base Layer.ShootAttackRight")
    };

    private Facing facing;
    private bool idleFacingUp;
    private bool attacking;
    private bool dying;
    private int currentState;

    private void LateUpdate()
    {
        if (dying)
            return;

        if (attacking)
        {
            AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
            if (state.fullPathHash != currentState || state.normalizedTime < 1f)
                return;

            attacking = false;
        }

        Vector2 velocity = body.linearVelocity;
        bool walking = !health.IsKnockedBack && velocity.sqrMagnitude > 0.01f;
        if (walking)
            SetFacing(velocity);

        int nextState = walking ? WalkStates[(int)facing] : idleFacingUp ? IdleUp : IdleDown;
        if (nextState != currentState)
            Play(nextState);
    }

    public void PlayAttack(Vector2 direction) => PlayAttack(direction, AttackStates);

    public void PlayMeleeAttack(Vector2 direction) => PlayAttack(direction, MeleeAttackStates);

    public void PlayShootAttack(Vector2 direction) => PlayAttack(direction, ShootAttackStates);

    private void PlayAttack(Vector2 direction, int[] states)
    {
        if (dying)
            return;

        SetFacing(direction);
        attacking = true;
        Play(states[(int)facing]);
    }

    public bool TryPlayDeath(out float duration)
    {
        duration = 0f;
        if (deathClip == null || animator == null || !animator.HasState(0, Death))
            return false;

        dying = true;
        Play(Death);
        duration = deathClip.length;
        return true;
    }

    private void SetFacing(Vector2 direction)
    {
        if (direction == Vector2.zero)
            return;

        if (Mathf.Abs(direction.x) >= Mathf.Abs(direction.y))
        {
            facing = direction.x < 0f ? Facing.Left : Facing.Right;
            return;
        }

        idleFacingUp = direction.y > 0f;
        facing = idleFacingUp ? Facing.Up : Facing.Down;
    }

    private void Play(int state)
    {
        currentState = state;
        animator.Play(state, 0, 0f);
    }
}
