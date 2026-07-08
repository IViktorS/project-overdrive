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
    /// Вызывается каждый кадр.
    /// Обновляет поворот индикатора в соответствии с текущим направлением прицела.
    /// </summary>
    void Update()
    {
        Vector2 direction = _playerAim.AimDirection;

        if(direction.sqrMagnitude < 0.0001f)
            return;

        var angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        _indicator.rotation = Quaternion.Euler(0f, 0f, angle);
    }
}
