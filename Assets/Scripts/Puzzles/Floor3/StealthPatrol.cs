using UnityEngine;

namespace Misterio.Puzzles.Floor3
{
    /// <summary>
    /// Deterministic patrol for Moema: fixed waypoints and constant speed, so the player can learn
    /// the pattern instead of guessing. Detection uses the dot product against the facing vector
    /// and a segment test against occluders (display cases), matching the design document.
    /// </summary>
    public sealed class StealthPatrol
    {
        readonly Vector2[] waypoints;
        readonly float speed;
        readonly float halfAngleDegrees;
        readonly float viewDistance;

        int currentIndex;
        Vector2 position;
        Vector2 facing;
        float waitTimer;
        readonly float waitAtWaypoint;
        float distractedTimer;

        public Vector2 Position => position;
        public Vector2 Facing => facing;
        public float HalfAngleDegrees => halfAngleDegrees;
        public float ViewDistance => viewDistance;
        public bool IsDistracted => distractedTimer > 0f;

        public StealthPatrol(Vector2[] waypoints, float speed, float viewDistance,
                             float halfAngleDegrees, float waitAtWaypoint = 0.4f)
        {
            this.waypoints = waypoints;
            this.speed = Mathf.Max(0.01f, speed);
            this.viewDistance = Mathf.Max(0.1f, viewDistance);
            this.halfAngleDegrees = Mathf.Clamp(halfAngleDegrees, 1f, 89f);
            this.waitAtWaypoint = Mathf.Max(0f, waitAtWaypoint);
            position = waypoints.Length > 0 ? waypoints[0] : Vector2.zero;
            currentIndex = waypoints.Length > 1 ? 1 : 0;
            facing = Vector2.right;
        }

        /// <summary>Moves along the loop; the path repeats forever at a constant speed.</summary>
        public void Tick(float deltaTime)
        {
            if (waypoints.Length < 2 || deltaTime <= 0f) return;

            if (distractedTimer > 0f)
            {
                distractedTimer = Mathf.Max(0f, distractedTimer - deltaTime);
                return;
            }

            if (waitTimer > 0f)
            {
                waitTimer = Mathf.Max(0f, waitTimer - deltaTime);
                return;
            }

            Vector2 target = waypoints[currentIndex];
            Vector2 delta = target - position;
            float distance = delta.magnitude;
            if (distance > 0.0001f) facing = delta / distance;

            float step = speed * deltaTime;
            if (step >= distance)
            {
                position = target;
                currentIndex = (currentIndex + 1) % waypoints.Length;
                waitTimer = waitAtWaypoint;
            }
            else
            {
                position += facing * step;
            }
        }

        /// <summary>
        /// A loose board makes noise: Moema stops and turns towards it. This is what makes the
        /// boards a real hazard instead of scenery, and it is fully deterministic.
        /// </summary>
        public void Distract(Vector2 source, float seconds)
        {
            Vector2 toSource = source - position;
            if (toSource.sqrMagnitude > 0.0001f) facing = toSource.normalized;
            distractedTimer = Mathf.Max(distractedTimer, seconds);
            waitTimer = 0f;
        }

        /// <summary>True when the point is inside the cone, in range and not hidden by an occluder.</summary>
        public bool CanSee(Vector2 point, Vector2[] occluders = null)
        {
            Vector2 toPoint = point - position;
            float distance = toPoint.magnitude;
            if (distance > viewDistance) return false;
            if (distance < 0.0001f) return true;

            Vector2 direction = toPoint / distance;
            float dot = Vector2.Dot(facing.normalized, direction);
            float cosLimit = Mathf.Cos(halfAngleDegrees * Mathf.Deg2Rad);
            if (dot < cosLimit) return false;

            if (occluders != null)
                foreach (Vector2[] segment in BuildSegments(occluders))
                    if (SegmentsIntersect(position, point, segment[0], segment[1])) return false;

            return true;
        }

        /// <summary>Occluders arrive as flat pairs: [a0,b0,a1,b1,...] describing blocking segments.</summary>
        static Vector2[][] BuildSegments(Vector2[] flat)
        {
            if (flat == null || flat.Length < 2) return new Vector2[0][];
            var result = new Vector2[flat.Length / 2][];
            for (int i = 0; i + 1 < flat.Length; i += 2)
                result[i / 2] = new[] { flat[i], flat[i + 1] };
            return result;
        }

        static bool SegmentsIntersect(Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p4)
        {
            float d1 = Cross(p3, p4, p1);
            float d2 = Cross(p3, p4, p2);
            float d3 = Cross(p1, p2, p3);
            float d4 = Cross(p1, p2, p4);
            return ((d1 > 0 && d2 < 0) || (d1 < 0 && d2 > 0)) &&
                   ((d3 > 0 && d4 < 0) || (d3 < 0 && d4 > 0));
        }

        static float Cross(Vector2 a, Vector2 b, Vector2 point) =>
            (b.x - a.x) * (point.y - a.y) - (b.y - a.y) * (point.x - a.x);
    }
}
