using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Cocos2Unity
{
    public class UINodeConvertor : Convertor
    {
        protected override GameObject CreateGameObject(CsdNode node, GameObject parent = null)
        {
            // ! do not change the order!!
            var go = CreateFromPrefab(node.FillNode);
            if (!go.TryGetComponent<RectTransform>(out var rt))
                rt = go.AddComponent<RectTransform>();
            if (parent != null)
            {
                go.transform.SetParent(parent.transform, false);
            }
            ConvertCanvasGameObject_Name(go, node.Name);
            ConvertCanvasGameObject_isActive(go, node.isActive);
            ConvertCanvasGameObject_Pivot(go, node.Pivot);
            ConvertCanvasGameObject_Anchor(go, node.Anchor);
            ConvertCanvasGameObject_Size(go, node.Size);
            ConvertCanvasGameObject_Position(go, node.Position);
            ConvertCanvasGameObject_Rotation(go, node.RotationSkew);
            ConvertCanvasGameObject_Scale(go, node.Scale);
            ConvertCanvasGameObject_Image(go, node.FillImage);
            ConvertCanvasGameObject_Color(go, node.Color);
            ConvertCanvasGameObject_BackgroundColor(go, node.FillColor);
            ConvertCanvasGameObject_isInteractive(go, node.isInteractive);
            ConvertCanvasGameObject_Children(go, node.Children);
            return go;
        }
        private Action<GameObject, string> ConvertCanvasGameObject_Name = (go, val) => go.name = val;
        private Action<GameObject, bool> ConvertCanvasGameObject_isActive = (go, val) => go.SetActive(val);
        private Action<GameObject, CsdVector3> ConvertCanvasGameObject_Size = (go, val) => go.GetComponent<RectTransform>().sizeDelta = new Vector2(val.X, val.Y);
        private Action<GameObject, CsdVector3> ConvertCanvasGameObject_Position = (go, val) =>
        {
            var rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = GetAnchoredPosition(go, val);
        };

        private Action<GameObject, CsdVector3> ConvertCanvasGameObject_Rotation = (go, val) => go.GetComponent<RectTransform>().Rotate(0, 0, -val.X);
        private Action<GameObject, CsdVector3> ConvertCanvasGameObject_Scale = (go, val) => go.GetComponent<RectTransform>().localScale = new Vector3(val.X, val.Y, val.Z);
        private Action<GameObject, CsdVector3> ConvertCanvasGameObject_Pivot = (go, val) => go.GetComponent<RectTransform>().pivot = new Vector2(val.X, val.Y);
        private Action<GameObject, CsdVector3> ConvertCanvasGameObject_Anchor = (go, val) =>
        {
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(val.X, val.Y);
            rt.anchorMax = new Vector2(val.X, val.Y);
        };
        private Action<GameObject, CsdVector3> ConvertCanvasGameObject_AnchorMax = (go, val) => go.GetComponent<RectTransform>().anchorMax = new Vector2(val.X, val.Y);
        private void ConvertCanvasGameObject_Image(GameObject go, CsdFileLink val) { if (val != null) LoadCanvasImage(go, val); }
        private void ConvertCanvasGameObject_Color(GameObject go, CsdColor val) { if (val != null) SetCanvasImageColor(go, val); }
        private void ConvertCanvasGameObject_isInteractive(GameObject go, bool val) { SetCanvasImageInteractive(go, val); }
        private void ConvertCanvasGameObject_BackgroundColor(GameObject go, CsdColorGradient val)
        {
            if (val != null && val.Mode != CsdColorGradient.ColorMode.None)
            {
                var image = go.GetComponent<Image>();
                if (!image)
                {
                    image = go.AddComponent<Image>();
                    var color = val.ColorA;
                    image.color = new Color(color.R, color.G, color.B, color.A);
                }
            }
        }
        private void ConvertCanvasGameObject_Children(GameObject go, List<CsdNode> val) { if (val != null) foreach (var child in val) CreateGameObject(child, go); }

        private static Vector2 GetAnchoredPosition(GameObject go, CsdVector3 position)
        {
            var prt = go.transform.parent as RectTransform;
            var anchoredPosition = new Vector2(position.X, position.Y);
            if (prt != null)
            {
                anchoredPosition -= prt.rect.size * prt.anchorMin;
            }
            return anchoredPosition;
        }
        private void LoadCanvasImage(GameObject go, CsdFileLink imageData)
        {
            var image = go.GetComponent<Image>();
            if (!image)
            {
                image = go.AddComponent<Image>();
            }
            // image.color = new Color(Random.Next(100) / 100f, Random.Next(100) / 100f, Random.Next(100) / 100f);
            var convertedSprite = GetSprite(imageData);
            if (convertedSprite?.sprite != null)
            {
                image.sprite = convertedSprite.sprite;
                if (convertedSprite.rotated)
                {
                    // image.transform.Rotate(0, 0, 90);
                    // var rt = image.GetComponent<RectTransform>();
                    // rt.sizeDelta = new Vector2(rt.sizeDelta.y, rt.sizeDelta.x);
                    // rt.pivot = new Vector2(rt.pivot.y, 1 - rt.pivot.x);
                    var ori = go.AddComponent<UIOrientation>();
                    ori.Orientation = UIOrientation.OrientationEnum.Left;
                }
            }
        }

        private void SetCanvasImageColor(GameObject go, CsdColor color)
        {
            var image = go.GetComponent<Image>();
            if (!image)
            {
                // image = go.AddComponent<Image>();
                return;
            }
            image.color = new Color(color.R, color.G, color.B, color.A);
        }

        private void SetCanvasImageInteractive(GameObject go, bool isInteractive)
        {
            var image = go.GetComponent<Image>();
            if (!image)
            {
                if (isInteractive)
                {
                    image = go.AddComponent<Image>();
                    image.color = Color.clear;
                }
                else
                {
                    return;
                }
            }
            image.raycastTarget = isInteractive;
        }

        protected override void BindAnimationCurve(AnimationClip clip, GameObject go, string relativePath, CsdTimeline timeline)
        {
            if (timeline.isActive != null)
            {
                clip.SetCurve(relativePath, typeof(GameObject), "m_IsActive", getFloatCurve(timeline.isActive, val => val.Value ? 1f : 0f));
            }
            if (timeline.Position != null)
            {
                clip.SetCurve(relativePath, typeof(RectTransform), "m_AnchoredPosition.x", getFloatCurve(timeline.Position, val => GetAnchoredPosition(go, val).x));
                clip.SetCurve(relativePath, typeof(RectTransform), "m_AnchoredPosition.y", getFloatCurve(timeline.Position, val => GetAnchoredPosition(go, val).y));
            }
            if (timeline.Rotation != null)
            {
                clip.SetCurve(relativePath, typeof(RectTransform), "m_LocalRotation.x", getFloatCurve(timeline.Rotation, val => Quaternion.Euler(0, 0, -val.X).x));
                clip.SetCurve(relativePath, typeof(RectTransform), "m_LocalRotation.y", getFloatCurve(timeline.Rotation, val => Quaternion.Euler(0, 0, -val.X).y));
                clip.SetCurve(relativePath, typeof(RectTransform), "m_LocalRotation.z", getFloatCurve(timeline.Rotation, val => Quaternion.Euler(0, 0, -val.X).z));
                clip.SetCurve(relativePath, typeof(RectTransform), "m_LocalRotation.w", getFloatCurve(timeline.Rotation, val => Quaternion.Euler(0, 0, -val.X).w));
            }
            if (timeline.Scale != null)
            {
                clip.SetCurve(relativePath, typeof(RectTransform), "m_LocalScale.x", getFloatCurve(timeline.Scale, val => val.X));
                clip.SetCurve(relativePath, typeof(RectTransform), "m_LocalScale.y", getFloatCurve(timeline.Scale, val => val.Y));
            }
            if (timeline.Pivot != null)
            {
                clip.SetCurve(relativePath, typeof(RectTransform), "m_Pivot.x", getFloatCurve(timeline.Pivot, val => val.X));
                clip.SetCurve(relativePath, typeof(RectTransform), "m_Pivot.y", getFloatCurve(timeline.Pivot, val => val.Y));
            }
            if (timeline.Image != null)
            {
                var binding = new UnityEditor.EditorCurveBinding { path = relativePath, type = typeof(Image), propertyName = "m_Sprite" };
                UnityEditor.AnimationUtility.SetObjectReferenceCurve(clip, binding, getObjectCurve(timeline.Image, val => GetSprite(val).sprite));
                // TODO what if the sprite is rotated?
            }

        }
        public static AnimationCurve getFloatCurve<T>(List<CsdFrame<T>> frameList, Func<T, float> getFrameValue) where T : ICsdParse<T>, new()
        {
            var ac = new AnimationCurve();
            foreach(var frame in frameList)
            {
                var time = frame.Time;
                var value = getFrameValue(frame.Value);
                Keyframe kf = new Keyframe(time, value);
                switch (frame.Type)
                {
                    case CsdFrame<T>.EasingType.Constant:
                        kf.inTangent = float.PositiveInfinity;
                        kf.outTangent = float.PositiveInfinity;
                        break;
                    case CsdFrame<T>.EasingType.Costum:
                        break;
                    case CsdFrame<T>.EasingType.Linear:
                        break;
                }
                ac.AddKey(kf);
            }
            return ac;
        }

        public static UnityEditor.ObjectReferenceKeyframe[] getObjectCurve<T>(List<CsdFrame<T>> frameList, Func<T, UnityEngine.Object> getFrameValue) where T : ICsdParse<T>, new()
        {
            var ac = new UnityEditor.ObjectReferenceKeyframe[frameList.Count];
            var index = 0;
            foreach (var frame in frameList)
            {
                UnityEditor.ObjectReferenceKeyframe kf = new UnityEditor.ObjectReferenceKeyframe();
                kf.time = frame.Time;
                kf.value = getFrameValue(frame.Value);
                ac[index++] = kf;
            }
            return ac;
        }
    }
}
