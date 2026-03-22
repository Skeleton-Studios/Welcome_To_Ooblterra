using System.IO;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using Welcome_To_Ooblterra.Properties;

namespace Welcome_To_Ooblterra.Things
{
    public class ContourMapRender : MonoBehaviour
    {
        private const int TextureSize = 2048;
        private const float OrthographicSize = 120f;
        private const float NearClip = 0.3f;
        private const float FarClip = 1000f;

        private Camera mapCamera;
        private RenderTexture renderTexture;
        private RenderTexture depthColourRT;
        private CommandBuffer depthCopyCmd;

        private static readonly WTOBase.WTOLogger Log = new(typeof(ContourMapRender), LogSourceType.Thing);

        private void Awake()
        {
            mapCamera = GetComponent<Camera>();
            if (mapCamera == null)
            {
                Log.Error("ContourMapRender requires a Camera component on the same GameObject.");
                return;
            }

            renderTexture = new RenderTexture(TextureSize, TextureSize, 32, RenderTextureFormat.Depth)
            {
                name = "WTOMapRenderTexture",
                depthStencilFormat = GraphicsFormat.D32_SFloat
            };
            renderTexture.Create();

            depthColourRT = new RenderTexture(TextureSize, TextureSize, 0, RenderTextureFormat.RFloat)
            {
                name = "WTODepthColourRT"
            };
            depthColourRT.Create();

            mapCamera.orthographic = true;
            mapCamera.orthographicSize = OrthographicSize;
            mapCamera.nearClipPlane = NearClip;
            mapCamera.farClipPlane = FarClip;
            mapCamera.targetTexture = renderTexture;
            mapCamera.depthTextureMode = DepthTextureMode.Depth;
            mapCamera.enabled = false;

            // Attach a command buffer to the map camera that copies its own depth
            // buffer into the readable colour RT after rendering completes.
            depthCopyCmd = new CommandBuffer { name = "WTO Depth Copy" };
            depthCopyCmd.Blit(BuiltinRenderTextureType.Depth, depthColourRT);
            mapCamera.AddCommandBuffer(CameraEvent.AfterEverything, depthCopyCmd);
        }

        private void Start()
        {
            CaptureDepthMap();
        }

        /// <summary>
        /// Call this to capture the depth map and write it to disk.
        /// </summary>
        public void CaptureDepthMap()
        {
            if (mapCamera == null || renderTexture == null || depthColourRT == null)
            {
                Log.Error("Cannot capture depth map: camera or render textures are not initialised.");
                return;
            }

            // Render the scene. The attached CommandBuffer will automatically copy
            // this camera's depth buffer into depthColourRT after rendering.
            mapCamera.Render();

            // Read back the depth data into a Texture2D.
            RenderTexture previousActive = RenderTexture.active;
            RenderTexture.active = depthColourRT;

            Texture2D depthReadback = new Texture2D(TextureSize, TextureSize, TextureFormat.RFloat, false);
            depthReadback.ReadPixels(new Rect(0, 0, TextureSize, TextureSize), 0, 0);
            depthReadback.Apply();

            RenderTexture.active = previousActive;

            // Encode as EXR (32-bit float) to preserve full depth precision.
            byte[] exrData = depthReadback.EncodeToEXR(Texture2D.EXRFlags.OutputAsFloat);
            UnityEngine.Object.Destroy(depthReadback);

            string outputDir = Path.Combine(Application.persistentDataPath, "WTO");
            Directory.CreateDirectory(outputDir);
            string outputPath = Path.Combine(outputDir, "ContourDepthMap.exr");

            File.WriteAllBytes(outputPath, exrData);
            Log.Info($"Depth map saved to {outputPath}");
        }

        private void OnDestroy()
        {
            if (depthCopyCmd != null)
            {
                if (mapCamera != null)
                {
                    mapCamera.RemoveCommandBuffer(CameraEvent.AfterEverything, depthCopyCmd);
                }
                depthCopyCmd.Release();
            }
            if (depthColourRT != null)
            {
                depthColourRT.Release();
                UnityEngine.Object.Destroy(depthColourRT);
            }
            if (renderTexture != null)
            {
                renderTexture.Release();
                UnityEngine.Object.Destroy(renderTexture);
            }
        }
    }
}
