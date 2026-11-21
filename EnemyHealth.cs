
using System;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyHealth : MonoBehaviour
{
    [Header("Enemy Health")]
    public float maxHealth = 100f;

    [Header("HP UI")]
    public Image hpFill;

    [Header("Knockback")]
    public float knockbackResistance = 1f;
    public float knockbackStunTime = 0.25f;

    [SerializeField] private EnemyAnimationController enemyAnimation;

    private float currentHealth;
    private float knockbackUntil;
    private Rigidbody2D rb;
    private bool isDead;

    public event Action Defeated;

    public bool IsKnockedBack =>
        Time.time < knockbackUntil;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Start()
    {
        currentHealth = maxHealth;
        UpdateHPUI();
    }

    public void TakeDamage(float damage)
    {
        TakeDamage(damage, Vector2.zero, 0f);
    }

    public void TakeDamage(
        float damage,
        Vector2 direction,
        float force)
    {
        if (isDead) return;

        currentHealth = Mathf.Clamp(
            currentHealth - damage,
            0f,
            maxHealth
        );

        UpdateHPUI();

        Debug.Log("Enemy HP: " + currentHealth);

        if (currentHealth <= 0f)
        {
            Die();
            return;
        }

        ApplyKnockback(direction, force);
    }

    void UpdateHPUI()
    {
        if (hpFill != null)
        {
            hpFill.fillAmount = maxHealth > 0f
                ? currentHealth / maxHealth
                : 0f;
        }
    }

    void ApplyKnockback(
        Vector2 direction,
        float force)
    {
        if (force <= 0f ||
            direction.sqrMagnitude < 0.001f)
            return;

        direction.Normalize();

        float resistance = Mathf.Max(
            0.01f,
            knockbackResistance
        );

        knockbackUntil =
            Time.time + knockbackStunTime;

        rb.linearVelocity = Vector2.zero;

        rb.AddForce(
            direction * (force / resistance),
            ForceMode2D.Impulse
        );
    }

    void Die()
    {
        isDead = true;

        Debug.Log("Enemy Defeated!");
        Defeated?.Invoke();

        if (enemyAnimation == null || !enemyAnimation.TryPlayDeath(out float duration))
        {
            Destroy(gameObject);
            return;
        }

        if (TryGetComponent(out EnemyAI ai))
            ai.enabled = false;

        rb.linearVelocity = Vector2.zero;
        rb.simulated = false;

        Canvas hpCanvas = hpFill != null ? hpFill.canvas : null;
        if (hpCanvas != null && hpCanvas.transform.IsChildOf(transform))
            hpCanvas.enabled = false;

        Destroy(gameObject, duration);
    }
}
