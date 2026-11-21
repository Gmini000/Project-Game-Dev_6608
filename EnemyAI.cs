
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(EnemyHealth))]
public class EnemyAI : MonoBehaviour
{
    [Header("Target")]
    public Transform player;

    [Header("Movement")]
    public float moveSpeed = 2.5f;
    public float detectionRange = 7f;
    public float attackRange = 1.2f;

    [Header("Attack")]
    public float attackDamage = 10f;
    public float attackCooldown = 1.5f;
    [field: SerializeField, Min(1)] public int ProjectilesPerAttack { get; set; } = 1;
    public bool FiresSecondVolley { get; set; }

    [SerializeField] private EnemyAnimationController enemyAnimation;
    [SerializeField, Range(0f, 1f)] private float retreatChance = 0.55f;
    [SerializeField] private Sprite projectileSprite;
    [SerializeField] private bool firesParallelShots;
    [SerializeField, Min(0f)] private float meleeRange;
    [SerializeField] private Sprite meleeWaveSprite;
    [SerializeField, Min(0f)] private float attackImpactDelay = .22f;

    private const float AttackRecovery = 0.42f;
    private const float SecondVolleyDelay = 0.2f;
    private const float RetreatDuration = 0.65f;
    private static readonly float[] RetreatAngles = { 0f, 45f, -45f, 90f, -90f };

    private Rigidbody2D rb;
    private EnemyHealth enemyHealth;
    private PlayerHealth playerHealth;
    private ArenaWalls arena;
    private Vector2 bodySize;
    private Vector2 waypoint;

