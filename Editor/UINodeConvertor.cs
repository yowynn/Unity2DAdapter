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
        protected override GameObject CreateNode(CsdNode node, GameObject parent = null)
        {
            // ! do not change the order!!
            var go = GetSubnode(node.FillNode);
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

        private Action<GameObject, CsdVector3> ConvertCanvasGameObject_Rotation = (go, val) =>
        {
            (var rotation, var skew) = SeparateRotationAndSkew(val);
            if (rotation != Vector3.zero)
            {
                go.GetComponent<RectTransform>().localEulerAngles = rotation;
            }
            if (skew != Vector2.zero)
            {
                if (!go.TryGetComponent<Cocos2Unity.Runtime.MeshTransform>(out var mt))
                {
                    mt = go.AddComponent<Cocos2Unity.Runtime.MeshTransform>();
                }
                mt.Skew = skew;
            }
        };
        private Action<GameObject, CsdVector3> ConvertCanvasGameObject_Scale = (go, val) => go.GetComponent<RectTransform>().localScale = new Vector3(val.X, val.Y, val.Z);
        private Action<GameObject, CsdVector3> ConvertCanvasGameObject_Pivot = (go, val) => go.GetComponent<RectTransform>().pivot = new Vector2(val.X, val.Y);
        private Action<GameObject, CsdVector3> ConvertCanvasGameObject_Anchor = (go, val) =>
        {
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(val.X, val.Y);
            rt.anchorMax = new Vector2(val.X, val.Y);
        };
        private void ConvertCanvasGameObject_Image(GameObject go, CsdFileLink val) { if (val != null) LoadCanvasImage(go, val); }
        private void ConvertCanvasGameObject_Color(GameObject go, CsdColor val) { if (val != null) SetCanvasNodeColor(go, val); }
        private void ConvertCanvasGameObject_isInteractive(GameObject go, bool val) { SetCanvasImageInteractive(go, val); }
        private void ConvertCanvasGameObject_BackgroundColor(GameObject go, CsdColorGradient val)
        {
            if (val != null && val.Mode != CsdColorGradient.ColorMode.None)
            {
                if (!go.TryGetComponent<Image>(out var image))
                {
                    image = go.AddComponent<Image>();
                }
                var color = val.Color;
                image.color = new Color(color.R, color.G, color.B, color.A);
            }
        }
        private void ConvertCanvasGameObject_Children(GameObject go, List<CsdNode> val) { if (val != null) foreach (var child in val) CreateNode(child, go); }

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
            if (!go.TryGetComponent<Image>(out var image))
            {
                image = go.AddComponent<Image>();
            }
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
                    if (!go.TryGetComponent<Cocos2Unity.Runtime.MeshTransform>(out var mt))
                    {
                        mt = go.AddComponent<Cocos2Unity.Runtime.MeshTransform>();
                    }
                    mt.Orientation = Cocos2Unity.Runtime.GraphicOrientation.Left;
                }
            }
        }

        private static (Vector3 Rotation, Vector2 Skew) SeparateRotationAndSkew(CsdVector3 rotationSkew)
        {
            var tuple = (Rotation: Vector3.zero, Skew: Vector2.zero);
            if (Mathf.Abs(rotationSkew.X - rotationSkew.Y) > 1)
            {
                tuple.Skew = new Vector2(-rotationSkew.X, -rotationSkew.Y);
            }
            else
            {
                tuple.Rotation = new Vector3(0, 0, -rotationSkew.X);
            }
            return tuple;
        }

        private void SetCanvasNodeColor(GameObject go, CsdColor color)
        {
            if (go.TryGetComponent<Image>(out var image))
            {
                image.color = new Color(color.R, color.G, color.B, color.A);
            }
            else
            {
                if (!go.TryGetComponent<Cocos2Unity.Runtime.MeshTransform>(out var mt))
                {
                    mt = go.AddComponent<Cocos2Unity.Runtime.MeshTransform>();
                }
                mt.Color = new Color(color.R, color.G, color.B, color.A);
            }
        }

        private void SetCanvasImageInteractive(GameObject go, bool isInteractive)
        {
            if (!go.TryGetComponent<Image>(out var image))
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

        protected override void BindAnimationCurves(AnimationClip clip, GameObject go, string relativePath, CsdTimeline timeline)
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
            if (timeline.RotationSkew != null)
            {
                // use "localEulerAnglesRaw.x" to set "Euler Angles" Mode
                // use "localEulerAnglesBaked.x" to set "Euler Angles (Quaternion)" Mode
                // use "localEulerAngles.x" to set "Quaternion" Mode
                bool hasSkew = false;
                AnimationUtility.SetEditorCurve(clip, GetBinding<RectTransform>("localEulerAnglesRaw.x"), getFloatCurve(timeline.RotationSkew, val =>
                {
                    var sep = SeparateRotationAndSkew(val);
                    hasSkew = hasSkew || sep.Skew != Vector2.zero;
                    return sep.Rotation.x;
                }));
                AnimationUtility.SetEditorCurve(clip, GetBinding<RectTransform>("localEulerAnglesRaw.y"), getFloatCurve(timeline.RotationSkew, val =>
                {
                    var sep = SeparateRotationAndSkew(val);
                    hasSkew = hasSkew || sep.Skew != Vector2.zero;
                    return sep.Rotation.y;
                }));
                AnimationUtility.SetEditorCurve(clip, GetBinding<RectTransform>("localEulerAnglesRaw.z"), getFloatCurve(timeline.RotationSkew, val =>
                {
                    var sep = SeparateRotationAndSkew(val);
                    hasSkew = hasSkew || sep.Skew != Vector2.zero;
                    return sep.Rotation.z;
                }));
                if (hasSkew)
                {
                    if (!go.TryGetComponent<Cocos2Unity.Runtime.MeshTransform>(out var mt))
                    {
                        mt = go.AddComponent<Cocos2Unity.Runtime.MeshTransform>();
                    }
                    AnimationUtility.SetEditorCurve(clip, GetBinding<Cocos2Unity.Runtime.MeshTransform>("m_Skew.x"), getFloatCurve(timeline.RotationSkew, val => SeparateRotationAndSkew(val).Skew.x));
                    AnimationUtility.SetEditorCurve(clip, GetBinding<Cocos2Unity.Runtime.MeshTransform>("m_Skew.y"), getFloatCurve(timeline.RotationSkew, val => SeparateRotationAndSkew(val).Skew.y));
                }
            }
            if (timeline.Scale != null)
            {
                AnimationUtility.SetEditorCurve(clip, GetBinding<RectTransform>("m_LocalScale.x"), getFloatCurve(timeline.Scale, val => val.X));
                AnimationUtility.SetEditorCurve(clip, GetBinding<RectTransform>("m_LocalScale.y"), getFloatCurve(timeline.Scale, val => val.Y));
                AnimationUtility.SetEditorCurve(clip, GetBinding<RectTransform>("m_LocalScale.z"), getFloatCurve(timeline.Scale, val => val.Z));
            }
            if (timeline.Pivot != null)
            {
                AnimationUtility.SetEditorCurve(clip, GetBinding<RectTransform>("m_Pivot.x"), getFloatCurve(timeline.Pivot, val => val.X));
                AnimationUtility.SetEditorCurve(clip, GetBinding<RectTransform>("m_Pivot.y"), getFloatCurve(timeline.Pivot, val => val.Y));
            }
            if (timeline.FillImage != null)
            {
                AnimationUtility.SetObjectReferenceCurve(clip, GetBinding<Image>("m_Sprite"), getObjectCurve(timeline.FillImage, val => GetSprite(val).sprite));
                // TODO what if the sprite is rotated?
            }
            if (timeline.Color_Alpha != null)
            {
                if (go.TryGetComponent<Image>(out var _))
                {
                    AnimationUtility.SetEditorCurve(clip, GetBinding<Image>("m_Color.a"), getFloatCurve(timeline.Color_Alpha, val => val));
                }
                else
                {
                    if (!go.TryGetComponent<Cocos2Unity.Runtime.MeshTransform>(out var mt))
                    {
                        mt = go.AddComponent<Cocos2Unity.Runtime.MeshTransform>();
                    }
                    AnimationUtility.SetEditorCurve(clip, GetBinding<Cocos2Unity.Runtime.MeshTransform>("m_Color.a"), getFloatCurve(timeline.Color_Alpha, val => val));
                }
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
            for (int i = 0; i < ac.keys.Length - 1; i++)
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
