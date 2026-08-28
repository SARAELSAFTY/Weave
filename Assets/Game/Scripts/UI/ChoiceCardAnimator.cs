using System.Collections;
using UnityEngine;

namespace Game.Scripts.UI
{
    /// <summary>
    /// Code-driven choice card feel, reproducing the original clip motion. The authored scene
    /// position is the parked (off-screen) pose; dragging toward this card triggers a smooth
    /// join-in tween, releasing eases it back out, and confirming tosses it away dismissively.
    /// Left/right motion constants are taken from the original Hover/Confirm clips.
    /// </summary>
    public class ChoiceCardAnimator : MonoBehaviour
    {
        [SerializeField, Range(0.05f, 0.5f), Tooltip("Drag progress at which this card starts joining in.")]
        private float revealThreshold = 0.15f;

        [SerializeField, Min(0.05f), Tooltip("Seconds for the join-in / ease-out tween.")]
        private float joinDuration = 0.25f;

        [SerializeField, Min(0.05f), Tooltip("Seconds for the confirm toss.")]
        private float confirmDuration = 0.5f;

        private struct SideMotion
        {
            public Vector2 joinOffset;
            public float joinTilt;
            public Vector2 dismissOffset;
            public float dismissTilt;
        }

        private static readonly SideMotion LeftMotion = new SideMotion
        {
            joinOffset = new Vector2(482f, -125f),
            joinTilt = -30f,
            dismissOffset = new Vector2(1534f, 1650f),
            dismissTilt = -42f
        };

        private static readonly SideMotion RightMotion = new SideMotion
        {
            joinOffset = new Vector2(-483f, -116f),
            joinTilt = 30f,
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

        private void Awake()
        {
            cardTransform = GetComponent<RectTransform>();
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }

            // The legacy clip-driven Animator rewrites root position/rotation every frame
            // and would fight the code-driven tween.
            Animator legacyAnimator = GetComponent<Animator>();
            if (legacyAnimator != null)
            {
                Destroy(legacyAnimator);
            }

            parkPosition = cardTransform.anchoredPosition;
            motion = parkPosition.x <= 0f ? LeftMotion : RightMotion;
            ApplyReveal(0f);
        }

        /// <summary>Feeds this side's drag progress; crossing the threshold tweens the card in, dropping below tweens it out.</summary>
        public void SetRevealProgress(float progress)
        {
            if (dismissing)
            {
                return;
            }

            target = Mathf.Clamp01(progress) > revealThreshold ? 1f : 0f;
        }

        /// <summary>Eases the card back to its parked, invisible state (cancelled drag).</summary>
        public void EaseBackToPark()
        {
            if (dismissing)
            {
                return;
            }

            target = 0f;
        }

        /// <summary>Instantly returns the card to its parked, invisible state.</summary>
        public void SnapToPark()
        {
            StopDismiss();
            dismissing = false;
            reveal = 0f;
            target = 0f;
            ApplyReveal(0f);
        }

        /// <summary>Reveals the card fully, then tosses it away dismissively (chosen side).</summary>
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

        private IEnumerator ConfirmDismissRoutine()
        {
            Vector2 joined = parkPosition + motion.joinOffset;
            float elapsed = 0f;

            while (elapsed < confirmDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / confirmDuration);
                float eased = t * t;
                cardTransform.anchoredPosition = Vector2.Lerp(joined, joined + motion.dismissOffset, eased);
                cardTransform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(motion.joinTilt, motion.dismissTilt, eased));
                yield return null;
            }

            canvasGroup.alpha = 0f;
            dismissRoutine = null;
        }

        private void ApplyReveal(float value)
        {
            float smooth = value * value * (3f - 2f * value);
            cardTransform.anchoredPosition = Vector2.Lerp(parkPosition, parkPosition + motion.joinOffset, smooth);
            cardTransform.localRotation = Quaternion.Euler(0f, 0f, motion.joinTilt * smooth);
            canvasGroup.alpha = smooth;
        }
    }
}
