using System.Collections;
using UnityEngine;

namespace Game.Scripts.UI
{
    /// <summary>Animates a single choice card between its parked, revealed and dismissed poses.</summary>
    /// <remarks>Left and right cards use mirrored, hand-tuned motion sets so each side joins and throws differently.</remarks>
    public class ChoiceCardAnimator : MonoBehaviour
    {
        [Tooltip("Drag progress (0-1) beyond which the card counts as revealed and eases fully into its join pose.")]
        [SerializeField, Range(0.05f, 0.5f)]
        private float revealThreshold = 0.15f;

        [Tooltip("Time in seconds the card takes to ease between its parked and revealed poses.")]
        [SerializeField, Min(0.05f)]
        private float joinDuration = 0.25f;

        [Tooltip("Total time in seconds of the confirm animation: dip, then throw off-screen.")]
        [SerializeField, Min(0.05f)]
        private float confirmDuration = 0.8f;

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

        // Hand-tuned motion values reproducing the original card feel; the two sides are intentionally asymmetric.
        private static readonly SideMotion LeftMotion = new SideMotion
        {
            joinOffset = new Vector2(482f, -125f),
            joinTilt = -30f,
            dipOffset = new Vector2(-20f, -71f),
            dismissOffset = new Vector2(1534f, 1650f),
            dismissTilt = -42f
        };

        private static readonly SideMotion RightMotion = new SideMotion
        {
            joinOffset = new Vector2(-483f, -116f),
            joinTilt = 30f,
            dipOffset = new Vector2(22f, -49f),
            dismissOffset = new Vector2(-809f, 1333f),
            dismissTilt = 31f
        };

        private RectTransform cardTransform;
        private CanvasGroup canvasGroup;
        private Vector2 parkPosition;
        private SideMotion motion;
        private float reveal;
        private float target;
        private bool dismissing;
        private Coroutine dismissRoutine;

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

        /// <summary>Marks the card revealed (eases to the join pose) once drag progress passes the reveal threshold, parked otherwise.</summary>
        /// <param name="progress">Drag progress from 0 (parked) to 1 (fully dragged).</param>
        public void SetRevealProgress(float progress)
        {
            if (dismissing)
            {
                return;
            }

            target = Mathf.Clamp01(progress) > revealThreshold ? 1f : 0f;
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

            float direction = Mathf.Sign(target - reveal);
            reveal = direction > 0f
                ? Mathf.Min(target, reveal + Time.deltaTime / joinDuration)
                : Mathf.Max(target, reveal - Time.deltaTime / joinDuration);
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
