using UnityEngine;

namespace Game.Scripts.Music
{
    /// <summary>Minimal play/stop wrapper around one AudioSource; scene buttons call <see cref="PlayMusic"/> and <see cref="StopMusic"/>.</summary>
    public class MusicController : MonoBehaviour
    {
        [Tooltip("Audio source that plays the game's music track.")]
        [SerializeField] private AudioSource musicSource;

        private void Awake()
        {
            if (InspectorValidation.RequireField(musicSource, nameof(musicSource), nameof(MusicController), this))
            {
                enabled = false;
            }
        }

        /// <summary>Starts the music unless it is already playing.</summary>
        public void PlayMusic()
        {
            if (!musicSource.isPlaying)
            {
                musicSource.Play();
            }
        }

        /// <summary>Stops the music.</summary>
        public void StopMusic()
        {
            musicSource.Stop();
        }
    }
}
