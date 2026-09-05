using UnityEngine;

public class MusicController : MonoBehaviour
{
    public AudioSource musicSource;

    // Call this method from the Play Button
    public void PlayMusic()
    {
        if (!musicSource.isPlaying)
        {
            musicSource.Play();
        }
    }

    // Call this method from the Stop Button
    public void StopMusic()
    {
        musicSource.Stop();
    }
}