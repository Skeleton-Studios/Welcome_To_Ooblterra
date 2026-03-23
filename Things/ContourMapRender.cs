using System.IO;
using UnityEngine;
using Welcome_To_Ooblterra.Properties;

namespace Welcome_To_Ooblterra.Things
{
    public class ContourMapRender : MonoBehaviour
    {
        [SerializeField]
        private Vector3 contourMin = new(-200, 150, -150);

        [SerializeField]
        private Vector3 contourMax = new(100, 150, 180);

        private const float MapPixelSize = 0.7f; // each map pixel represents this many world units

        [SerializeField]
        private LayerMask raycastMask = Physics.AllLayers;

        [SerializeField]
        private MeshCollider[] enableColliders = [];

        [SerializeField]
        private Collider[] disableColliders = [];

        private static readonly WTOBase.WTOLogger Log = new(typeof(ContourMapRender), LogSourceType.Thing);

        private void Start()
        {
            CaptureDepthMap();
        }

        /// <summary>
        /// Captures depth by raycasting once per output pixel and writes an EXR map to disk.
        /// </summary>
        public void CaptureDepthMap()
        {
            PrepareCollidersForRaycast();

            try
            {
                int textureWidth = Mathf.CeilToInt((contourMax.x - contourMin.x) / MapPixelSize);
                int textureHeight = Mathf.CeilToInt((contourMax.z - contourMin.z) / MapPixelSize);
                Texture2D depthTexture = new Texture2D(textureWidth, textureHeight, TextureFormat.RFloat, false);
                Color[] pixels = new Color[textureWidth * textureHeight];

                for (int y = 0; y < textureHeight; y++)
                {
                    float v = 1f - ((float)y / (textureHeight - 1));

                    for (int x = 0; x < textureWidth; x++)
                    {
                        float u = (float)x / (textureWidth - 1);

                        Ray ray = new(new Vector3(x * MapPixelSize + contourMin.x, contourMin.y, v * (contourMax.z - contourMin.z) + contourMin.z), Vector3.down);

                        float depthValue = 1000f;
                        if (Physics.Raycast(ray, out RaycastHit hit, 1000f, raycastMask, QueryTriggerInteraction.Ignore))
                        {
                            depthValue = Vector3.Dot(hit.point - ray.origin, Vector3.down);
                        }

                        int pixelIndex = ((textureHeight - 1 - y) * textureWidth) + x;
                        pixels[pixelIndex] = new Color(depthValue, 0f, 0f, 1f);
                    }

                    if ((y & 127) == 0)
                    {
                        Log.Info($"Contour depth capture progress: {y}/{textureHeight} rows");
                    }
                }

                depthTexture.SetPixels(pixels);
                depthTexture.Apply(false, false);

                byte[] exrData = depthTexture.EncodeToEXR(Texture2D.EXRFlags.OutputAsFloat);
                UnityEngine.Object.Destroy(depthTexture);

                string outputDir = Path.Combine(Application.persistentDataPath, "WTO");
                Directory.CreateDirectory(outputDir);
                string outputPath = Path.Combine(outputDir, "ContourDepthMap.exr");

                File.WriteAllBytes(outputPath, exrData);
                Log.Info($"Depth map saved to {outputPath}");
            }
            finally
            {
                RestoreColliders();
            }
        }

        private void PrepareCollidersForRaycast()
        {
            foreach(var col in enableColliders)
            {
                col.enabled = true;
            }
            foreach(var col in disableColliders)
            {
                col.enabled = false;
            }
            Physics.SyncTransforms();
        }

        private void RestoreColliders()
        {
            foreach(var col in enableColliders)
            {
                col.enabled = false;
            }
            foreach(var col in disableColliders)
            {
                col.enabled = true;
            }
            Physics.SyncTransforms();
        }
    }
}
