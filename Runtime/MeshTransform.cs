using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Cocos2Unity.Runtime
{
    /// <summary>
    /// 图像朝向
    /// </summary>
    public enum GraphicOrientation
    {
        Up,
        Down,
        Left,
        Right,
    }

    /// <summary>
    /// 扩展关于Mesh的一些变化实现：
    ///     1. 贴图方向
    ///     2. 图像倾斜（//TODO：倾斜的继承叠加）
    ///     3. 颜色叠加（继承叠加）
    /// </summary>
    // [RequireComponent(typeof(Graphic))]
    [RequireComponent(typeof(RectTransform))]
    public class MeshTransform : BaseMeshEffect
    {
        [SerializeField, Tooltip("图像朝向")]
        private GraphicOrientation m_Orientation = GraphicOrientation.Up;

        [SerializeField, Tooltip("倾斜")]
        private Vector2 m_Skew = Vector2.zero;

        [Header("受继承影响的属性")]
        [SerializeField, Tooltip("颜色叠加")]
        private Color m_Color = Color.white;

        private Color m_inheritColor = Color.white;

        public Color MixedColor => m_Color * m_inheritColor;
        public GraphicOrientation Orientation { get => m_Orientation; set => m_Orientation = value; }
        public Vector2 Skew { get => m_Skew; set => m_Skew = value; }
        public Color Color { get => m_Color; set { m_Color = value; SyncInheritColor(); } }

        public override void ModifyMesh(VertexHelper vh)
        {
            RectTransform rt = transform as RectTransform;
            Vector3 center = rt.rect.center;
            Vector3 pivot = rt.pivot;
            float ratio = rt.rect.width / rt.rect.height;

            UIVertex vt = default;
            Vector3 pos;
            for (int i = 0; i < vh.currentVertCount; i++)
            {
                vh.PopulateUIVertex(ref vt, i);
                pos = vt.position;
                // Modify Orientation
                switch (m_Orientation)
                {
                    case GraphicOrientation.Up:
                        break;
                    case GraphicOrientation.Down:
                        pos -= center;
                        (pos.x, pos.y) = (-pos.x, -pos.y);
                        pos += center;
                        break;
                    case GraphicOrientation.Left:
                        pos -= center;
                        (pos.x, pos.y) = (-pos.y * ratio, pos.x / ratio);
                        pos += center;
                        break;
                    case GraphicOrientation.Right:
                        pos -= center;
                        (pos.x, pos.y) = (pos.y * ratio, -pos.x / ratio);
                        pos += center;
                        break;
                }
                // Modify Skew
                if (m_Skew != Vector2.zero)
                {
                    var SkewRad = m_Skew * Mathf.Deg2Rad;
                    (pos.x, pos.y) = (pos.x * Mathf.Cos(SkewRad.y) - pos.y * Mathf.Sin(SkewRad.x), pos.y * Mathf.Cos(SkewRad.x) + pos.x * Mathf.Sin(SkewRad.y));
                    (pos.x, pos.y) = (pos.x * Mathf.Cos(SkewRad.y) - pos.y * Mathf.Sin(SkewRad.x), pos.y * Mathf.Cos(SkewRad.x) + pos.x * Mathf.Sin(SkewRad.y));
                }
                // Modify Color
                if (MixedColor != Color.white)
                {
                    vt.color = vt.color * MixedColor;
                }
                vt.position = pos;
                vh.SetUIVertex(vt, i);
            }
        }

        public void SyncInheritColor()
        {
            var mixedColor = MixedColor;
            if (mixedColor != Color.white)
            {
                foreach (Transform t in gameObject.transform)
                {
                    var rt = t as RectTransform;
                    if (rt != null)
                    {
                        if (!rt.TryGetComponent<MeshTransform>(out var mt))
                        {
                            mt = rt.gameObject.AddComponent<MeshTransform>();
                        }
                        mt.SyncInheritColor(mixedColor);
                    }
                }
                if (graphic != null)
                {
                    graphic.SetVerticesDirty();
                }
            }
        }

        public void SyncInheritColor(Color inheritColor)
        {
            m_inheritColor = inheritColor;
            SyncInheritColor();
        }

        protected override void Awake()
        {
            var pt = transform.parent as RectTransform;
            if (pt != null)
            {
                if (pt.TryGetComponent<MeshTransform>(out var mt))
                {
                    SyncInheritColor(mt.MixedColor);
                }
            }
        }

        protected override void OnDidApplyAnimationProperties()
        {
            SyncInheritColor();
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            SyncInheritColor();
        }
    }
#endif
}
