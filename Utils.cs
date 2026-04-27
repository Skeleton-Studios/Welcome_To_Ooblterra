using UnityEngine;
using UnityEngine.AI;

namespace Welcome_To_Ooblterra
{
    class Utils
    {
        public static NavMeshHit? GetRandomNavMeshPositionInRadius(Vector3 pos, float radius = 10f)
        {
            float y = pos.y;
            pos = Random.insideUnitSphere * radius + pos;
            pos.y = y;
            if (NavMesh.SamplePosition(pos, out NavMeshHit navHit, radius, -1))
            {
                return navHit;
            }
            return null;
        }

        public static NavMeshHit? GetRandomNavMeshPositionInRadiusExtended(Vector3 pos, float radius = 10f, float radiusExpansionFactor = 2.0f, int maxIterations = 3) 
        {
            for(int i = 0; i < maxIterations; i++) 
            {
                float expandedRadius = radius * Mathf.Pow(radiusExpansionFactor, i);
                var navHit = GetRandomNavMeshPositionInRadius(pos, expandedRadius);
                if (navHit.HasValue) 
                {
                    return navHit;
                }
            }

            return null;
        }
    }
}
