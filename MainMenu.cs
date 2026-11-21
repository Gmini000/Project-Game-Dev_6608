using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour, IPointerClickHandler
{
    public const string FirstStage = "SlumLevel";

    [SerializeField] private RectTransform gear;
    [SerializeField] private RectTransform logoArea;
    [SerializeField] private GameObject menuOptions;
    [SerializeField] private GameObject newGameButton;
    [SerializeField] private float gearDegreesPerSecond = 24f;

    private bool leavingMenu;

    private void Update()
    {
        gear.Rotate(0f, 0f, -gearDegreesPerSecond * Time.unscaledDeltaTime);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (menuOptions.activeSelf || eventData.button != PointerEventData.InputButton.Left ||
            RectTransformUtility.RectangleContainsScreenPoint(logoArea, eventData.position, eventData.pressEventCamera))
            return;

        menuOptions.SetActive(true);
        EventSystem.current.SetSelectedGameObject(newGameButton);
    }

    public void NewGame()
    {
        if (leavingMenu || !menuOptions.activeSelf)
            return;

        leavingMenu = true;
        PlayerHealth.ResetStageTransfer();
        Time.timeScale = 1f;
        SceneManager.LoadSceneAsync(FirstStage);
    }

    public void Exit()
    {
        if (leavingMenu || !menuOptions.activeSelf)
            return;

        leavingMenu = true;
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
