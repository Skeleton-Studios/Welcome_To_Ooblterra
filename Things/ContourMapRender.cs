using System.IO;
using UnityEngine;
using Welcome_To_Ooblterra.Properties;

namespace Welcome_To_Ooblterra.Things
{
    /// <summary>
    /// Captures a depth map of the main moon on spawn and outputs it to a file.
    /// This is then postprocessed by a Python script to generate contour lines for
    /// the map.
    /// 
    /// This is a one time capture and should not exist when players are playing the game.
    /// </summary>
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
            if (WTOBase.WTOStartupType.Value == StartupType.ContourMapRender)
            {
                CaptureDepthMap();
                Application.Quit(0);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// Captures depth by raycasting once per output pixel and writes an EXR file to disk.
        /// </summary>
        public void CaptureDepthMap()
        {
            PrepareCollidersForRaycast();

            try
            {
                int textureWidth = Mathf.CeilToInt((contourMax.x - contourMin.x) / MapPixelSize);
                int textureHeight = Mathf.CeilToInt((contourMax.z - contourMin.z) / MapPixelSize);
                Color[] pixels = new Color[textureWidth * textureHeight];

                for (int y = 0; y < textureHeight; y++)
                {
                    for (int x = 0; x < textureWidth; x++)
                    {
                        Ray ray = new(
                            new Vector3(
                                contourMin.x + x * MapPixelSize,
                                contourMin.y, // fixed height for ray origin
                                contourMin.z + y * MapPixelSize
                            ), 
                            Vector3.down
                        );

                        float depthValue = 1000f;
                        if (Physics.Raycast(ray, out RaycastHit hit, 1000f, raycastMask, QueryTriggerInteraction.Ignore))
                        {
                            depthValue = hit.distance;
                        }

                        int pixelIndex = ((textureHeight - 1 - y) * textureWidth) + x;
                        pixels[pixelIndex] = new Color(depthValue, 0f, 0f, 1f);
                    }

                    if ((y & 127) == 0)
                    {
                        Log.Info($"Contour depth capture progress: {y}/{textureHeight} rows");
                    }
                }

                byte[] exrData = EncodeEXR(pixels, textureWidth, textureHeight);

                File.WriteAllBytes(WTOBase.WTOContourMapWritePath.Value, exrData);
                Log.Info($"Depth map saved to {WTOBase.WTOContourMapWritePath.Value}");
            }
            finally
            {
                RestoreColliders();
            }
        }

        private byte[] EncodeEXR(Color[] pixels, int width, int height)
        {
            Texture2D depthTexture = new(width, height, TextureFormat.RFloat, false);
            try
            {
                depthTexture.SetPixels(pixels);
                depthTexture.Apply(false, false);
                return depthTexture.EncodeToEXR(Texture2D.EXRFlags.OutputAsFloat);
            }
            finally
            {
                Destroy(depthTexture);
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
