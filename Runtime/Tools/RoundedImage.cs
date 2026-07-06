using UnityEngine;
using UnityEngine.UI;

namespace AlmediaLink.Tools
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform), typeof(MaskableGraphic))]
    public class RoundedImage : MonoBehaviour
    {
        private static readonly int WidthHeightRadiusId = Shader.PropertyToID("_WidthHeightRadius");
        private const string ShaderName = "AlmediaLinkSDK/UI/RoundedRect";

        [SerializeField, Min(0), Tooltip("Corner radius in pixels.")]
        private float _radius = 16f;

        [SerializeField, Tooltip("Direct reference to the RoundedRect shader. Prevents shader stripping in builds.")]
        private Shader _shaderOverride;

        private Material _material;
        private MaskableGraphic _graphic;

        private void OnEnable()
        {
            TryGetComponent(out _graphic);
            EnsureMaterial();
            Refresh();
        }

        private void OnDisable()
        {
            if (_graphic != null)
                _graphic.material = null;
            DestroyMaterial();
        }

        private void OnDestroy()
        {
            DestroyMaterial();
        }

        private void OnRectTransformDimensionsChange()
        {
            if (enabled && _material != null)
                Refresh();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_graphic == null)
                TryGetComponent(out _graphic);
            EnsureMaterial();
            Refresh();
        }
#endif

        private void EnsureMaterial()
        {
            if (_material != null) return;

            var shader = _shaderOverride != null ? _shaderOverride : Shader.Find(ShaderName);
            if (shader == null)
            {
                AlmediaLog.Error("[AlmediaLink] RoundedRect shader not found. Ensure the shader is included in the build.");
                return;
            }

            _material = new Material(shader);
            _material.hideFlags = HideFlags.HideAndDontSave;

            if (_graphic != null)
                _graphic.material = _material;
        }

        private void Refresh()
        {
            if (_material == null) return;

            var rect = ((RectTransform)transform).rect;
            _material.SetVector(WidthHeightRadiusId, new Vector4(rect.width, rect.height, _radius, 0f));
        }

        private void DestroyMaterial()
        {
            if (_material == null) return;

            if (Application.isPlaying)
                Destroy(_material);
            else
                DestroyImmediate(_material);

            _material = null;
        }
    }
}