    private float nextAttackTime;
    private float nextPathUpdate;
    private float retreatStart;
    private float retreatEnd;
    private bool preparingAttack;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        enemyHealth = GetComponent<EnemyHealth>();
    }

    void OnDisable()
    {
        StopAllCoroutines();
        preparingAttack = false;
    }

    void Start()
    {
        // Automatically find Player by tag
        if (player == null)
        {
            GameObject target =
                GameObject.FindGameObjectWithTag("Player");

            if (target != null)
                player = target.transform;
        }

        if (player != null)
            playerHealth = player.GetComponent<PlayerHealth>();

        arena = FindFirstObjectByType<ArenaWalls>();
        var bodyCollider = GetComponent<Collider2D>();
        bodySize = bodyCollider != null ? (Vector2)bodyCollider.bounds.size : Vector2.one * .8f;
    }

    void FixedUpdate()
    {
        if (player == null)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        // Let knockback physics control the enemy
        if (enemyHealth.IsKnockedBack)
            return;

        if (preparingAttack)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        if (Time.time < retreatEnd)
        {
            rb.linearVelocity = Time.time < retreatStart ? Vector2.zero : RetreatVelocity();
            return;
        }

        Vector2 target = player.position;
        float distance = Vector2.Distance(rb.position, target);
        if (distance > detectionRange || CanStrike(target, distance))
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        rb.linearVelocity = ChaseVelocity(target);
    }

    void Update()
    {
        if (player == null || playerHealth == null)
            return;

        if (preparingAttack || enemyHealth.IsKnockedBack || Time.time < retreatEnd)
            return;

        Vector2 target = player.position;
        if (Time.time >= nextAttackTime && CanStrike(target, Vector2.Distance(rb.position, target)))
        {
            AttackPlayer();
        }
    }

    void AttackPlayer()
    {
        nextAttackTime = Time.time + attackCooldown;

        Debug.Log("Enemy Attack!");

        Vector2 direction = (Vector2)player.position - rb.position;
        if (projectileSprite != null && meleeRange > 0f)
        {
            bool melee = direction.sqrMagnitude <= meleeRange * meleeRange;
            if (melee ? meleeWaveSprite != null : firesParallelShots)
                direction = CardinalDirection(direction);

            if (enemyAnimation != null)
            {
                if (melee)
                    enemyAnimation.PlayMeleeAttack(direction);
                else
                    enemyAnimation.PlayShootAttack(direction);
            }
            StartCoroutine(ResolveMeleeOrShot(melee, direction));
            return;
        }

        if (enemyAnimation != null)
            enemyAnimation.PlayAttack(direction);

        if (projectileSprite != null)
        {
            FireProjectiles(direction);
            if (FiresSecondVolley)
                StartCoroutine(FireSecondVolley(direction));
        }
        else
            playerHealth.TakeDamage(attackDamage);

        TryRetreat();
    }

    private IEnumerator ResolveMeleeOrShot(bool melee, Vector2 direction)
    {
        preparingAttack = true;
        yield return new WaitForSeconds(attackImpactDelay);
        preparingAttack = false;
        if (playerHealth == null || playerHealth.IsDead || enemyHealth.IsKnockedBack)
            yield break;

        if (melee && meleeWaveSprite != null)
        {
            Vector2 origin = rb.position + direction * .9f;
            float span = direction.x == 0f ? 2.26f : 1.8f;
            if (!ArenaWalls.BlocksAttack(rb.position, origin))
                EnemyProjectile.FireSlash(origin, direction, attackDamage, meleeWaveSprite, playerHealth, span);
        }
        else if (melee)
        {
            Vector2 target = player.position;
            Vector2 offset = target - rb.position;
            if (offset.sqrMagnitude <= meleeRange * meleeRange && Vector2.Dot(direction, offset) >= 0f &&
                !ArenaWalls.BlocksAttack(rb.position, target))
                playerHealth.TakeDamage(attackDamage);
        }
        else if (firesParallelShots)
            FireParallelProjectiles(direction);
        else
            FireProjectiles(direction);

        TryRetreat(Mathf.Max(0f, AttackRecovery - attackImpactDelay));
    }

    private static Vector2 CardinalDirection(Vector2 aim)
    {
        if (aim == Vector2.zero)
            return Vector2.down;

        if (Mathf.Abs(aim.x) >= Mathf.Abs(aim.y))
            return aim.x < 0f ? Vector2.left : Vector2.right;

        return aim.y < 0f ? Vector2.down : Vector2.up;
    }

    private void FireParallelProjectiles(Vector2 direction)
    {
        // Local offsets follow the two flame heads in each release pose.
        Vector2 center;
        float halfSpacing;
        if (direction.x == 0f)
        {
            center = direction.y > 0f ? new Vector2(0f, .71f) : new Vector2(0f, -.62f);
            halfSpacing = direction.y > 0f ? .45f : .58f;
        }
        else
        {
            center = direction.x > 0f ? new Vector2(.86f, -.18f) : new Vector2(-1.02f, -.145f);
            halfSpacing = direction.x > 0f ? .19f : .22f;
        }

        Vector2 side = Vector2.Perpendicular(direction) * halfSpacing;
        FireFromMuzzle(center - side, direction);
        FireFromMuzzle(center + side, direction);
    }

    private void FireFromMuzzle(Vector2 localMuzzle, Vector2 direction)
    {
        Vector2 origin = transform.TransformPoint(localMuzzle);
        if (!ArenaWalls.BlocksAttack(rb.position, origin))
            EnemyProjectile.Fire(origin, direction, attackDamage, projectileSprite, playerHealth, sizeMultiplier: 1.65f);
    }

    private void TryRetreat(float recovery = AttackRecovery)
    {
        if (Random.value >= retreatChance)
            return;

        retreatStart = Time.time + recovery;
        retreatEnd = retreatStart + RetreatDuration;
    }

    private IEnumerator FireSecondVolley(Vector2 aim)
    {
        yield return new WaitForSeconds(SecondVolleyDelay);
        if (playerHealth != null && !playerHealth.IsDead)
            FireProjectiles(aim, 180f / Mathf.Max(1, ProjectilesPerAttack));
    }

    private void FireProjectiles(Vector2 aim, float angleOffset = 0f)
    {
        int count = Mathf.Max(1, ProjectilesPerAttack);
        Vector2 direction = aim.sqrMagnitude > .0001f ? aim.normalized : Vector2.down;
        float spacing = 360f / count;

        for (int i = 0; i < count; i++)
        {
            Vector2 shotDirection = Quaternion.Euler(0f, 0f, i * spacing + angleOffset) * direction;
            EnemyProjectile.Fire(rb.position, shotDirection, attackDamage, projectileSprite, playerHealth);
        }
    }

    private bool CanStrike(Vector2 target, float distance) =>
        distance <= attackRange && !ArenaWalls.BlocksAttack(rb.position, target);

    private Vector2 ChaseVelocity(Vector2 target)
    {
        if (arena == null || ArenaWalls.CanTravel(rb.position, target, bodySize))
        {
            nextPathUpdate = 0f;
            return (target - rb.position).normalized * moveSpeed;
        }

        if (Time.time >= nextPathUpdate)
        {
            waypoint = arena.NextWaypoint(rb.position, target, bodySize);
            nextPathUpdate = Time.time + .22f;
        }

        return Vector2.ClampMagnitude((waypoint - rb.position) / Time.fixedDeltaTime, moveSpeed);
    }

    private Vector2 RetreatVelocity()
    {
        Vector2 away = (rb.position - (Vector2)player.position).normalized;
        foreach (float angle in RetreatAngles)
        {
            Vector2 direction = Quaternion.Euler(0f, 0f, angle) * away;
            if (ArenaWalls.CanTravel(rb.position, rb.position + direction * .45f, bodySize))
                return direction * moveSpeed;
        }

        return Vector2.zero;
    }
}
