using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace Base
{
    public static class StaticUtil
    {
        /// <summary>Wrapper for your pooling solution integration.</summary>
        public static Object Spawn(Object original, Vector3 position, Quaternion rotation)
        {
            // Sweet pooling code here
            return Object.Instantiate(original, position, rotation);
        }

        /// <summary>Wrapper for your pooling solution integration.</summary>
        public static void DeSpawn(Object target)
        {
            // Sweet pooling code here
            Object.Destroy(target);
        }

        public static void DeSpawn(Object target, float time)
        {
            Object.Destroy(target, time);
        }

        public static IEnumerator DestroyInternal(GameObject target, float time)
        {
            yield return new WaitForSeconds(time);

            NetworkServer.Destroy(target);
            yield return null;
        }

        /// <summary> Checks to see if a GameObject is on a layer in a LayerMask. </summary>
        /// <returns> True if the provided LayerMask contains the layer of the GameObject provided. </returns>
        public static bool LayerMatchTest(LayerMask approvedLayers, GameObject objInQuestion)
        {
            return ((1 << objInQuestion.layer) & approvedLayers) != 0;
        }

        public static float FastDistance(Vector3 v1, Vector3 v2)
        {
            float f;
            float f2;
            f = v1.x - v2.x;
            if (f < 0) { f = f * -1; }
            f2 = v1.z - v2.z;
            if (f2 < 0) { f2 = f2 * -1; }

            if (f > f2) { f2 = f; }

            return f2;
        }
    }
}