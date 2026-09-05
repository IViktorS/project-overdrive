using UnityEngine;

/// <summary>
/// Компонент для случайного воспроизведения аудиоклипов со случайным изменением высоты тона.
/// Требует наличия компонента <see cref="AudioSource"/> на игровом объекте.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class RandomSound : MonoBehaviour
{
    /// <summary>
    /// Массив звуковых клипов, из которых случайным образом выбирается один для воспроизведения.
    /// </summary>
    [Tooltip("Набор звуковых клипов для случайного воспроизведения")]
    [SerializeField]
    private AudioClip[] _clips;

    /// <summary>
    /// Разброс (отклонение) высоты звука (pitch). Например, 0.03 означает случайное отклонение на ±3%.
    /// </summary>
    [Tooltip("Разброс высоты звука, доля. Например, 0.03 = ±3%")]
    [SerializeField]
    private float _pitchVariance = 0.03f;

    /// <summary>
    /// Ссылка на компонент <see cref="AudioSource"/>, прикрепленный к данному объекту.
    /// </summary>
    private AudioSource _audioSource;

    /// <summary>
    /// Признак того, что об отсутствии клипов уже сообщено.
    /// Нужен, чтобы предупреждение не повторялось на каждом вызове <see cref="Play"/>.
    /// </summary>
    private bool _missingClipsReported;

    /// <summary>
    /// Инициализирует компонент, получая ссылку на <see cref="AudioSource"/>.
    /// </summary>
    private void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
    }

    /// <summary>
    /// Выбирает случайный аудиоклип из массива <see cref="_clips"/>,
    /// применяет случайную вариацию высоты тона (в пределах <see cref="_pitchVariance"/>)
    /// и единоразово воспроизводит его.
    /// </summary>
    public void Play()
    {
        if (_clips == null || _clips.Length == 0)
        {
            // Предупреждаем один раз: это ошибка настройки, а не событие геймплея.
            // Логировать на каждый вызов нельзя — Play() срабатывает несколько раз в секунду.
            if (!_missingClipsReported)
            {
                _missingClipsReported = true;
                Debug.LogWarning($"{name}: RandomSound — не задан ни один клип", this);
            }

            return;
        }

        var clip = _clips[Random.Range(0, _clips.Length)];

        // Массив может быть нужного размера, но с пустыми слотами — так уже случалось
        // со звуками попадания, и Unity молча сыпал "PlayOneShot was called with a
        // null AudioClip", а звук просто не играл.
        if (clip == null)
        {
            if (!_missingClipsReported)
            {
                _missingClipsReported = true;
                Debug.LogWarning($"{name}: RandomSound — в массиве клипов есть пустые слоты", this);
            }

            return;
        }

        _audioSource.pitch = 1f + Random.Range(-_pitchVariance, _pitchVariance);
        _audioSource.PlayOneShot(clip);
    }
}
