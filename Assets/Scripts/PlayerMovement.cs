using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Управляет передвижением игрового персонажа с использованием новой системы ввода (Input System).
/// Реализует обычное перемещение и механику рывка (dash) с кадрами неуязвимости (i-frames).
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    #region Настройки движения и рывка отображаемые в инспекторе Unity
    /// <summary>
    /// Скорость ходьбы персонажа в юнитах в секунду.
    /// </summary>
    [Header("Движение")]
    [Tooltip("Скорость ходьбы, units/сек")]
    [SerializeField]
    private float _moveSpeed = 6f;

    /// <summary>
    /// Ускорение персонажа (как быстро он достигает максимальной скорости). 
    /// Большое значение обеспечивает мгновенный разгон, низкое — эффект "скольжения".
    /// </summary>
    [Tooltip("Как быстро набирает скорость. Большое значение = мгновенно, маленькое = 'скользко'")]
    [SerializeField]
    private float _acceleration = 90f;

    /// <summary>
    /// Торможение персонажа (как быстро он останавливается при отпускании кнопок перемещения). 
    /// Большое значение обеспечивает мгновенную остановку, низкое — инерционное скольжение.
    /// </summary>
    [Tooltip("Как быстро скорость гасится при отпускании клавиш")]
    [SerializeField]
    private float _deceleration = 120f;

    /// <summary>
    /// Скорость персонажа во время рывка в юнитах в секунду.
    /// </summary>
    [Header("Рывок")]
    [Tooltip("Скорость рывка, units/сек")]
    [SerializeField]
    private float _dashSpeed = 22f;

    /// <summary>
    /// Длительность рывка в секундах. Общая дистанция рывка рассчитывается как <see cref="_dashSpeed"/> * <see cref="_dashDuration"/>.
    /// </summary>
    [Tooltip("Длительность рывка, сек. Дистанция рывка = dashSpeed * dashDuration")]
    [SerializeField]
    private float _dashDuration = 0.15f;

    /// <summary>
    /// Время перезарядки (cooldown): интервал в секундах от начала одного рывка до возможности использовать следующий.
    /// </summary>
    [Tooltip("Перезарядка: время от начала рывка до следующего, сек")]
    [SerializeField]
    private float _dashCooldown = 0.6f;

    /// <summary>
    /// Длительность кадров неуязвимости (i-frames) после начала рывка в секундах.
    /// Обычно устанавливается чуть больше, чем сама длительность рывка.
    /// </summary>
    [Tooltip("Длительность неуязвимости. Обычно чуть больше dashDuration")]
    [SerializeField]
    private float _iFrameDuration = 0.18f;
    #endregion

    /// <summary>
    /// Компонент твёрдого тела для управления физикой перемещения.
    /// </summary>
    private Rigidbody2D _rigidbody;

    /// <summary>
    /// Экземпляр сгенерированного класса для работы с новой системой ввода.
    /// </summary>
    private InputSystem_Actions _controls;

    /// <summary>
    /// Текущий нормализованный вектор ввода направления (WASD/стики).
    /// </summary>
    private Vector2 _moveInput;

    /// <summary>
    /// Направление последнего сделанного шага. 
    /// Используется для того, чтобы персонаж делал рывок в ту же сторону, куда шел,
    /// даже если игрок уже отпустил клавиши движения.
    /// </summary>
    private Vector2 _lastMoveDirection = Vector2.right;

    /// <summary>
    /// Возможные состояния перемещения персонажа.
    /// </summary>
    private enum State
    {
        /// <summary>Обычное движение, контролируемое вводом игрока.</summary>
        Normal,
        /// <summary>Выполнение рывка.</summary>
        Dashing
    }

    /// <summary>
    /// Текущее состояние перемещения персонажа.
    /// </summary>
    private State _state = State.Normal;

    /// <summary>
    /// Время, оставшееся до окончания текущего рывка.
    /// </summary>
    private float _dashTimeLeft;

    /// <summary>
    /// Время, оставшееся до завершения перезарядки (cooldown), после которой можно начать новый рывок.
    /// </summary>
    private float _dashCooldownLeft;

    /// <summary>
    /// Время, оставшееся до окончания текущего периода неуязвимости (i-frames).
    /// </summary>
    private float _iFrameTimeLeft;

    /// <summary>
    /// Направление, в котором совершается текущий рывок. Фиксируется при его старте.
    /// </summary>
    private Vector2 _dashDirection;

    /// <summary>
    /// Флаг проверки на неуязвимость персонажа.
    /// Если true — урон должен игнорироваться, если false — урон наносится обычным образом.
    /// </summary>
    private bool _isInvulnerable => _iFrameTimeLeft > 0f;

    /// <summary>
    /// Инициализация компонентов при загрузке скрипта.
    /// </summary>
    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody2D>();
        _controls = new InputSystem_Actions();
    }

    /// <summary>
    /// Активация ввода и подписка на события при включении объекта.
    /// </summary>
    private void OnEnable()
    {
        _controls.Player.Enable();

        // Подписка на событие нажатия кнопки рывка.
        // Указывает явно, что действие выполнено (performed), а не начато (started) или отменено (canceled).
        _controls.Player.Dash.performed += OnDashPressed;
    }

    /// <summary>
    /// Отключение ввода и отписка от событий при выключении объекта.
    /// </summary>
    private void OnDisable()
    {
        _controls.Player.Dash.performed -= OnDashPressed;
        _controls.Player.Disable();
    }

    /// <summary>
    /// Считывание пользовательского ввода каждый кадр для предотвращения пропуска нажатий клавиш.
    /// </summary>
    private void Update()
    {
        _moveInput = _controls.Player.Move.ReadValue<Vector2>();
        
        // Ограничиваем длину вектора до 1, чтобы передвижение по диагонали не было быстрее.
        if(_moveInput.sqrMagnitude > 1f)
        {
            _moveInput.Normalize(); 
        }

        // Запоминаем последнее направление движения, если персонаж реально двигается.
        // Это предотвращает запись нулевого вектора при отпускании клавиш.
        if (_moveInput.sqrMagnitude > 0.01f)
        {
            _lastMoveDirection = _moveInput; 
        }       
    }

    /// <summary>
    /// Применение передвижения и управление таймерами на шаге физического движка.
    /// </summary>
    private void FixedUpdate()
    {
        var deltaTime = Time.fixedDeltaTime;

        if (_dashCooldownLeft > 0f)
            _dashCooldownLeft -= deltaTime;

        if(_iFrameTimeLeft > 0f)
            _iFrameTimeLeft -= deltaTime;

        switch (_state)
        {
            case State.Normal:
                MoveNormal(deltaTime);
                break;
            case State.Dashing:
                TickDash(deltaTime); 
                break;
        }
    }

    /// <summary>
    /// Применяет расчет движения игрока в нормальном состоянии с учетом ускорения и торможения.
    /// </summary>
    /// <param name="deltaTime">Прошедшее с последнего физического обновления время в секундах.</param>
    private void MoveNormal(float deltaTime)
    {
        var target = _moveInput * _moveSpeed;

        // При наличии ввода разгоняемся, при отпускании — тормозим.
        var rate = (_moveInput.sqrMagnitude > 0.01f) ? _acceleration : _deceleration;
        _rigidbody.linearVelocity = Vector2.MoveTowards(_rigidbody.linearVelocity, target, rate * deltaTime);
    }

    /// <summary>
    /// Обработчик события совершения рывка (Dash) из Input System. Проверяет возможность выполнения рывка.
    /// </summary>
    /// <param name="context">Контекст события пользовательского ввода.</param>
    private void OnDashPressed(InputAction.CallbackContext context)
    {
        // Нельзя сделать еще один рывок во время текущего рывка или до окончания перезарядки
        if (_state != State.Normal || _dashCooldownLeft > 0f)
            return; 

        StartDash();
    }

    /// <summary>
    /// Инициализация и запуск состояния рывка: расчет направления, установка таймеров и начального импульса.
    /// </summary>
    private void StartDash()
    {
        // Направление задается по текущему вводу. Если ввода нет (игрок стоит) — по направлению последнего шага.
        _dashDirection = (_moveInput.sqrMagnitude > 0.01f) ? _moveInput : _lastMoveDirection;
        _state = State.Dashing;
        _dashTimeLeft = _dashDuration;
        _dashCooldownLeft = _dashCooldown;
        _iFrameTimeLeft = _iFrameDuration;
        _rigidbody.linearVelocity = _dashDirection * _dashSpeed;
    }

    /// <summary>
    /// Обновление логики во время состояния рывка: поддержание постоянной скорости и выход из состояния.
    /// </summary>
    /// <param name="deltaTime">Прошедшее с последнего физического обновления время в секундах.</param>
    private void TickDash(float deltaTime)
    {
        _dashTimeLeft -= deltaTime;

        // Постоянное поддержание скорости рывка дает предсказуемую и стабильную пройденную дистанцию
        _rigidbody.linearVelocity = _dashDirection * _dashSpeed;

        if(_dashTimeLeft <= 0f)
            _state = State.Normal; // Возврат в обычное состояние после окончания времени рывка
    }
}