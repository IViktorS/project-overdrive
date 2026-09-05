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
    /// Масштаб времени, к которому нужно вернуться после хитстопа.
    /// Запоминается перед заморозкой, а не хардкодится единицей: иначе хитстоп
    /// молча отменил бы паузу или замедление, если они появятся.
    /// </summary>
    private float _normalTimeScale = 1f;

    /// <summary>
    /// Инициализирует синглтон, уничтожая дубликаты.
    /// </summary>
    /// <remarks>
    /// <see cref="DontDestroyOnLoad"/> нужен, потому что по дизайн-документу игра
    /// состоит из комнат: при их загрузке обычный объект сцены был бы уничтожен,
    /// и хитстоп перестал бы работать после первого же перехода.
    /// </remarks>
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// Снимает заморозку, если объект уничтожают прямо во время хитстопа
    /// (например, при выходе из Play-режима). Иначе время осталось бы стоять.
    /// </summary>
    private void OnDestroy()
    {
        if (Instance != this)
            return;

        if (_coroutine != null)
            Time.timeScale = _normalTimeScale;

        Instance = null;
    }

    /// <summary>
    /// Останавливает время на заданное число РЕАЛЬНЫХ секунд, затем возвращает нормальный ход.
    /// </summary>
    /// <param name="seconds">Длительность заморозки в реальном времени (напр. 0.04 = 40 мс).</param>
    public void HitStop(float seconds)
    {
        if (seconds <= 0f)
            return;

        if (_coroutine != null)
        {
            // Уже в хитстопе: гасим старую корутину, но НЕ перезаписываем
            // _normalTimeScale — сейчас там ноль, и мы бы застряли в заморозке навсегда.
            StopCoroutine(_coroutine);
        }
        else
        {
            _normalTimeScale = Time.timeScale;
        }

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

        Time.timeScale = _normalTimeScale;
        _coroutine = null;
    }
}
