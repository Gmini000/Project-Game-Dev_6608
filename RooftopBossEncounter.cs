using System.Collections;
using TMPro;
using UnityEngine;

public class RooftopBossEncounter : MonoBehaviour
{
    [SerializeField] private EnemyHealth boss;
    [SerializeField] private TMP_Text status;
    [SerializeField] private PlayerHealth player;

    private void OnEnable()
    {
        status.text = "Rooftop Wraith Core";
        boss.Defeated += Clear;
    }

    private void OnDisable()
    {
        boss.Defeated -= Clear;
    }

    private void Clear()
    {
        status.text = "Factory Rooftop Cleared";
        if (player.FinishVictoriousRun())
            StartCoroutine(ShowVictoryAfterDeath());
    }

    private IEnumerator ShowVictoryAfterDeath()
    {
        yield return new WaitUntil(() => boss == null);
        player.gameOverUI.ShowVictory(player);
    }
}
