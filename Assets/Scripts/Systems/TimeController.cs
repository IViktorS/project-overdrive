using System.Collections;
using UnityEngine;

/// <summary>
/// Глобальное управление игровым временем. Отвечает за хитстоп (hit-stop) —
/// кратковременную полную остановку времени в момент попадания, усиливающую «вес» удара.
/// </summary>
public class TimeController : MonoBehaviour
{
    /// <summary>
    /// Единственный экземпляр (простой синглтон), чтобы вызывать хитстоп из любого места.
    /// </summary>
    public static TimeController Instance { get; private set; }

    /// <summary>
    /// Корутина текущего хитстопа. Хранится, чтобы новый удар перезапускал эффект, 
    /// а не накладывал их.
    /// </summary>
    private Coroutine _coroutine;

    /// <summary>
    /// Инициализирует синглтон, уничтожая дубликаты.
    /// </summary>
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    /// <summary>
    /// Останавливает время на заданное число РЕАЛЬНЫХ секунд, затем возвращает нормальный ход.
    /// </summary>
    /// <param name="seconds">Длительность заморозки в реальном времени (напр. 0.04 = 40 мс).</param>
    public void HitStop(float seconds)
    {
        // Если уже есть активный хитстоп, останавливаем его.
        if (_coroutine != null)
            StopCoroutine(_coroutine);

        // Запускаем новый хитстоп.
        _coroutine = StartCoroutine(HitStopCoroutine(seconds));
    }

    /// <summary>
    /// Корутина для кратковременной остановки времени.
    /// Замораживает счетчик времени, ждет указанное количество реальных секунд 
    /// и восстанавливает ход времени.
    /// </summary>
    /// <param name="seconds">Время остановки в реальных секундах.</param>
    private IEnumerator HitStopCoroutine(float seconds)
    {
        // Полная остановка времени.
        // Физика и анимация будут заморожены, но корутины продолжат работать.
        Time.timeScale = 0f;

        // Ждем заданное количество реальных секунд.
        // Не зависимо от Time.timeScale, поэтому используем WaitForSecondsRealtime.
        yield return new WaitForSecondsRealtime(seconds);

        Time.timeScale = 1f;
        _coroutine = null;
    }
}
