using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Класс манекена (цели), который может получать урон.
/// </summary>
public class Dummy : MonoBehaviour
{
    /// <summary>
    /// Максимальное количество здоровья.
    /// </summary>
    [Header("Здоровье")]
    [SerializeField]
    private float _maxHealth = 100f;

    [Header("Реакция на попадание")]
    /// <summary>
    /// Спрайт, который вспыхивает белым при попадании.
    /// </summary>
    [Tooltip("Спрайт, который вспыхивает белым при попадании")]
    [SerializeField]
    private SpriteRenderer _spriteRenderer;

    /// <summary>
    /// Длительность вспышки белого цвета при попадании (сек.).
    /// </summary>
    [Tooltip("Длительность вспышки белого цвета при попадании (сек.)")]
    [SerializeField]
    private float _flashDuration = 0.05f;

    /// <summary>
    /// Длительность хитстопа при попадании (сек.).
    /// </summary>
    [Tooltip("Длительность хитстопа при попадании (сек.)")]
    [SerializeField]
    private float _hitStopDuration = 0.04f;

    /// <summary>
    /// Звуковой эффект при попадании. Воспроизводится случайный звук из набора.
    /// </summary>
    [SerializeField]
    private RandomSound _hitSound;

    /// <summary>
    /// Текущее количество здоровья манекена.
    /// </summary>
    private float _health;

    /// <summary>
    /// Изначальный цвет спрайта.
    /// </summary>
    private Color _baseColor;

    /// <summary>
    /// Текущая запущенная корутина вспышки.
    /// </summary>
    private Coroutine _flashCoroutone;

    /// <summary>
    /// Инициализация компонентов манекена до запуска игры.
    /// </summary>
    private void Awake()
    {
        _health = _maxHealth;

        if(_spriteRenderer != null )
            _baseColor = _spriteRenderer.color;
    }

    /// <summary>
    /// Обработка получения урона: здоровье, вспышка, хитстоп.
    /// </summary>
    /// <param name="damage">Количество урона.</param>
    /// <param name="fromDirection">Направление, откуда пришёл урон (для будущего отброса врагов).</param>
    public void TakeHit(float damage, Vector2 fromDirection)
    {
        _health -= damage;

        Debug.Log($"{name}: -{damage:F0} → HP {_health:F0}");

        Flash();

        if(TimeController.Instance != null)
            TimeController.Instance.HitStop(_hitStopDuration);

        if(_hitSound != null)
            _hitSound.Play();

        if (_health <= 0f)
            _health = _maxHealth;
    }

    /// <summary>
    /// Запускает (или перезапускает) короткую белую вспышку спрайта.
    /// </summary>
    private void Flash()
    {
        if (_spriteRenderer == null)
            return;

        if(_flashCoroutone != null)
            StopCoroutine(_flashCoroutone);

        _flashCoroutone = StartCoroutine(FlashCoroutine());
    }

    /// <summary>
    /// Корутина для эффекта вспышки (мигания белым цветом) манекена.
    /// </summary>
    private IEnumerator FlashCoroutine()
    {
        _spriteRenderer.color = Color.white;

        // виден даже во время хитстопа
        yield return new WaitForSecondsRealtime(_flashDuration);

        _spriteRenderer.color = _baseColor;
        _flashCoroutone = null;
    }
}
