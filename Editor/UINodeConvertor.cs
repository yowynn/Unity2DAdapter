using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using Wynncs.Entry;

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
                    var color = val.Color;
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
            EditorCurveBinding GetBinding<T>(string propertyName)
            {
                return new EditorCurveBinding { path = relativePath, type = typeof(T), propertyName = propertyName };
            }

            if (timeline.isActive != null)
            {
                AnimationUtility.SetEditorCurve(clip, GetBinding<GameObject>("m_IsActive"), getFloatCurve(timeline.isActive, val => val.Value ? 1f : 0f));
            }
            if (timeline.Position != null)
            {
                AnimationUtility.SetEditorCurve(clip, GetBinding<RectTransform>("m_AnchoredPosition.x"), getFloatCurve(timeline.Position, val => GetAnchoredPosition(go, val).x));
                AnimationUtility.SetEditorCurve(clip, GetBinding<RectTransform>("m_AnchoredPosition.y"), getFloatCurve(timeline.Position, val => GetAnchoredPosition(go, val).y));
            }
            if (timeline.Rotation != null)
            {
                AnimationUtility.SetEditorCurve(clip, GetBinding<RectTransform>("m_LocalRotation.x"), getFloatCurve(timeline.Rotation, val => Quaternion.Euler(0, 0, -val.X).x));
                AnimationUtility.SetEditorCurve(clip, GetBinding<RectTransform>("m_LocalRotation.y"), getFloatCurve(timeline.Rotation, val => Quaternion.Euler(0, 0, -val.X).y));
                AnimationUtility.SetEditorCurve(clip, GetBinding<RectTransform>("m_LocalRotation.z"), getFloatCurve(timeline.Rotation, val => Quaternion.Euler(0, 0, -val.X).z));
                AnimationUtility.SetEditorCurve(clip, GetBinding<RectTransform>("m_LocalRotation.w"), getFloatCurve(timeline.Rotation, val => Quaternion.Euler(0, 0, -val.X).w));
            }
            if (timeline.Scale != null)
            {
                AnimationUtility.SetEditorCurve(clip, GetBinding<RectTransform>("m_LocalScale.x"), getFloatCurve(timeline.Scale, val => val.X));
                AnimationUtility.SetEditorCurve(clip, GetBinding<RectTransform>("m_LocalScale.y"), getFloatCurve(timeline.Scale, val => val.Y));
            }
            if (timeline.Pivot != null)
            {
                AnimationUtility.SetEditorCurve(clip, GetBinding<RectTransform>("m_Pivot.x"), getFloatCurve(timeline.Pivot, val => val.X));
                AnimationUtility.SetEditorCurve(clip, GetBinding<RectTransform>("m_Pivot.y"), getFloatCurve(timeline.Pivot, val => val.Y));
            }
            if (timeline.Image != null)
            {
                AnimationUtility.SetObjectReferenceCurve(clip, GetBinding<Image>("m_Sprite"), getObjectCurve(timeline.Image, val => GetSprite(val).sprite));
                // TODO what if the sprite is rotated?
            }
            if (timeline.Color_Alpha != null)
            {
                if (go.TryGetComponent<Image>(out var _))
                    AnimationUtility.SetEditorCurve(clip, GetBinding<Image>("m_Color.a"), getFloatCurve(timeline.Color_Alpha, val => val));
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
                ac.AddKey(kf);
            }
            for (int i = 0; i < frameList.Count - 1; i++)
            {
                var frame = frameList[i];
                SetFreeCubicBezier(ac, i, frame.Bezier);
            }
            return ac;
        }


        public static ObjectReferenceKeyframe[] getObjectCurve<T>(List<CsdFrame<T>> frameList, Func<T, UnityEngine.Object> getFrameValue) where T : ICsdParse<T>, new()
        {
            var ac = new ObjectReferenceKeyframe[frameList.Count];
            var index = 0;
            foreach (var frame in frameList)
            {
                var kf = new ObjectReferenceKeyframe();
                kf.time = frame.Time;
                kf.value = getFrameValue(frame.Value);
                ac[index++] = kf;
            }
            return ac;
        }

        private static void SetFreeCubicBezier(AnimationCurve ac, int keyIndex, float x1, float y1, float x2, float y2)
        {
            AnimationUtility.SetKeyBroken(ac, keyIndex, true);
            AnimationUtility.SetKeyBroken(ac, keyIndex + 1, true);
            AnimationUtility.SetKeyRightTangentMode(ac, keyIndex, AnimationUtility.TangentMode.Free);
            AnimationUtility.SetKeyLeftTangentMode(ac, keyIndex + 1, AnimationUtility.TangentMode.Free);
            var from = ac.keys[keyIndex];
            var to = ac.keys[keyIndex + 1];
            from.weightedMode = WeightedMode.Both;
            to.weightedMode = WeightedMode.Both;
            var scale = new Vector2(to.time - from.time, to.value - from.value);
            if (scale.x > 0 && scale.y != 0)
            {
                var vfrom = new Vector2(x1, y1) * scale;
                var vto = (new Vector2(x2, y2) - Vector2.one) * scale;
                from.outTangent = vfrom.y / vfrom.x;
                to.inTangent = vto.y / vto.x;
                from.outWeight = x1;
                to.inWeight = 1 - x2;
                ac.MoveKey(keyIndex, from);
                ac.MoveKey(keyIndex + 1, to);
            }
        }

        private static void SetFreeCubicBezier(AnimationCurve ac, int keyIndex, CubicBezier bezier)
        {
            SetFreeCubicBezier(ac, keyIndex, bezier.x1, bezier.y1, bezier.x2, bezier.y2);
        }
    }
}
