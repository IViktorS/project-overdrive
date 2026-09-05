using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Оружие игрока — гитара. Создаёт волновую атаку по нажатию ЛКМ
/// с ограничением темпа (кулдаун) и буферизацией ввода.
/// </summary>
public class GuitarWeapon : MonoBehaviour
{
    /// <summary>
    /// Способ ведения огня.
    /// </summary>
    public enum FireMode
    {
        /// <summary>Один клик — один выстрел. Удержание ЛКМ ничего не даёт.</summary>
        Click,

        /// <summary>Удержание ЛКМ ведёт непрерывный огонь с темпом кулдауна.</summary>
        Hold
    }

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
    /// Способ ведения огня. Отладочная настройка: переключается прямо в Play-режиме,
    /// чтобы сравнить ощущения и выбрать окончательный вариант.
    /// </summary>
    /// <remarks>
    /// Дизайн-документ описывает выстрел как «удар по струнам» — это довод за
    /// <see cref="FireMode.Click"/>. Но жанр (Isaac, Gungeon) приучил игроков
    /// держать кнопку, а док же упоминает, что «спам расшатывает кучность»,
    /// подразумевая возможность частой стрельбы. Решается только плейтестом.
    /// </remarks>
    [Header("Отладка / эксперимент")]
    [Tooltip("Click = один клик один выстрел; Hold = удержание даёт автоогонь с тем же темпом")]
    [SerializeField]
    private FireMode _fireMode = FireMode.Click;

    /// <summary>
    /// Звуковой эффект выстрела. Воспроизводится случайный звук из набора.
    /// </summary>
    [SerializeField]
    private RandomSound _shotSound;

    [Header("Визуальная отдача")]
    /// <summary>
    /// Спрайт игрока, который дёргается при выстреле (дочерний Visual)
    /// </summary>
    [Tooltip("Спрайт игрока, который дёргается при выстреле (дочерний Visual)")]
    [SerializeField]
    private Transform _visual;

    /// <summary>
    /// Сила отдачи (смещение спрайта игрока)
    /// </summary>
    [Tooltip("Сила отдачи (смещение спрайта игрока)")]
    [SerializeField]
    private float _recoilDistance = 0.15f;

    /// <summary>
    /// Скорость возврата спрайта игрока в исходное положение, сек
    /// </summary>
    [Tooltip("Скорость возврата спрайта игрока в исходное положение, сек")]
    [SerializeField]
    private float _recoilRecovery = 0.08f;

    /// <summary>
    /// Камера, получающая толчок при выстреле. Пусто = толчка нет.
    /// </summary>
    [Tooltip("Камера, получающая толчок при выстреле. Пусто = без толчка")]
    [SerializeField]
    private CameraRig _cameraRig;

    /// <summary>
    /// Сила толчка камеры при выстреле, юниты. Держать МАЛЕНЬКОЙ: при темпе
    /// ~4 выстрела/сек заметный толчок превращается в постоянную тряску
    /// и мешает читать телеграфы атак врага.
    /// </summary>
    [Tooltip("Сила толчка камеры при выстреле, юниты (держать маленькой)")]
    [SerializeField]
    private float _cameraBumpStrength = 0.12f;
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
    /// Текущее смещение спрайта от отдачи. Плавно возвращается к нулю.
    /// </summary>
    private Vector2 _recoilOffset;

    /// <summary>
    /// Момент последнего нажатия ЛКМ (для буферизации).
    /// <see cref="float.NegativeInfinity"/> означает, что необработанного нажатия нет.
    /// </summary>
    private float _lastPressTime = float.NegativeInfinity;

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

        if (!WantsToFire())
            return;

        Fire();
    }

    /// <summary>
    /// Есть ли сейчас намерение выстрелить, с учётом выбранного <see cref="FireMode"/>.
    /// </summary>
    /// <remarks>
    /// Буфер ввода работает в обоих режимах: клик, сделанный чуть раньше окончания
    /// кулдауна, не теряется, а срабатывает сразу по его истечении. Без этого
    /// ритмичная стрельба «в темп» ощущается как проглатывание нажатий.
    /// </remarks>
    private bool WantsToFire()
    {
        // Нажатие, попавшее в окно буферизации, засчитывается в любом режиме.
        if (Time.time - _lastPressTime <= _inputBuffer)
            return true;

        // В режиме удержания продолжаем стрелять, пока кнопка зажата.
        return _fireMode == FireMode.Hold && _controls.Player.Attack.IsPressed();
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
        _lastPressTime = float.NegativeInfinity; // буфер израсходован

        if(_shotSound != null)
            _shotSound.Play();

        // Толкаем спрайт назад — против направления выстрела.
        _recoilOffset = - _playerAim.AimDirection * _recoilDistance;

        // Толчок камеры туда же, куда уходит отдача — против выстрела.
        if (_cameraRig != null)
            _cameraRig.Bump(-_playerAim.AimDirection, _cameraBumpStrength);

        // Сюда позже ляжет game feel: вспышка, гильза-медиатор.
    }

    /// <summary>
    /// Плавное возвращение спрайта игрока в исходное положение после визуальной отдачи.
    /// Вызывается в LateUpdate, чтобы смещать уже позиционированный объект.
    /// </summary>
    private void LateUpdate()
    {
        if(_visual == null)
            return;

        var speed = _recoilDistance / _recoilRecovery;
        _recoilOffset = Vector2.MoveTowards(_recoilOffset, Vector2.zero, speed * Time.deltaTime);
        _visual.localPosition = _recoilOffset;
    }
}
