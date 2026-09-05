using UnityEngine;

/// <summary>
/// Камера боевой комнаты: мягко следует за игроком, подаётся в сторону прицела
/// и принимает короткие направленные толчки (bump) от игровых событий.
/// </summary>
/// <remarks>
/// Осознанно НЕ содержит тряски экрана на рутинные события. По дизайн-документу
/// атаки врага всегда в визуальном приоритете, а при темпе стрельбы ~4 выстрела/сек
/// постоянная тряска забивает их телеграфы. Тряска — удел добиваний и крупных событий.
/// </remarks>
[RequireComponent(typeof(Camera))]
public class CameraRig : MonoBehaviour
{
    #region Настройки, отображаемые в инспекторе Unity
    /// <summary>
    /// Объект, за которым следует камера (обычно игрок).
    /// </summary>
    [Header("Слежение")]
    [Tooltip("За кем следим (обычно Player)")]
    [SerializeField]
    private Transform _target;

    /// <summary>
    /// Время сглаживания слежения, сек. Меньше — камера жёстче привязана к цели,
    /// больше — мягче и с большим отставанием.
    /// </summary>
    [Tooltip("Время сглаживания слежения, сек (меньше = жёстче привязка)")]
    [SerializeField]
    private float _followSmoothTime = 0.15f;

    /// <summary>
    /// Источник направления прицела. Если не задан — смещение к прицелу отключается.
    /// </summary>
    [Header("Смещение к прицелу (lookahead)")]
    [Tooltip("Источник прицела. Пусто = смещение к курсору отключено")]
    [SerializeField]
    private PlayerAim _playerAim;

    /// <summary>
    /// Какую долю расстояния до курсора камера отыгрывает смещением.
    /// 0 — камера строго на игроке, 0.5 — на полпути к курсору.
    /// </summary>
    [Tooltip("Доля расстояния до курсора (0 = без смещения)")]
    [Range(0f, 0.6f)]
    [SerializeField]
    private float _lookaheadFactor = 0.25f;

    /// <summary>
    /// Максимальное смещение к прицелу в юнитах. Не даёт камере уехать от игрока
    /// слишком далеко при курсоре у края экрана.
    /// </summary>
    [Tooltip("Максимальное смещение к прицелу, юниты")]
    [SerializeField]
    private float _lookaheadMaxDistance = 2f;

    /// <summary>
    /// Время сглаживания смещения к прицелу, сек. Держится заметно больше,
    /// чем у слежения — иначе камера дёргается за каждым рывком мыши.
    /// </summary>
    [Tooltip("Время сглаживания смещения к прицелу, сек")]
    [SerializeField]
    private float _lookaheadSmoothTime = 0.25f;

    /// <summary>
    /// Время затухания толчка, сек. За него смещение от bump возвращается к нулю.
    /// </summary>
    [Header("Толчок (bump)")]
    [Tooltip("Время затухания толчка, сек")]
    [SerializeField]
    private float _bumpRecovery = 0.12f;

    /// <summary>
    /// Ограничение накопленного толчка, юниты. Защита от «улёта» камеры
    /// при частых событиях подряд (например, стрельбе очередью).
    /// </summary>
    [Tooltip("Максимальное смещение от толчков, юниты")]
    [SerializeField]
    private float _bumpMaxDistance = 0.6f;
    #endregion

    /// <summary>
    /// Исходная глубина камеры по Z. В 2D её нельзя терять, иначе сцена пропадёт из виду.
    /// </summary>
    private float _depth;

    /// <summary>
    /// Текущая скорость слежения. Служебное состояние для <see cref="Vector2.SmoothDamp"/>.
    /// </summary>
    private Vector2 _followVelocity;

    /// <summary>
    /// Текущее (сглаженное) смещение к прицелу.
    /// </summary>
    private Vector2 _lookahead;

    /// <summary>
    /// Служебная скорость сглаживания смещения к прицелу.
    /// </summary>
    private Vector2 _lookaheadVelocity;

    /// <summary>
    /// Текущее смещение от толчков. Затухает к нулю каждый кадр.
    /// </summary>
    private Vector2 _bumpOffset;

    /// <summary>
    /// Запоминает глубину камеры и ставит её на цель без сглаживания,
    /// чтобы первый кадр не «подъезжал» из мировой точки (0;0).
    /// </summary>
    private void Awake()
    {
        _depth = transform.position.z;

        if (_target != null)
            transform.position = new Vector3(_target.position.x, _target.position.y, _depth);
    }

    /// <summary>
    /// Добавляет камере короткий направленный толчок.
    /// </summary>
    /// <param name="direction">Направление толчка (нормализуется внутри).</param>
    /// <param name="strength">Сила толчка в юнитах.</param>
    public void Bump(Vector2 direction, float strength)
    {
        if (direction.sqrMagnitude < 0.0001f)
            return;

        _bumpOffset = Vector2.ClampMagnitude(
            _bumpOffset + direction.normalized * strength,
            _bumpMaxDistance);
    }

    /// <summary>
    /// Двигает камеру в <see cref="LateUpdate"/> — после того, как физика и скрипты
    /// уже переставили игрока в этом кадре. Иначе камера следует за прошлым
    /// положением цели и картинка заметно дрожит.
    /// </summary>
    /// <remarks>
    /// Используется масштабируемое время (<see cref="Time.deltaTime"/>), поэтому
    /// во время хитстопа камера замирает вместе со всей сценой — так и задумано.
    /// </remarks>
    private void LateUpdate()
    {
        if (_target == null)
            return;

        UpdateLookahead();
        UpdateBump();

        Vector2 desiredPosition = (Vector2)_target.position + _lookahead + _bumpOffset;

        Vector2 smoothedPosition = Vector2.SmoothDamp(
            transform.position,
            desiredPosition,
            ref _followVelocity,
            _followSmoothTime);

        transform.position = new Vector3(smoothedPosition.x, smoothedPosition.y, _depth);
    }

    /// <summary>
    /// Пересчитывает смещение камеры в сторону прицела.
    /// Камера подаётся туда, куда игрок целится, открывая обзор на угрозу заранее.
    /// </summary>
    private void UpdateLookahead()
    {
        Vector2 desiredLookahead = Vector2.zero;

        if (_playerAim != null && _lookaheadFactor > 0f)
        {
            Vector2 toCursor = _playerAim.AimWorldPoint - (Vector2)_target.position;
            desiredLookahead = Vector2.ClampMagnitude(
                toCursor * _lookaheadFactor,
                _lookaheadMaxDistance);
        }

        _lookahead = Vector2.SmoothDamp(
            _lookahead,
            desiredLookahead,
            ref _lookaheadVelocity,
            _lookaheadSmoothTime);
    }

    /// <summary>
    /// Гасит накопленный толчок, возвращая камеру к «честному» положению.
    /// </summary>
    private void UpdateBump()
    {
        if (_bumpOffset.sqrMagnitude < 0.000001f)
        {
            _bumpOffset = Vector2.zero;
            return;
        }

        float recoverySpeed = _bumpMaxDistance / Mathf.Max(_bumpRecovery, 0.0001f);
        _bumpOffset = Vector2.MoveTowards(_bumpOffset, Vector2.zero, recoverySpeed * Time.deltaTime);
    }
}
