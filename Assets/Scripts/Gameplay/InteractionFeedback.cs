using UnityEngine;

namespace Misterio.Gameplay
{
    /// <summary>
    /// The floating marker and prompt that tell the player what can be used. It follows the guard's
    /// nearest interactable, bobs gently and pulses, and it writes the object's own prompt into the
    /// HUD so the player always knows both what and how.
    /// </summary>
    public sealed class InteractionFeedback : MonoBehaviour
    {
        [SerializeField] SpriteRenderer marker;
        [SerializeField] HudView hud;
        [SerializeField] float bobAmplitude = 0.018f;
        [SerializeField] float bobSpeed = 2.4f;
        [SerializeField] float pulseSpeed = 3.2f;

        GuardActor guard;
        Interactable current;
        Vector3 markerBase;
        float time;

        public static InteractionFeedback Create(Sprite markerSprite, HudView hudView, GuardActor actor)
        {
            var host = new GameObject("Feedback de interação");
            var feedback = host.AddComponent<InteractionFeedback>();
            feedback.hud = hudView;
            feedback.guard = actor;

            var view = new GameObject("Marcador");
            view.transform.SetParent(host.transform, false);
            var renderer = view.AddComponent<SpriteRenderer>();
            renderer.sprite = markerSprite;
            renderer.sortingOrder = 60;
            renderer.enabled = false;
            feedback.marker = renderer;
            return feedback;
        }

        void LateUpdate()
        {
            if (guard == null) return;
            Interactable nearest = guard.Nearest;

            if (nearest != current)
            {
                current = nearest;
                if (marker != null)
                {
                    marker.enabled = current != null;
                    if (current != null)
                    {
                        // Sit the marker above the object, with a small offset so it never covers it.
                        Vector2 point = current.InteractPoint;
                        markerBase = new Vector3(point.x, point.y + current.MarkerHeight, 0f);
                        marker.transform.position = markerBase;
                    }
                }
                hud?.SetPrompt(current != null ? current.Prompt : string.Empty);
            }

            if (marker == null || !marker.enabled || current == null) return;

            time += Time.deltaTime;
            Vector2 target = current.InteractPoint;
            markerBase = new Vector3(target.x, target.y + current.MarkerHeight, 0f);

            float bob = Mathf.Sin(time * bobSpeed) * bobAmplitude;
            marker.transform.position = markerBase + new Vector3(0f, bob, 0f);

            // A soft pulse keeps the marker readable without flashing.
            float pulse = 0.82f + 0.18f * Mathf.Sin(time * pulseSpeed);
            var color = marker.color;
            color.a = pulse;
            marker.color = color;
        }
    }
}
