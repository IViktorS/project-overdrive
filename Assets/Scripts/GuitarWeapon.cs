using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Оружие игрока — гитара. Создаёт волновую атаку по нажатию ЛКМ
/// с ограничением темпа (кулдаун) и буферизацией ввода.
/// </summary>
public class GuitarWeapon : MonoBehaviour
{
    #region Настройки, отображаемые в инспекторе Unity
    /// <summary>
    /// Префаб волны, создаваемый при выстреле.
    /// </summary>
    [Header("Ссылки")]
    [Tooltip("Префаб волны (объект со скриптом Wave)")]
    [SerializeField]
    private Wave _wavePrefab;

    /// <summary>
    /// Компонент прицеливания, из которого берётся направление выстрела.
    /// </summary>
    [Tooltip("Источник направления выстрела")]
    [SerializeField]
    private PlayerAim _playerAim;

    /// <summary>
    /// Точка появления волны. Если не задана - берется позиция самого оружия.
    /// </summary>
    [Tooltip("Откуда вылетает волна. Пусто = позиция этого объекта")]
    [SerializeField]
    private Transform _muzzle;

    /// <summary>
    /// Базовые характеристики волны. Педали будут менять копию этих значений, а не шаблон.
    /// </summary>
    [Header("Характеристика волны")]
    [SerializeField]
    private WaveStats _baseStats = new();

    /// <summary>
    /// Минимальный интервал между выстрелами, сек (темп стрельбы).
    /// ~0.23–0.28 c ≈ восьмые на 110–130 BPM (метал-темп из дизайн-документа).
    /// </summary>
    [Header("Темп")]
    [Tooltip("Кулдаун между выстрелами, сек (~0.23 - 0.28)")]
    [SerializeField]
    private float _fireCooldown = 0.25f;

    /// <summary>
    /// Окно буферизации ввода, сек. Клик чуть раньше готовности не теряется,
    /// а срабатывает сразу по окончании кулдауна.
    /// </summary>
    [Tooltip("Насколько заранее засчитывается клик (сек)")]
    [SerializeField]
    private float _inputBuffer = 0.1f;

    /// <summary>
    /// Звуковой эффект выстрела. Воспроизводится случайный звук из набора.
    /// </summary>
    [SerializeField]
    private RandomSound _shotSound;
    #endregion

    /// <summary>
    /// Сгенерированный класс ввода.
    /// </summary>
    private InputSystem_Actions _controls;

    /// <summary>
    /// Момент времени, начиная с которого можно выстрелятить снова.
    /// </summary>
    private float _nextFireTime;

    /// <summary>
    /// Момент последнего нажатия ЛКМ (для буферизации).
    /// Большое отрицательное значение означает, что необработанного нажатия нет.
    /// </summary>
    private float _lastPressTime = -999f;

    /// <summary>
    /// Инициализация компонентов при загрузке скрипта.
    /// </summary>
    private void Awake()
    {
        _controls = new InputSystem_Actions();

        if(_muzzle == null)
            _muzzle = transform;
    }

    /// <summary>
    /// Активация ввода и подписка на нажатие стрельбы.
    /// </summary>
    private void OnEnable()
    {
        _controls.Player.Enable();
        _controls.Player.Attack.performed += OnAttackPressed;
    }

    /// <summary>
    /// Отписка от нажатия стрельбы и деактивация ввода.
    /// </summary>
    private void OnDisable()
    {
        _controls.Player.Attack.performed -= OnAttackPressed;
        _controls.Player.Disable();
    }

    /// <summary>
    /// Запоминает момент нажатия. Решение о выстреле принимается в Update
    /// что бы учесть кулдаун и буфер.
    /// </summary>
    /// <param name="context">Контекст события ввода.</param>
    private void OnAttackPressed(InputAction.CallbackContext context)
    {
        _lastPressTime = Time.time;
    }

    /// <summary>
    /// Каждый кадр проверяет, можно ли стрелять (кулдаун + буфер) 
    /// и вызывает Fire() при необходимости.
    /// </summary>
    private void Update()
    {
        // Кулдаун ещё не истёк.
        if (Time.time < _nextFireTime)
            return;

        // Нет клика в пределах окна буферизации (или он уже израсходован).
        if (Time.time - _lastPressTime > _inputBuffer)
            return;

        Fire();
    }

    /// <summary>
    /// Создает волну, назначает кулдаун и сбрасывает буфер. 
    /// </summary>
    private void Fire()
    {
        // Создаём копию характеристик волны, чтобы педали могли её модифицировать.
        var waveStats = _baseStats.Clone();

        var wave = Instantiate(_wavePrefab, _muzzle.position, Quaternion.identity);
        wave.Initialization(waveStats, _playerAim.AimDirection);

        _nextFireTime = Time.time + _fireCooldown;
        _lastPressTime = -999f; // сброс буфера

        if(_shotSound != null)
            _shotSound.Play();

        // Сюда позже ляжет game feel: отдача, вспышка, гильза-медиатор.
    }
}
