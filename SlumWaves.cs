using System;
using System.Collections;
using TMPro;
using UnityEngine;

public class SlumWaves : MonoBehaviour
{
    [SerializeField] private EnemyHealth enemyPrefab;
    [SerializeField] private EnemyHealth alternateEnemyPrefab;
    [SerializeField] private Transform[] enemySpawns;
    [SerializeField] private Transform largeEnemySpawn;
    [SerializeField, Min(1)] private int largeEnemyProjectilesPerAttack = 1;
    [SerializeField] private bool largeEnemyFiresSecondVolley;
    [SerializeField] private TMP_Text waveText;
    [SerializeField, Min(0f)] private float breakBetweenWaves = 0.8f;
    [SerializeField] private string stageClearText = "Stage Clear  |  Enter the gate";

    private const int WaveCount = 3;
    private const float LargeEnemyScaleMultiplier = 2f;
    private const float LargeEnemyHealth = 400f;
    private const float LargeEnemyDamage = 50f;

    public event Action Cleared;
    public int CurrentWave { get; private set; }
    public int EnemiesRemaining { get; private set; }

    private IEnumerator Start()
    {
        for (int wave = 1; wave <= WaveCount; wave++)
        {
            CurrentWave = wave;
            if (wave == WaveCount)
            {
                EnemiesRemaining = 1;
                SpawnEnemy(enemyPrefab, largeEnemySpawn, LargeEnemyScaleMultiplier);
            }
            else
            {
                EnemiesRemaining = enemySpawns.Length;
                SpawnRegularEnemies();
            }

            UpdateWaveText();
            while (EnemiesRemaining > 0)
                yield return null;

            if (wave < WaveCount)
            {
                waveText.text = $"Wave {wave} cleared";
                yield return new WaitForSeconds(breakBetweenWaves);
            }
        }

        waveText.text = stageClearText;
        Cleared?.Invoke();
    }

    private void SpawnRegularEnemies()
    {
        bool mixedWave = alternateEnemyPrefab != null && enemySpawns.Length >= 2;
        int primarySlot = mixedWave ? UnityEngine.Random.Range(0, enemySpawns.Length) : -1;
        int alternateSlot = mixedWave
            ? (primarySlot + UnityEngine.Random.Range(1, enemySpawns.Length)) % enemySpawns.Length
            : -1;

        for (int i = 0; i < enemySpawns.Length; i++)
        {
            bool useAlternate = mixedWave && (i == alternateSlot ||
                (i != primarySlot && UnityEngine.Random.value < .5f));
            SpawnEnemy(useAlternate ? alternateEnemyPrefab : enemyPrefab, enemySpawns[i], 1f);
        }
    }

    private void SpawnEnemy(EnemyHealth prefab, Transform spawn, float multiplier)
    {
        EnemyHealth enemy = Instantiate(prefab, spawn.position, spawn.rotation, transform);
        enemy.Defeated += CountDefeat;
        if (multiplier == 1f)
            return;

        Vector3 scale = enemy.transform.localScale;
        var ai = enemy.GetComponent<EnemyAI>();
        var bodyCollider = enemy.GetComponent<BoxCollider2D>();
        Vector2 scaledSize = Vector2.Scale(bodyCollider.size, new Vector2(Mathf.Abs(scale.x), Mathf.Abs(scale.y)));

        enemy.transform.localScale = new Vector3(scale.x * multiplier, scale.y * multiplier, scale.z);
        enemy.maxHealth = LargeEnemyHealth;
        ai.attackDamage = LargeEnemyDamage;
        ai.ProjectilesPerAttack = largeEnemyProjectilesPerAttack;
        ai.FiresSecondVolley = largeEnemyFiresSecondVolley;
        // Keep the larger enemy within striking range even when its collider meets the player at a corner.
        ai.attackRange += scaledSize.magnitude * 0.5f * (multiplier - 1f);
    }

    private void CountDefeat()
    {
        EnemiesRemaining--;
        UpdateWaveText();
    }

    private void UpdateWaveText()
    {
        waveText.text = $"Wave {CurrentWave} / {WaveCount}    Enemies: {EnemiesRemaining}";
    }
}
