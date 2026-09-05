using UnityEngine;

/// <summary>
/// Визуальный индикатор (заглушка или отладочный элемент), отображающий текущее направление прицеливания.
/// Поворачивает указанный объект так, чтобы он указывал в сторону курсора мыши.
/// </summary>
public class AimIndicator : MonoBehaviour
{
    /// <summary>
    /// Ссылка на компонент прицеливания игрока, из которого берётся направление.
    /// </summary>
    [SerializeField]
    private PlayerAim _playerAim;

    /// <summary>
    /// Объект-указатель, который будет вращаться в сторону направления прицеливания.
    /// </summary>
    [SerializeField]
    private Transform _indicator;

    /// <summary>
    /// Проверяет, что обязательные ссылки заданы. Без этого компонент падал бы
    /// с NullReferenceException каждый кадр, что маскирует настоящую причину —
    /// незаполненное поле в инспекторе.
    /// </summary>
    private void Awake()
    {
        if (_playerAim == null || _indicator == null)
        {
            Debug.LogWarning($"{name}: AimIndicator — не заданы PlayerAim и/или Indicator, компонент отключён", this);
            enabled = false;
        }
    }

    /// <summary>
    /// Вызывается каждый кадр.
    /// Обновляет поворот индикатора в соответствии с текущим направлением прицела.
    /// </summary>
    private void Update()
    {
        Vector2 direction = _playerAim.AimDirection;

        if(direction.sqrMagnitude < 0.0001f)
            return;

        var angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        _indicator.rotation = Quaternion.Euler(0f, 0f, angle);
    }
}
