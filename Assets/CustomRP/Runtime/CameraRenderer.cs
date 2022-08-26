using UnityEngine;
using UnityEngine.Rendering;

namespace CustomRP.Runtime
{
    public partial class CameraRenderer
    {
        private const string BUFFER_NAME = "Render Camera";
        private static readonly ShaderTagId UnlitShaderTagId = new ShaderTagId("SRPDefaultUnlit");

        private ScriptableRenderContext _context;
        private Camera _camera;

        private readonly CommandBuffer _buffer = new()
        {
            name = BUFFER_NAME
        };

        private CullingResults _cullingResults;

        public void Render(ScriptableRenderContext context, Camera camera)
        {
            _context = context;
            _camera = camera;

            PrepareBuffer();
            PrepareForSceneWindow();
            
            if (!Cull())
                return;

            Setup();
            DrawVisibleGeometry();
            DrawUnsupportedShaders();
            DrawGizmos();
            Submit();
        }

        private void Setup()
        {
            _context.SetupCameraProperties(_camera);
            var flags = _camera.clearFlags;
            _buffer.ClearRenderTarget(
                flags <= CameraClearFlags.Depth,
                flags == CameraClearFlags.Color,
                flags == CameraClearFlags.Color ? 
                    _camera.backgroundColor.linear : Color.clear
            );
            _buffer.BeginSample(SampleName);
            ExecuteBuffer();
        }

        private void DrawVisibleGeometry()
        {
            var sortingSettings = new SortingSettings(_camera)
            {
                criteria = SortingCriteria.CommonOpaque
            };

            var drawingSettings = new DrawingSettings(UnlitShaderTagId, sortingSettings);
            var filteringSettings = new FilteringSettings(RenderQueueRange.opaque);

            _context.DrawRenderers(_cullingResults, ref drawingSettings, ref filteringSettings);

            _context.DrawSkybox(_camera);

            sortingSettings.criteria = SortingCriteria.CommonTransparent;
            drawingSettings.sortingSettings = sortingSettings;
            filteringSettings.renderQueueRange = RenderQueueRange.transparent;

            _context.DrawRenderers(_cullingResults, ref drawingSettings, ref filteringSettings);
        }

        private void ExecuteBuffer()
        {
            _context.ExecuteCommandBuffer(_buffer);
            _buffer.Clear();
        }

        private void Submit()
        {
            _buffer.EndSample(SampleName);
            ExecuteBuffer();
            _context.Submit();
        }

        private bool Cull()
        {
            if (_camera.TryGetCullingParameters(out var p))
            {
                _cullingResults = _context.Cull(ref p);
                return true;
            }

            return false;
        }
    }
}