using UnityEngine;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour
{
    [Header("Здоровье")]
    [Tooltip("Максимальное здоровье (сколько ударов выдерживает)")]
    [SerializeField] private int _maxHealth = 3;

    [Tooltip("Слайдер UI для отображения здоровья")]
    [SerializeField] private Slider _healthSlider;

    [Header("Экран смерти")]
    [Tooltip("Скрипт экрана смерти")]
    [SerializeField] private DeathScreen _deathScreen;

    private int _currentHealth;

    private void Start()
    {
        _currentHealth = _maxHealth;

        // Настраиваем слайдер
        if (_healthSlider != null)
        {
            _healthSlider.maxValue = _maxHealth;
            _healthSlider.value = _currentHealth;
        }
    }

    /// <summary>
    /// Получить урон. Вызывается мобом при атаке.
    /// </summary>
    public void TakeDamage(int damage)
    {
        _currentHealth -= damage;

        // Обновляем слайдер
        if (_healthSlider != null)
            _healthSlider.value = _currentHealth;

        // Проверяем смерть
        if (_currentHealth <= 0)
            Die();
    }

    private void Die()
    {
        if (_deathScreen != null)
            _deathScreen.ShowDeathScreen();
    }

    public int GetCurrentHealth() => _currentHealth;
    public int GetMaxHealth() => _maxHealth;
}