using UnityEngine;
namespace Brainrot
{
    public enum IslandZone { Beach, Meadows, Forest, Ruins, Swamp }

    /// <summary>The same deterministic map is used by scenery, movement, AI, spawning and the map UI.</summary>
    public static class IslandMap
    {
        public const float Radius = 22f;
        public static Vector3 StartPosition => new Vector3(0, .15f, 0);
        public static Vector3 Center(IslandZone zone)
        {
            switch (zone)
            {
                case IslandZone.Beach: return new Vector3(0, 0, -65);
                case IslandZone.Forest: return new Vector3(-48, 0, 35);
                case IslandZone.Ruins: return new Vector3(46, 0, 40);
                case IslandZone.Swamp: return new Vector3(55, 0, -26);
                default: return new Vector3(0, 0, -12);
            }
        }
        public static IslandZone GetZone(Vector3 position)
        {
            return IslandZone.Beach;
        }
        public static string Name(IslandZone zone)
        {
            switch (zone)
            {
                case IslandZone.Beach: return "Остров";
                case IslandZone.Meadows: return "Луга";
                case IslandZone.Forest: return "Лес Патапима";
                case IslandZone.Ruins: return "Макаронные руины";
                default: return "Болото Бомбардиро";
            }
        }
        public static UnityEngine.Color Color(IslandZone zone)
        {
            switch (zone)
            {
                case IslandZone.Beach: return new UnityEngine.Color(.98f,.80f,.43f);
                case IslandZone.Meadows: return new UnityEngine.Color(.48f,.75f,.24f);
                case IslandZone.Forest: return new UnityEngine.Color(.17f,.48f,.26f);
                case IslandZone.Ruins: return new UnityEngine.Color(.70f,.72f,.34f);
                default: return new UnityEngine.Color(.31f,.44f,.31f);
            }
        }
        public static EnemyKind EnemyForZone(IslandZone zone)
        {
            switch (zone)
            {
                case IslandZone.Meadows: return EnemyKind.LiriliLarila;
                case IslandZone.Forest: return EnemyKind.BrrBrrPatapim;
                case IslandZone.Ruins: return EnemyKind.CappuccinoAssassino;
                case IslandZone.Swamp: return EnemyKind.BombardiroCrocodilo;
                default: return EnemyKind.Normal;
            }
        }
        public static float CoastRadius(float angleRadians)
        {
            return Radius;
        }
        public static bool IsWalkable(Vector3 position, float margin = 2)
        {
            if (float.IsNaN(position.x) || float.IsNaN(position.z) || float.IsInfinity(position.x) || float.IsInfinity(position.z)) return false;
            float radius = Mathf.Max(0, CoastRadius(Mathf.Atan2(position.z, position.x)) - Mathf.Max(0, margin));
            return position.x * position.x + position.z * position.z <= radius * radius;
        }
        public static Vector3 ClampToLand(Vector3 position, float margin = 2)
        {
            if (float.IsNaN(position.x) || float.IsNaN(position.z) || float.IsInfinity(position.x) || float.IsInfinity(position.z)) return new Vector3(0, position.y, 0);
            Vector2 plane = new Vector2(position.x, position.z);
            float radius = Mathf.Max(0, CoastRadius(Mathf.Atan2(position.z, position.x)) - Mathf.Max(0, margin));
            if (plane.sqrMagnitude > radius * radius) { plane = plane.normalized * radius; position.x = plane.x; position.z = plane.y; }
            return position;
        }
        public static Vector3 ClampToZone(Vector3 position, IslandZone zone, float margin = 2)
        {
            position = ClampToLand(position, margin);
            if (GetZone(position) == zone) return position;
            Vector3 center = Center(zone); center.y = position.y;
            // Voronoi cells are convex. The center-to-target segment has one cell boundary.
            Vector3 inside = center, outside = position;
            for (int i = 0; i < 18; i++)
            {
                Vector3 middle = (inside + outside) * .5f;
                if (GetZone(middle) == zone && IsWalkable(middle, margin)) inside = middle;
                else outside = middle;
            }
            // Slightly inset from the shared edge prevents floating-point oscillation.
            Vector3 inset = Vector3.MoveTowards(inside, center, .05f);
            return ClampToLand(inset, margin);
        }
        public static bool TrySpawnPoint(IslandZone zone, Vector3 player, float minDistance, float maxDistance, out Vector3 point)
        {
            float minimum = Mathf.Max(2, minDistance), maximum = Mathf.Max(minimum, maxDistance);
            for (int i = 0; i < 64; i++)
            {
                float angle = Random.Range(0, Mathf.PI * 2);
                float radius = Mathf.Sqrt(Random.Range(minimum * minimum, maximum * maximum));
                Vector3 candidate = player + new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * radius;
                candidate.y = .02f;
                if (IsWalkable(candidate, 4) && GetZone(candidate) == zone) { point = candidate; return true; }
            }
            point = Vector3.zero;
            return false;
        }
    }
}
