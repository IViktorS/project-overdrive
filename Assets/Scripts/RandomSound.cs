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
            Debug.Log($"{name}: RandomSound — клипов нет");
            return;
        } 
            

        var clip = _clips[Random.Range(0, _clips.Length)];
        _audioSource.pitch = 1f + Random.Range(-_pitchVariance, _pitchVariance);
        _audioSource.PlayOneShot(clip);

        Debug.Log($"{name}: играю {clip.name}");
    }
}
