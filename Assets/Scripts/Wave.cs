using UnityEngine;

/// <summary>
/// Класс волны, представляющий летящий снаряд или атакующая волна.
/// </summary>
public class Wave : MonoBehaviour
{
    /// <summary>
    /// Коэффициент увеличения хитбокса относительно визуальной ширины волны.
    /// </summary>
    private const float HitboxGenerosity = 1.5f;

    /// <summary>
    /// Глубина (толщина) фронта волны.
    /// </summary>
    private const float FrontDepth = 0.4f;

    /// <summary>
    /// Спрайт-заглушка, который визуально представляет фронт волны.
    /// </summary>
    [SerializeField]
    private Transform _visual;

    /// <summary>
    /// Характеристики волны.
    /// </summary>
    private WaveStats _stats;

    /// <summary>
    /// Направление движения волны.
    /// </summary>
    private Vector2 _direction;

    /// <summary>
    /// Пройденное волной расстояние.
    /// </summary>
    private float _traveled;

    /// <summary>
    /// Количество пробитых целей.
    /// </summary>
    private int pierced;

    /// <summary>
    /// Физическое тело волны.
    /// </summary>
    private Rigidbody2D _rigidbody;

    /// <summary>
    /// Инициализация параметров волны.
    /// </summary>
    /// <param name="stats">Характеристики волны.</param>
    /// <param name="direction">Направление движения.</param>
    public void Init(WaveStats stats, Vector2 direction)
    {
        _stats = stats;
        _direction = direction.normalized;
        transform.right = _direction;

        _rigidbody = GetComponent<Rigidbody2D>();

        var box = GetComponent<BoxCollider2D>();
        box.isTrigger = true;
        box.size = new Vector2(FrontDepth, _stats.visualWidth * HitboxGenerosity);

        if (_visual != null)
            _visual.localScale = new Vector3(FrontDepth, _stats.visualWidth, 1f);
    }

    /// <summary>
    /// Обновление физики: перемещение волны и проверка пройденного расстояния.
    /// </summary>
    private void FixedUpdate()
    {
        var step = _stats.speed * Time.fixedDeltaTime;
        _rigidbody.MovePosition(_rigidbody.position + _direction * step);
        _traveled += step;

        if(_traveled >= _stats.length)
            Destroy(gameObject);
    }

    /// <summary>
    /// Обработка столкновения волны с препятствиями и целями.
    /// </summary>
    /// <param name="other">Коллайдер объекта, с которым столкнулась волна.</param>
    private void OnTriggerEnter2D(Collider2D other)
    {
        var dummy = other.GetComponent<Dummy>();
        if (dummy != null)
            return;

        var damage = _stats.damage;
        if(_stats.distanceFalloff > 0f)
            damage *= 1f - _stats.distanceFalloff * (_traveled / _stats.length);

        for (int i = 0; i < pierced; i++)
            damage *= 1f - _stats.pierceFalloff;

        dummy.TakeHit(damage, _direction);

        pierced++;

        if(pierced > _stats.pierceCount)
            Destroy(gameObject);
    }
}
