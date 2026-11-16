using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Misterio.Cutscenes;

namespace Misterio.Credits
{
    /// <summary>
    /// Rolagem cinematográfica dos créditos sobre o saguão noturno.
    /// Reaproveita o motivo do xilofone da CC01 via CC01Soundscape.
    /// Ao terminar, dispara onCreditsFinished para um menu futuro.
    /// </summary>
    public sealed class CreditsRollController : MonoBehaviour
    {
        const float ScrollDuration = 26f;
        const float EndFadeToBlack = 1f;
        const float EndHold = 3f;
        const float EndFadeOut = 1f;
        const float TotalDuration = ScrollDuration + EndFadeToBlack + EndHold + EndFadeOut;

        [Header("Scene")]
        [SerializeField] Camera sceneCamera;
        [SerializeField] RectTransform creditsRoot;
        [SerializeField] float scrollStartY = -320f;
        [SerializeField] float scrollEndY = 320f;
        [SerializeField] SpriteRenderer moth;
        [SerializeField] SpriteRenderer mothGlow;
        [SerializeField] SpriteRenderer[] dustMotes;

        [Header("UI")]
        [SerializeField] Text hint;
        [SerializeField] Text endCard;
        [SerializeField] Image vignetteImage;
        [SerializeField] Image fadeImage;

        [Header("Audio")]
        [SerializeField] AudioSource ambientSource;
        [SerializeField] AudioSource musicSource;

        [Header("Integration")]
        [SerializeField] UnityEvent onCreditsFinished = new UnityEvent();

        float elapsed;
        double lastRealtime;
        bool finished;
        Vector3 cameraStart;
        Vector3 mothStart;
        Vector3[] dustStarts;

        public bool Finished => finished;

        void Awake()
        {
            var soundscape = GetComponent<CC01Soundscape>();
            if (soundscape == null) soundscape = gameObject.AddComponent<CC01Soundscape>();
            if (sceneCamera == null) sceneCamera = Camera.main;
            cameraStart = sceneCamera != null ? sceneCamera.transform.position : Vector3.zero;
            mothStart = moth != null ? moth.transform.position : Vector3.zero;
            dustStarts = new Vector3[dustMotes != null ? dustMotes.Length : 0];
            for (int i = 0; i < dustStarts.Length; i++)
                dustStarts[i] = dustMotes[i].transform.position;

            if (creditsRoot != null)
            {
                var p = creditsRoot.localPosition;
                p.y = scrollStartY;
                creditsRoot.localPosition = p;
            }
            if (hint != null) hint.enabled = false;
            if (endCard != null) endCard.enabled = false;
            if (fadeImage != null) fadeImage.color = Color.black;
            if (vignetteImage != null) vignetteImage.color = new Color(1f, 1f, 1f, 0.85f);
            SetEnabled(dustMotes, true);
            if (moth != null) moth.enabled = true;
            if (mothGlow != null) mothGlow.enabled = true;

            lastRealtime = Time.realtimeSinceStartupAsDouble;

            if (ambientSource != null && soundscape.Ambient != null)
            {
                ambientSource.clip = soundscape.Ambient;
                ambientSource.loop = true;
                ambientSource.volume = 1f;
                ambientSource.Play();
            }
            if (musicSource != null && soundscape.Xylophone != null)
            {
                musicSource.clip = soundscape.Xylophone;
                musicSource.loop = true;
                musicSource.volume = 0.12f;
                musicSource.Play();
            }
        }

        void Update()
        {
            if (finished) return;
            HandleSkip();
            double now = Time.realtimeSinceStartupAsDouble;
            elapsed = Mathf.Min(TotalDuration, elapsed + (float)Math.Min(0.05, Math.Max(0.0, now - lastRealtime)));
            lastRealtime = now;
            UpdateScroll();
            UpdateCamera();
            UpdateMoth();
            UpdateDust();
            UpdateHint();
            UpdateEndCardAndFade();
            UpdateMusicFade();
            if (elapsed >= TotalDuration) Finish();
        }

        void HandleSkip()
        {
            if (elapsed >= ScrollDuration) return;
            bool pressed = false;
            var kb = Keyboard.current;
            if (kb != null && (kb.escapeKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame))
                pressed = true;
            var mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame)
                pressed = true;
            if (pressed)
            {
                elapsed = ScrollDuration - 0.05f;
                if (hint != null) hint.enabled = false;
            }
        }

