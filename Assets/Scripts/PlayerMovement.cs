using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Перемещение персонажа и рывок (dash) для twin-stick roguelite.
/// Отвечает за: чтение WASD, движение через физику, рывок с кадрами неуязвимости (i-frames).
/// Поворот спрайта к курсору вынесен в отдельный скрипт PlayerAim.
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
    private float moveSpeed = 6f;

    /// <summary>
    /// Как быстро персонаж набирает скорость. 
    /// Большое значение = мгновенно, маленькое = медленно.
    /// </summary>
    [Tooltip("Как быстро набирает скорость. Большое значение = мгновенно, маленькое = 'скользко'")]
    [SerializeField]
    private float acceleration = 90f;

    /// <summary>
    /// Как быстро персонаж теряет скорость при отпускании клавиш. 
    /// Большое значение = мгновенно, маленькое = медленно.
    /// </summary>
    [Tooltip("Как быстро скорость гасится при отпускании клавиш")]
    [SerializeField]
    private float deceleration = 120f;

    /// <summary>
    /// Cкорость рывка персонажа в юнитах в секунду.
    /// </summary>
    [Header("Рывок")]
    [Tooltip("Скорость рывка, units/сек")]
    [SerializeField]
    private float dashSpeed = 22f;

    /// <summary>
    /// Длительность рывка в секундах. Дистанция рывка = dashSpeed * dashDuration.
    /// </summary>
    [Tooltip("Длительность рывка, сек. Дистанция рывка = dashSpeed * dashDuration")]
    [SerializeField]
    private float dashDuration = 0.15f;

    /// <summary>
    /// Перезфрядка: время от начала рывка до следующего, сек.
    /// </summary>
    [Tooltip("Перезфрядка: время от начала рывка до следующего, сек")]
    [SerializeField]
    private float dashCooldown = 0.6f;

    /// <summary>
    /// Длительность неуязвимости (i-frames) после начала рывка, сек.
    /// </summary>
    [Tooltip("Длительность неуязвимости. Обычно чуть больше dashDuration")]
    [SerializeField]
    private float iFrameDuration = 0.18f;
    #endregion

    private Rigidbody2D rb;

    /// <summary>
    /// Cгенерированный класс из .inputactions
    /// </summary>
    private InputSystem_Actions controls;

    /// <summary>
    /// Текущий ввод WASD (нормализованный)
    /// </summary>
    private Vector2 moveInput;

    /// <summary>
    /// Куда персонаж двигался в прошлый кадр. 
    /// Используется для рывка, чтобы персонаж рывком продолжал двигаться в том же направлении, 
    /// даже если игрок отпустил клавиши.
    /// </summary>
    private Vector2 lastMoveDirection = Vector2.right;

    /// <summary>
    /// Состояние персонажа: обычное или рывок.
    /// </summary>
    private enum State
    {
        Normal,
        Dashing
    }

    private State state = State.Normal;

    /// <summary>
    /// Cколько ещё длится текущий рывок
    /// </summary>
    private float dashTimeLeft;

    /// <summary>
    /// Cколько ещё осталось до следующего рывка (перезарядка)
    /// </summary>
    private float dashCooldownLeft;

    /// <summary>
    /// Cколько ещё длится текущая неуязвимость (i-frames)
    /// </summary>
    private float iFrameTimeLeft;

    /// <summary>
    /// Направление рывка, фиксируется на старте.
    /// </summary>
    private Vector2 dashDirection;

    /// <summary>
    /// Для системы урона: 
    /// true = персонаж в данный момент неуязвим (i-frames), 
    /// false = урон наносится.
    /// </summary>
    private bool IsInvulnerable => iFrameTimeLeft > 0f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        controls = new InputSystem_Actions();
    }

    private void OnEnable()
    {
        controls.Player.Enable();

        // Подписка на событие нажатия кнопки рывка.
        // Указывает явно, что действие выполнено (performed), а не начато (started) или отменено (canceled).
        controls.Player.Dash.performed += OnDashPressed;
    }

    private void OnDisable()
    {
        controls.Player.Dash.performed -= OnDashPressed;
        controls.Player.Disable();
    }

    // Ввод читаем в Update (каждый кадр отрисовки) — так не пропускаем нажатия.
    private void Update()
    {
        moveInput = controls.Player.Move.ReadValue<Vector2>();
        
        if(moveInput.sqrMagnitude > 1f)
        {
            // Ограничиваем длину вектора до 1, чтобы диагонали не были быстрее.
            moveInput.Normalize(); 
        }

        // Запоминаем последнее направление движения, если персонаж реально двигается.
        // Предотвращает запоминание нулевого вектора, когда игрок отпустил клавиши
        // или стик геймпада немного двигается около нуля.
        if (moveInput.sqrMagnitude > 0.01f)
        {
            // Запоминаем последнее направление движения
            lastMoveDirection = moveInput; 
        }       
    }

    private void OnDashPressed(InputAction.CallbackContext context)
    {
        throw new NotImplementedException();
    }
}