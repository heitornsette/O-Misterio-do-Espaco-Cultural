using UnityEngine;

namespace Misterio.Gameplay
{
    /// <summary>
    /// Scrolls a background strip at a fraction of the camera speed, which is what gives the outdoor
    /// approach its depth. The layer is wider than the view and wraps horizontally.
    /// </summary>
    public sealed class ParallaxLayer : MonoBehaviour
    {
        [SerializeField, Range(0f, 1.5f)] float factor = 0.5f;
        [SerializeField] float wrapWidth = 0f;
        [SerializeField] Transform cameraTransform;
        [SerializeField] float baseY;
        [SerializeField] float baseX;

        float startCameraX;

        public void Configure(Transform target, float parallaxFactor, float width, Vector2 origin)
        {
            cameraTransform = target;
            factor = parallaxFactor;
            wrapWidth = width;
            baseX = origin.x;
            baseY = origin.y;
            startCameraX = target != null ? target.position.x : 0f;
            transform.position = new Vector3(baseX, baseY, transform.position.z);
        }

        void LateUpdate()
        {
            if (cameraTransform == null) return;
            float travelled = cameraTransform.position.x - startCameraX;
            float offset = travelled * factor;

            // Wrap so a finite strip can cover an endless walk.
            if (wrapWidth > 0.01f) offset = Mathf.Repeat(offset, wrapWidth);

            transform.position = new Vector3(baseX + offset, baseY, transform.position.z);
        }
    }
}
