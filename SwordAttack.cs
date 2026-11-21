
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SwordAttack : MonoBehaviour
{
    [Header("Sword References")]
    public Transform swordPivot;
    public GameObject swordVisual;
    public TrailRenderer swordTrail;
    [SerializeField] private SpriteRenderer slashRenderer;
    [SerializeField] private Sprite[] slashFrames;

    [Header("Attack Settings")]
    public float attackDamage = 20f;
    public float attackRange = 0.9f;
    public float attackRadius = 0.45f;
    public float attackDuration = 0.18f;
    public float attackCooldown = 0.3f;
    public float AttackSpeedMultiplier { get; set; } = 1f;

    [Header("Combo Settings")]
    public float comboResetTime = 1f;
    public float heavyAttackMultiplier = 2f;

    [Header("Knockback Settings")]
    public float knockbackForce = 3f;
    public float heavyKnockbackMultiplier = 2f;

    [Header("Enemy Detection")]
    public LayerMask enemyLayer;

    private Camera mainCamera;

    private bool isAttacking = false;
    private bool queuedAttack = false;

    private int comboStep = 0;

    private float lastAttackTime = -100f;
    private float nextAttackTime = 0f;

    void Start()
    {
        mainCamera = Camera.main;

        if (swordVisual != null)
            swordVisual.SetActive(false);

        if (swordTrail != null)
        {
            swordTrail.emitting = false;
            swordTrail.Clear();
        }
    }

    void Update()
    {
        if (Time.timeScale == 0f)
            return;

        if (Input.GetMouseButtonDown(0))
        {
            if (isAttacking)
            {
                queuedAttack = true;
            }
            else if (Time.time >= nextAttackTime)
            {
                StartCoroutine(AttackSequence());
            }
        }
    }

    IEnumerator AttackSequence()
    {
        if (swordPivot == null || mainCamera == null)
            yield break;

        isAttacking = true;

        while (true)
        {
            queuedAttack = false;

            // Combo reset
            if (Time.time - lastAttackTime > comboResetTime)
                comboStep = 0;

            comboStep++;

            if (comboStep > 3)
                comboStep = 1;

            lastAttackTime = Time.time;
            nextAttackTime = Time.time + attackCooldown / AttackSpeedMultiplier;

            Debug.Log("Combo Attack: " + comboStep);

            // Get mouse position
            Vector3 mouseScreen = Input.mousePosition;

            mouseScreen.z = Mathf.Abs(
                mainCamera.transform.position.z -
                transform.position.z
            );

            Vector3 mouseWorld =
                mainCamera.ScreenToWorldPoint(mouseScreen);

            Vector2 direction =
                (Vector2)(mouseWorld - transform.position);

            if (direction.sqrMagnitude < 0.001f)
                direction = Vector2.right;

            direction.Normalize();

            float baseAngle = Mathf.Atan2(
                direction.y,
                direction.x
            ) * Mathf.Rad2Deg;

            // Sword swing angles
            float startOffset;
            float endOffset;

            if (comboStep == 1)
            {
                startOffset = -75f;
                endOffset = 65f;
            }
            else if (comboStep == 2)
            {
                startOffset = 75f;
                endOffset = -65f;
            }
            else
            {
                startOffset = -110f;
                endOffset = 110f;
            }

            float duration = attackDuration / AttackSpeedMultiplier;
            float damage = attackDamage;
            float currentKnockback = knockbackForce;

            // Combo damage
            if (comboStep == 2)
            {
                damage *= 1.25f;
            }

            if (comboStep == 3)
            {
                duration *= 1.4f;
                damage *= heavyAttackMultiplier;

                currentKnockback *=
                    heavyKnockbackMultiplier;
            }

            HashSet<EnemyHealth> hitEnemies =
                new HashSet<EnemyHealth>();

            // Show sword
            if (swordVisual != null)
                swordVisual.SetActive(true);

            // Position sword at start of swing
            swordPivot.rotation = Quaternion.Euler(
                0f, 0f, baseAngle + startOffset
            );

            bool useSlashSprites = slashRenderer != null && slashFrames != null && slashFrames.Length > 0;
            if (useSlashSprites)
            {
                slashRenderer.transform.rotation = Quaternion.Euler(0f, 0f, baseAngle);
                slashRenderer.enabled = true;
            }
            else if (swordTrail != null)
            {
                swordTrail.emitting = false;
                swordTrail.Clear();
                swordTrail.emitting = true;
            }

            float elapsed = 0f;

            while (elapsed < duration)
            {
                float progress =
                    Mathf.Clamp01(elapsed / duration);

                if (useSlashSprites)
                {
                    int frame = Mathf.Min((int)(progress * slashFrames.Length), slashFrames.Length - 1);
                    slashRenderer.sprite = slashFrames[frame];
                    slashRenderer.transform.position = transform.position + (Vector3)(direction * attackRange);
                }

                float angle = baseAngle + Mathf.Lerp(
                    startOffset,
                    endOffset,
                    progress
                );

                swordPivot.rotation =
                    Quaternion.Euler(0f, 0f, angle);

                float radians =
                    angle * Mathf.Deg2Rad;

                Vector2 swingDirection = new Vector2(
                    Mathf.Cos(radians),
                    Mathf.Sin(radians)
                );

                // Hitbox follows the sword
                Vector2 hitCenter =
                    (Vector2)transform.position +
                    swingDirection * attackRange;

                Collider2D[] hits =
                    Physics2D.OverlapCircleAll(
                        hitCenter,
                        attackRadius,
                        enemyLayer
                    );

                foreach (Collider2D hit in hits)
                {
                    EnemyHealth enemy =
                        hit.GetComponentInParent<EnemyHealth>();

                    if (enemy == null)
                        continue;

                    if (hitEnemies.Contains(enemy))
                        continue;

                    if (ArenaWalls.BlocksAttack(transform.position, hit.bounds.center))
                        continue;

                    hitEnemies.Add(enemy);

                    // Knock enemy away from player
                    Vector2 knockbackDirection =
                        (Vector2)enemy.transform.position -
                        (Vector2)transform.position;

                    if (knockbackDirection.sqrMagnitude
                        < 0.001f)
                    {
                        knockbackDirection = swingDirection;
                    }

                    enemy.TakeDamage(
                        damage,
                        knockbackDirection.normalized,
                        currentKnockback
                    );
                }

                elapsed += Time.deltaTime;
                yield return null;
            }

            // Stop producing new trail
            if (swordTrail != null)
                swordTrail.emitting = false;

            if (slashRenderer != null)
                slashRenderer.enabled = false;

            if (swordVisual != null)
                swordVisual.SetActive(false);

            // Attack cooldown
            while (Time.time < nextAttackTime)
                yield return null;

            if (!queuedAttack || comboStep >= 3)
                break;
        }

        queuedAttack = false;
        isAttacking = false;
    }

    void OnDisable()
    {
        StopAllCoroutines();

        if (slashRenderer != null)
            slashRenderer.enabled = false;

        if (swordTrail != null)
        {
            swordTrail.emitting = false;
            swordTrail.Clear();
        }

        if (swordVisual != null)
            swordVisual.SetActive(false);

        isAttacking = false;
        queuedAttack = false;
    }
}
