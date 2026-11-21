
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerHealth : MonoBehaviour
{
    [Header("Player Health")]
    public float maxHealth = 1000f;

    [Header("Player UI")]
    public Image hpFill;
    public TMP_Text hpText;

    [Header("Game Over")]
    public GameOverUI gameOverUI;
    [SerializeField] private PlayerAnimationController playerAnimation;

    private float currentHealth;
    private bool isDead = false;
    private bool runFinished;
    private List<UpgradePick> upgrades = new();

    public readonly struct UpgradePick
    {
        public readonly string stage;
        public readonly StageUpgrade upgrade;
        public readonly float before;
        public readonly float after;

        public UpgradePick(string stage, StageUpgrade upgrade, float before, float after)
        {
            this.stage = stage;
            this.upgrade = upgrade;
            this.before = before;
            this.after = after;
        }
    }

    private struct StageTransfer
    {
        public string destination;
        public float health;
        public float maxHealth;
        public float attackDamage;
        public float attackSpeed;
        public float moveSpeed;
        public int dashCapacity;
        public double playTime;
        public List<UpgradePick> upgrades;
    }

    private static StageTransfer nextStage;

    public bool IsDead => isDead;
    public float CurrentHealth => currentHealth;
    public double PlayTime { get; private set; }
    public IReadOnlyList<UpgradePick> SelectedUpgrades => upgrades;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void ResetStageTransfer()
    {
        nextStage = default;
    }

    void Start()
    {
        currentHealth = maxHealth;
        if (gameObject.scene.name == nextStage.destination)
        {
            maxHealth = nextStage.maxHealth;
            currentHealth = Mathf.Clamp(nextStage.health, 0f, maxHealth);
            var attack = GetComponent<SwordAttack>();
            attack.attackDamage = nextStage.attackDamage;
            attack.AttackSpeedMultiplier = nextStage.attackSpeed;
            var movement = GetComponent<PlayerMovement>();
            movement.moveSpeed = nextStage.moveSpeed;
            movement.SetDashCapacity(nextStage.dashCapacity);
            PlayTime = nextStage.playTime;
            upgrades = nextStage.upgrades;
        }
        ResetStageTransfer();
        isDead = false;

        UpdateHPUI();
    }

    private void Update()
    {
        if (!isDead && !runFinished)
            PlayTime += Time.deltaTime;
    }

    public void CarryHealthTo(string sceneName)
    {
        var attack = GetComponent<SwordAttack>();
        var movement = GetComponent<PlayerMovement>();
        nextStage = new StageTransfer
        {
            destination = sceneName,
            health = currentHealth,
            maxHealth = maxHealth,
            attackDamage = attack.attackDamage,
            attackSpeed = attack.AttackSpeedMultiplier,
            moveSpeed = movement.moveSpeed,
            dashCapacity = movement.DashCapacity,
            playTime = PlayTime,
            upgrades = upgrades
        };
    }

    public void ApplyUpgrade(StageUpgrade upgrade)
    {
        if (isDead || runFinished)
            return;

        float before = UpgradeValue(upgrade);
        switch (upgrade)
        {
            case StageUpgrade.MaxHealth:
                maxHealth += 100f;
                break;
            case StageUpgrade.Heal:
                currentHealth = Mathf.Min(maxHealth, currentHealth * 1.5f);
                break;
            case StageUpgrade.AttackDamage:
                GetComponent<SwordAttack>().attackDamage += 50f;
                break;
            case StageUpgrade.AttackSpeed:
                GetComponent<SwordAttack>().AttackSpeedMultiplier *= 1.5f;
                break;
            case StageUpgrade.MoveSpeed:
                GetComponent<PlayerMovement>().moveSpeed *= 1.5f;
                break;
            case StageUpgrade.Dash:
                var movement = GetComponent<PlayerMovement>();
                movement.SetDashCapacity(movement.DashCapacity + 1);
                break;
        }
        upgrades.Add(new UpgradePick(gameObject.scene.name, upgrade, before, UpgradeValue(upgrade)));
        UpdateHPUI();
    }

    private float UpgradeValue(StageUpgrade upgrade) => upgrade switch
    {
        StageUpgrade.MaxHealth => maxHealth,
        StageUpgrade.Heal => currentHealth,
        StageUpgrade.AttackDamage => GetComponent<SwordAttack>().attackDamage,
        StageUpgrade.AttackSpeed => GetComponent<SwordAttack>().AttackSpeedMultiplier,
        StageUpgrade.MoveSpeed => GetComponent<PlayerMovement>().moveSpeed,
        StageUpgrade.Dash => GetComponent<PlayerMovement>().DashCapacity,
        _ => 0f
    };

    public void TakeDamage(float damage)
    {
        if (isDead || runFinished) return;

        currentHealth = Mathf.Clamp(
            currentHealth - damage,
            0f,
            maxHealth
        );

        UpdateHPUI();

        Debug.Log("Player HP: " + currentHealth);

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    void UpdateHPUI()
    {
        float hpPercent = maxHealth > 0f
            ? currentHealth / maxHealth
            : 0f;

        if (hpFill != null)
        {
            hpFill.fillAmount = hpPercent;
        }

        if (hpText != null)
        {
            hpText.text =
                Mathf.CeilToInt(currentHealth)
                + " / " +
                Mathf.CeilToInt(maxHealth);
        }
    }

    void Die()
    {
        isDead = true;

        Debug.Log("GAME OVER - Player Defeated!");

        StopPlayer();

        float duration = 0f;
        bool playsDeath = playerAnimation != null && playerAnimation.TryPlayDeath(out duration);
        if (gameOverUI == null)
            return;

        if (playsDeath)
        {
            Time.timeScale = 0f;
            StartCoroutine(ShowGameOverAfterDeath(duration));
        }
        else
        {
            gameOverUI.ShowGameOver(this);
        }
    }

    public bool FinishVictoriousRun()
    {
        if (isDead || runFinished)
            return false;

        runFinished = true;
        StopPlayer();
        return true;
    }

    private void StopPlayer()
    {
        PlayerMovement movement =
            GetComponent<PlayerMovement>();

        SwordAttack attack =
            GetComponent<SwordAttack>();

        Rigidbody2D rb =
            GetComponent<Rigidbody2D>();

        if (movement != null)
            movement.enabled = false;

        if (attack != null)
            attack.enabled = false;

        if (rb != null)
            rb.linearVelocity = Vector2.zero;
    }

    private IEnumerator ShowGameOverAfterDeath(float duration)
    {
        yield return new WaitForSecondsRealtime(duration);
        gameOverUI.ShowGameOver(this);
    }
}
