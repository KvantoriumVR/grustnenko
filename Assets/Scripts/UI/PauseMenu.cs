using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Меню паузы. Вызывается по нажатию Escape.
/// Canvas всегда включён, включаются только фон и панель.
/// </summary>
public class PauseMenu : MonoBehaviour
{
    [Header("UI элементы")]
    [Tooltip("Полупрозрачный чёрный фон (Image)")]
    [SerializeField] private GameObject _background;

    [Tooltip("Панель с кнопками")]
    [SerializeField] private GameObject _pausePanel;

    [Tooltip("Кнопка продолжения")]
    [SerializeField] private Button _resumeButton;

    [Tooltip("Кнопка перезапуска")]
    [SerializeField] private Button _restartButton;

    [Tooltip("Кнопка выхода")]
    [SerializeField] private Button _exitButton;

    [Header("Мобы (перетащить всех)")]
    [Tooltip("Массив мобов, которые будут отключены при паузе")]
    [SerializeField] private MonoBehaviour[] _mobs;

    private bool _isPaused = false;

    private void Start()
    {
        // Скрываем меню при старте
        _background.SetActive(false);
        _pausePanel.SetActive(false);

        _resumeButton.onClick.AddListener(ResumeGame);
        _restartButton.onClick.AddListener(RestartGame);
        _exitButton.onClick.AddListener(ExitGame);
    }

    private void Update()
    {
        // Открыть/закрыть меню по Escape
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (_isPaused)
                ResumeGame();
            else
                ShowPauseMenu();
        }
    }

    /// <summary>
    /// Показать меню паузы
    /// </summary>
    private void ShowPauseMenu()
    {
        _background.SetActive(true);
        _pausePanel.SetActive(true);
        Time.timeScale = 0f;
        _isPaused = true;

        // Отключаем мобов
        foreach (var mob in _mobs)
            if (mob != null) mob.enabled = false;

        // Разблокируем курсор только для обычной версии (не VR)
        if (!UnityEngine.XR.XRSettings.isDeviceActive)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    /// <summary>
    /// Продолжить игру
    /// </summary>
    private void ResumeGame()
    {
        _background.SetActive(false);
        _pausePanel.SetActive(false);
        Time.timeScale = 1f;
        _isPaused = false;

        // Включаем мобов обратно
        foreach (var mob in _mobs)
            if (mob != null) mob.enabled = true;

        // Блокируем курсор обратно для не-VR режима
        if (!UnityEngine.XR.XRSettings.isDeviceActive)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
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

    private void OnDestroy()
    {
        _resumeButton.onClick.RemoveListener(ResumeGame);
        _restartButton.onClick.RemoveListener(RestartGame);
        _exitButton.onClick.RemoveListener(ExitGame);
    }
}