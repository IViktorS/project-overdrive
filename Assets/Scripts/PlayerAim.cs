using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Вычисляет позицию и направление прицеливания персонажа на основе положения курсора мыши.
/// Предоставляет данные о мировых координатах прицела, векторе направления и индексе направления для других компонентов.
/// </summary>
public class PlayerAim : MonoBehaviour
{
    /// <summary>
    /// Ссылка на камеру, используемую для перевода экранных координат в мировые.
    /// </summary>
    private Camera _camera;

    /// <summary>
    /// Точка в мировых координатах, куда в данный момент направлен прицел (курсор мыши).
    /// Используется для стрельбы или применения способностей в нужном направлении.
    /// </summary>
    public Vector2 AimWorldPoint {  get; private set; }

    /// <summary>
    /// Нормализованный вектор направления от персонажа к точке прицеливания <see cref="AimWorldPoint"/>.
    /// </summary>
    public Vector2 AimDirection { get; private set; }

    /// <summary>
    /// Индекс направления прицеливания (от 0 до 7), где 0 — вправо, 1 — вправо-вверх, 
    /// 2 — вверх и так далее против часовой стрелки.
    /// Используется для выбора нужного направления в системе анимаций или спрайтов.
    /// </summary>
    public int FacingIndex { get; private set; }

    /// <summary>
    /// Инициализация начальных компонентов. Присваивает Камеру, если она не задана.
    /// </summary>
    public void Awake()
    {
        if(_camera == null)
            _camera = Camera.main;
    }

    /// <summary>
    /// Вызывается каждый кадр. 
    /// Считывает позицию мыши и обновляет координаты, вектор и индекс направления прицеливания.
    /// </summary>
    void Update()
    {
        // В будущем здесь можно добавить поддержку геймпада и других устройств ввода,
        // но в данный момент обрабатывается только мышь.
        if (Mouse.current == null)
            return;

        Vector2 screenPosition = Mouse.current.position.ReadValue();

        // Для 2D-ортографической камеры координата Z игнорируется.
        AimWorldPoint = _camera.ScreenToWorldPoint(screenPosition);

        AimDirection = (AimWorldPoint - (Vector2)transform.position).normalized;

        if(AimDirection.sqrMagnitude < 0.0001f)
            return;

        float angle = Mathf.Atan2(AimDirection.y, AimDirection.x) * Mathf.Rad2Deg;

        if(angle < 0f)
            angle += 360f;

        FacingIndex = Mathf.RoundToInt(angle / 45f) % 8;
    }
}
