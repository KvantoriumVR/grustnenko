using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Экран смерти. Показывается при гибели игрока.
/// Canvas всегда включён, включаются только фон и панель.
/// </summary>
public class DeathScreen : MonoBehaviour
{
    [Header("UI элементы")]
    [Tooltip("Полупрозрачный чёрный фон (Image)")]
    [SerializeField] private GameObject _background;

    [Tooltip("Панель с кнопками и текстом")]
    [SerializeField] private GameObject _deathPanel;

    [Tooltip("Кнопка перезапуска")]
    [SerializeField] private Button _restartButton;

    [Tooltip("Кнопка выхода")]
    [SerializeField] private Button _exitButton;

    private void Start()
    {
        _restartButton.onClick.AddListener(RestartGame);
        _exitButton.onClick.AddListener(ExitGame);
    }

    /// <summary>
    /// Показать экран смерти
    /// </summary>
    public void ShowDeathScreen()
    {
        _background.SetActive(true);
        _deathPanel.SetActive(true);
        Time.timeScale = 0f;

        // Разблокируем курсор только для обычной версии (не VR)
        if (!UnityEngine.XR.XRSettings.isDeviceActive)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    private void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void ExitGame()
    {
        Time.timeScale = 1f;
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}