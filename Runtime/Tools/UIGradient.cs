using UnityEngine;
using UnityEngine.UI;

namespace AlmediaLink.Tools
{
    [AddComponentMenu("UI/Gradient")]
    [RequireComponent(typeof(Graphic))]
    public class UIGradient : BaseMeshEffect
    {
        public enum Direction { Horizontal, Vertical, DiagonalLTR, DiagonalRTL }

        [SerializeField, Tooltip("Gradient direction across the UI element.")] Direction _direction = Direction.Horizontal;
        [SerializeField] Color _colorStart = new Color(0.482f, 0.282f, 0.765f);
        [SerializeField] Color _colorEnd   = new Color(0.729f, 0.098f, 1.000f);

        public override void ModifyMesh(VertexHelper vh)
        {
            if (!IsActive() || vh.currentVertCount == 0) return;

            var verts = new System.Collections.Generic.List<UIVertex>();
            vh.GetUIVertexStream(verts);
            
            float xMin = float.MaxValue, xMax = float.MinValue;
            float yMin = float.MaxValue, yMax = float.MinValue;
            foreach (var v in verts)
            {
                if (v.position.x < xMin) xMin = v.position.x;
                if (v.position.x > xMax) xMax = v.position.x;
                if (v.position.y < yMin) yMin = v.position.y;
                if (v.position.y > yMax) yMax = v.position.y;
            }

            float w = xMax - xMin, h = yMax - yMin;

            for (int i = 0; i < verts.Count; i++)
            {
                var v = verts[i];
                float t = _direction switch
                {
                    Direction.Vertical      => (v.position.y - yMin) / h,
                    Direction.DiagonalLTR   => ((v.position.x - xMin) / w + (v.position.y - yMin) / h) * 0.5f,
                    Direction.DiagonalRTL   => ((xMax - v.position.x) / w + (v.position.y - yMin) / h) * 0.5f,
                    _                       => (v.position.x - xMin) / w,
                };
                v.color = Color.Lerp(_colorStart, _colorEnd, t);
                verts[i] = v;
            }

            vh.Clear();
            vh.AddUIVertexTriangleStream(verts);
        }

#if UNITY_EDITOR
        protected override void OnValidate() { base.OnValidate(); graphic?.SetVerticesDirty(); }
#endif
    }
}