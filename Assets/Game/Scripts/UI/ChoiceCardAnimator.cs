using System.Collections;
using UnityEngine;

namespace Game.Scripts.UI
{
    /// <summary>Animates a single choice card between its parked, revealed and dismissed poses.</summary>
    /// <remarks>Left and right cards use mirrored, hand-tuned motion sets so each side joins and throws differently.</remarks>
    public class ChoiceCardAnimator : MonoBehaviour
    {
        [Tooltip("Minimum drag progress (0-1) before the choice card begins to reveal (deadzone).")]
        [SerializeField, Range(0f, 0.3f)]
        private float revealThreshold = 0.05f;

        [Tooltip("Time in seconds the card takes to ease between its parked and revealed poses.")]
        [SerializeField, Min(0.05f)]
        private float joinDuration = 0.25f;

        [Tooltip("Total time in seconds of the confirm animation: dip, then throw off-screen.")]
        [SerializeField, Min(0.05f)]
        private float confirmDuration = 0.65f;

        [Tooltip("Fraction of Confirm Duration spent dipping before the card is thrown.")]
        [SerializeField, Range(0.1f, 0.9f)]
        private float confirmDipFraction = 0.45f;

        /// <summary>Offset and tilt values describing one side's card motion.</summary>
        private struct SideMotion
        {
            public Vector2 joinOffset;
            public float joinTilt;
            public Vector2 dipOffset;
            public Vector2 dismissOffset;
            public float dismissTilt;
        }

        // Hand-tuned mirrored motion sets so left and right choices feel balanced and symmetrical.
        private static readonly SideMotion LeftMotion = new SideMotion
        {
            joinOffset = new Vector2(480f, -120f),
            joinTilt = -30f,
            dipOffset = new Vector2(-20f, -60f),
            dismissOffset = new Vector2(1500f, 1600f),
            dismissTilt = -40f
        };

        private static readonly SideMotion RightMotion = new SideMotion
        {
            joinOffset = new Vector2(-480f, -120f),
            joinTilt = 30f,
            dipOffset = new Vector2(20f, -60f),
            dismissOffset = new Vector2(-1500f, 1600f),
            dismissTilt = 40f
        };

        private RectTransform cardTransform;
        private CanvasGroup canvasGroup;
        private Vector2 parkPosition;
        private SideMotion motion;
        private float reveal;
        private float target;
        private bool dismissing;
        private Coroutine dismissRoutine;

        /// <summary>True while the confirm-dismiss animation is in flight.</summary>
        public bool IsDismissing => dismissing;

        private void Awake()
        {
            cardTransform = GetComponent<RectTransform>();
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }

            parkPosition = cardTransform.anchoredPosition;
            motion = parkPosition.x <= 0f ? LeftMotion : RightMotion;
            ApplyReveal(0f);
        }

        /// <summary>Updates the card reveal pose proportionally with the drag progress.</summary>
        /// <param name="progress">Drag progress from 0 (parked) to 1 (fully dragged).</param>
        public void SetRevealProgress(float progress)
        {
            if (dismissing)
            {
                return;
            }

            float clamped = Mathf.Clamp01(progress);
            if (clamped <= revealThreshold)
            {
                target = 0f;
            }
            else
            {
                target = revealThreshold < 1f ? (clamped - revealThreshold) / (1f - revealThreshold) : 0f;
            }

            reveal = target;
            ApplyReveal(reveal);
        }

        /// <summary>Eases the card back to its parked pose after an unconfirmed drag.</summary>
        public void EaseBackToPark()
        {
            if (dismissing)
            {
                return;
            }

            target = 0f;
        }

        /// <summary>Immediately resets the card to its parked pose, cancelling any dismissal in flight.</summary>
        public void SnapToPark()
        {
            StopDismiss();
            dismissing = false;
            reveal = 0f;
            target = 0f;
            ApplyReveal(0f);
        }

        /// <summary>Starts the confirm animation: the card dips, then throws off-screen.</summary>
        public void PlayConfirm()
        {
            StopDismiss();
            dismissing = true;
            ApplyReveal(1f);
            dismissRoutine = StartCoroutine(ConfirmDismissRoutine());
        }

        private void StopDismiss()
        {
            if (dismissRoutine != null)
            {
                StopCoroutine(dismissRoutine);
                dismissRoutine = null;
            }
        }

        private void Update()
        {
            if (dismissing || Mathf.Approximately(reveal, target))
            {
                return;
            }

            reveal = Mathf.MoveTowards(reveal, target, Time.deltaTime / joinDuration);
            ApplyReveal(reveal);
        }

        /// <summary>Plays the dip-then-throw dismissal over Confirm Duration seconds, then hides the card.</summary>
        private IEnumerator ConfirmDismissRoutine()
        {
            Vector2 joined = parkPosition + motion.joinOffset;
            Vector2 dip = joined + motion.dipOffset;
            Vector2 end = joined + motion.dismissOffset;
            float elapsed = 0f;
            float dipTime = confirmDuration * confirmDipFraction;

            while (elapsed < confirmDuration)
            {
                elapsed += Time.deltaTime;
                Vector2 position;
                if (elapsed < dipTime)
                {
                    float t = Mathf.Clamp01(elapsed / dipTime);
                    float eased = 1f - (1f - t) * (1f - t);
                    position = Vector2.Lerp(joined, dip, eased);
                }
                else
                {
                    float t = Mathf.Clamp01((elapsed - dipTime) / (confirmDuration - dipTime));
                    float eased = t * t;
                    position = Vector2.Lerp(dip, end, eased);
                }

                cardTransform.anchoredPosition = position;
                cardTransform.localRotation = Quaternion.Euler(0f, 0f,
                    Mathf.Lerp(motion.joinTilt, motion.dismissTilt, Mathf.Clamp01(elapsed / confirmDuration)));
                yield return null;
            }

            canvasGroup.alpha = 0f;
            dismissing = false;
            dismissRoutine = null;
            reveal = 0f;
            target = 0f;
            ApplyReveal(0f);
        }

        private void ApplyReveal(float value)
        {
            // Smoothstep so the join eases in and out instead of moving linearly.
            float smooth = value * value * (3f - 2f * value);
            cardTransform.anchoredPosition = Vector2.Lerp(parkPosition, parkPosition + motion.joinOffset, smooth);
            cardTransform.localRotation = Quaternion.Euler(0f, 0f, motion.joinTilt * smooth);
            canvasGroup.alpha = smooth;
        }
    }
}