        void UpdateScroll()
        {
            if (creditsRoot == null) return;
            float t = Mathf.Clamp01(elapsed / ScrollDuration);
            float y = Mathf.Lerp(scrollStartY, scrollEndY, t);
            var p = creditsRoot.localPosition;
            p.y = y;
            creditsRoot.localPosition = p;
        }

        void UpdateCamera()
        {
            if (sceneCamera == null) return;
            float t = Mathf.Clamp01(elapsed / TotalDuration);
            sceneCamera.orthographicSize = Mathf.Lerp(1f, 1.08f, t);
            var pos = cameraStart;
            pos.y += Mathf.Lerp(0f, -0.03f, t);
            sceneCamera.transform.position = pos;
        }

        void UpdateMoth()
        {
            if (moth == null) return;
            float t = Mathf.Clamp01(elapsed / TotalDuration);
            float x = Mathf.Lerp(-1.6f, 1.6f, t);
            float y = mothStart.y + Mathf.Sin(elapsed * 0.7f) * 0.05f;
            moth.transform.position = new Vector3(x, y, mothStart.z);
            moth.color = new Color(1f, 1f, 1f, 0.55f + 0.2f * Mathf.Sin(elapsed * 4f));
            if (mothGlow != null)
            {
                mothGlow.transform.position = moth.transform.position;
                mothGlow.color = new Color(1f, 0.78f, 0.42f, 0.16f + 0.12f * (0.5f + 0.5f * Mathf.Sin(elapsed * 3f)));
            }
        }

        void UpdateDust()
        {
            if (dustMotes == null) return;
            for (int i = 0; i < dustMotes.Length; i++)
            {
                var m = dustMotes[i];
                if (m == null) continue;
                var p = dustStarts[i];
                float y = p.y + Mathf.Repeat(elapsed * (0.05f + (i % 3) * 0.02f), 1.2f);
                m.transform.position = new Vector3(p.x + Mathf.Sin(elapsed * 0.3f + i) * 0.02f, y, p.z);
                m.color = new Color(0.82f, 0.84f, 0.76f, 0.16f + 0.1f * Mathf.Sin(elapsed * 0.8f + i));
            }
        }

        void UpdateHint()
        {
            if (hint == null) return;
            hint.enabled = elapsed > 3f && elapsed < ScrollDuration;
        }

        void UpdateEndCardAndFade()
        {
            float opening = elapsed < 2f ? 1f - Mathf.SmoothStep(0f, 1f, elapsed / 2f) : 0f;
            float endBlack = 0f;
            if (elapsed >= ScrollDuration)
                endBlack = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(ScrollDuration, ScrollDuration + EndFadeToBlack, elapsed));
            if (fadeImage != null)
                fadeImage.color = new Color(0f, 0f, 0f, Mathf.Max(opening, endBlack));

            if (endCard != null)
            {
                float cardIn = Mathf.InverseLerp(ScrollDuration + EndFadeToBlack * 0.5f, ScrollDuration + EndFadeToBlack + 0.4f, elapsed);
                float cardOut = Mathf.InverseLerp(TotalDuration - EndFadeOut, TotalDuration, elapsed);
                float alpha = Mathf.Clamp01(cardIn) * (1f - Mathf.Clamp01(cardOut));
                endCard.enabled = alpha > 0f;
                var c = endCard.color;
                c.a = alpha;
                endCard.color = c;
            }
        }

        void UpdateMusicFade()
        {
            if (musicSource == null) return;
            float fadeOutStart = TotalDuration - EndFadeOut - 0.5f;
            musicSource.volume = elapsed > fadeOutStart
                ? Mathf.Lerp(0.12f, 0f, Mathf.InverseLerp(fadeOutStart, TotalDuration, elapsed))
                : 0.12f;
        }

        void Finish()
        {
            finished = true;
            if (ambientSource != null) ambientSource.Stop();
            if (musicSource != null) musicSource.Stop();
            onCreditsFinished?.Invoke();
        }

        static void SetEnabled(SpriteRenderer[] renderers, bool value)
        {
            if (renderers == null) return;
            for (int i = 0; i < renderers.Length; i++)
                if (renderers[i] != null) renderers[i].enabled = value;
        }
    }
}
