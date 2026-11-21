using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SlumExit : MonoBehaviour
{
    [SerializeField] private SlumWaves waves;
    [SerializeField] private GameObject openPassage;
    [SerializeField] private Transform shutter;
    [SerializeField] private Collider2D blocker;
    [SerializeField] private Collider2D entryTrigger;
    [SerializeField, Min(0f)] private float openingDuration = 0.35f;
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private string nextScene;
    [SerializeField] private StageRewardPanel stageRewards;

    private void OnEnable()
    {
        if (waves != null)
            waves.Cleared += Open;
    }

    private void OnDisable()
    {
        if (waves != null)
            waves.Cleared -= Open;
    }

    private void Open()
    {
        if (stageRewards != null)
            StageRewardPanel.Offer(stageRewards, playerHealth, OpenGate);
        else
            OpenGate();
    }

    private void OpenGate()
    {
        StartCoroutine(RaiseShutter());
    }

    private IEnumerator RaiseShutter()
    {
        if (openPassage != null)
            openPassage.SetActive(true);
        if (shutter != null)
        {
            shutter.gameObject.SetActive(true);
            Vector3 closedScale = shutter.localScale;
            for (float elapsed = 0f; elapsed < openingDuration; elapsed += Time.deltaTime)
            {
                float height = 1f - Mathf.SmoothStep(0f, 1f, elapsed / openingDuration);
                shutter.localScale = new Vector3(closedScale.x, closedScale.y * height, closedScale.z);
                yield return null;
            }
            shutter.gameObject.SetActive(false);
        }

        blocker.enabled = false;
        entryTrigger.enabled = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!entryTrigger.enabled || playerHealth == null || playerHealth.IsDead ||
            string.IsNullOrEmpty(nextScene) || other.GetComponentInParent<PlayerHealth>() != playerHealth)
            return;

        entryTrigger.enabled = false;
        playerHealth.CarryHealthTo(nextScene);
        SceneManager.LoadSceneAsync(nextScene);
    }
}
