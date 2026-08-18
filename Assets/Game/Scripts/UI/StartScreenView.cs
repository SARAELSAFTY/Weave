using System;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Scripts.UI
{
    public class StartScreenView : MonoBehaviour
    {
        [SerializeField, Tooltip("Button that begins the run.")] private Button playButton;
        [SerializeField, Tooltip("Button that quits the game.")] private Button quitButton;

        public event Action PlayRequested;
        public event Action QuitRequested;

        private void Awake()
        {
            if (playButton == null)
            {
                Debug.LogError($"[StartScreenView] Missing required Inspector reference '{nameof(playButton)}' on '{gameObject.name}'.", this);
                enabled = false;
                return;
            }

            if (quitButton == null)
            {
                Debug.LogError($"[StartScreenView] Missing required Inspector reference '{nameof(quitButton)}' on '{gameObject.name}'.", this);
                enabled = false;
                return;
            }

            playButton.onClick.AddListener(RequestPlay);
            quitButton.onClick.AddListener(RequestQuit);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        public void Show()
        {
            gameObject.SetActive(true);
        }

        private void RequestPlay()
        {
            PlayRequested?.Invoke();
        }

        private void RequestQuit()
        {
            QuitRequested?.Invoke();
        }
    }
}
