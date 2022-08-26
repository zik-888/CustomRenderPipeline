using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;

namespace CustomRP.Runtime
{
    public partial class CameraRenderer
    {
        partial void DrawGizmos ();
        partial void DrawUnsupportedShaders ();
        partial void PrepareForSceneWindow ();
        
        partial void PrepareBuffer ();
        
#if UNITY_EDITOR
        private static Material _errorMaterial;

        private static readonly ShaderTagId[] LegacyShaderTagIds =
        {
            new("Always"),
            new("ForwardBase"),
            new("PrepassBase"),
            new("Vertex"),
            new("VertexLMRGBM"),
            new("VertexLM")
        };

        private string SampleName { get; set; }
        
        partial void PrepareBuffer ()
        {
            Profiler.BeginSample("Editor Only");
            _buffer.name = SampleName = _camera.name;
            Profiler.EndSample();
        }

        partial void PrepareForSceneWindow () 
        {
            if (_camera.cameraType == CameraType.SceneView)
                ScriptableRenderContext.EmitWorldGeometryForSceneView(_camera);
        }
        
        partial void DrawGizmos () 
        {
            if (Handles.ShouldRenderGizmos()) 
            {
                _context.DrawGizmos(_camera, GizmoSubset.PreImageEffects);
                _context.DrawGizmos(_camera, GizmoSubset.PostImageEffects);
            }
        }

        partial void DrawUnsupportedShaders()
        {
            if (_errorMaterial == null)
            {
                _errorMaterial = new Material(Shader.Find("Hidden/InternalErrorShader"));
            }

            var sortingSettings = new SortingSettings(_camera);
            var drawingSettings = new DrawingSettings(LegacyShaderTagIds[0], sortingSettings)
            {
                overrideMaterial = _errorMaterial
            };

            for (var i = 0; i < LegacyShaderTagIds.Length; i++)
                drawingSettings.SetShaderPassName(i, LegacyShaderTagIds[i]);

            var filteringSettings = FilteringSettings.defaultValue;
            _context.DrawRenderers(_cullingResults, ref drawingSettings, ref filteringSettings);
        }
        
#else
        const string SampleName = bufferName;
#endif
    }
}