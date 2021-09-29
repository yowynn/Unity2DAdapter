using UnityEngine;
using UnityEngine.UI;

namespace Cocos2Unity
{
    [RequireComponent(typeof(Graphic))]
    public class UIOrientation : BaseMeshEffect
    {
        public enum OrientationEnum{
            Up,
            Down,
            Left,
            Right,
        }
        [SerializeField, Tooltip("方向")] public OrientationEnum Orientation = OrientationEnum.Up;
        public override void ModifyMesh(VertexHelper vh)
        {
            RectTransform rt = graphic.rectTransform;
            Vector3 center = rt.rect.center;
            float ratio = rt.rect.width / rt.rect.height;

            UIVertex vt = default;
            Vector3 pos;
            for (int i = 0; i < vh.currentVertCount; i++)
            {
                vh.PopulateUIVertex(ref vt, i);
                pos = vt.position;
                pos -= center;
                switch (Orientation)
                {
                    case OrientationEnum.Up:
                        break;
                    case OrientationEnum.Down:
                        (pos.x, pos.y) = (-pos.x, -pos.y);
                        break;
                    case OrientationEnum.Left:
                        (pos.x, pos.y) = (-pos.y * ratio, pos.x / ratio);
                        break;
                    case OrientationEnum.Right:
                        (pos.x, pos.y) = (pos.y * ratio, -pos.x / ratio);
                        break;
                }
                pos += center;
                vt.position = pos;
                vh.SetUIVertex(vt, i);
            }
        }
    }
}
