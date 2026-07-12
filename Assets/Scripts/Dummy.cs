using UnityEngine;

/// <summary>
/// Класс манекена (цели), который может получать урон.
/// </summary>
public class Dummy : MonoBehaviour
{
    /// <summary>
    /// Максимальное количество здоровья.
    /// </summary>
    [SerializeField]
    private float _maxHealth = 100f;

    /// <summary>
    /// Текущее количество здоровья манекена.
    /// </summary>
    private float _health;

    /// <summary>
    /// Инициализация компонентов манекена до запуска игры.
    /// </summary>
    private void Awake()
    {
        _health = _maxHealth;
    }

    /// <summary>
    /// Обработка получения урона.
    /// </summary>
    /// <param name="damage">Количество получаемого урона.</param>
    /// <param name="fromDirection">Направление, откуда был нанесён урон.</param>
    public void TakeHit(float damage, Vector2 fromDirection)
    {
        _health -= damage;

        Debug.Log($"{name}: -{damage:F0} → HP {_health:F0}");

        if (_health < 0f)
            _health = _maxHealth;
    }
}
