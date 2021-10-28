using System;
using System.Collections.Generic;
using Cocos2Unity.Models;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Cocos2Unity.Unity
{
    public class CanvasAnimatedGameObjectConvertor : AnimatedGameObjectConvertor
    {
        #region implemented abstract members of AnimatedGameObjectConvertor
        protected override void BindComponentData(GameObject node, ModNode nodeData)
        {
            if (!node.TryGetComponent<RectTransform>(out var rt))
                rt = node.AddComponent<RectTransform>();
            // ! DO NOT CHANGE ORDER OF THESE LINES !
            BindName(node, nodeData.Name);  // 名字
            BindVisible(node, nodeData.Visible);  // 是否可见
            BindPivot(node, nodeData.Pivot);  // 对齐点
            BindAnchor(node, nodeData.Anchor);  // 锚点
            BindRect(node, nodeData.Rect);  // 大小
            BindSkew(node, nodeData.Skew);  // 旋转和倾斜
            BindScale(node, nodeData.Scale);  // 缩放
            BindFiller(node, nodeData.Filler);  // 填充
            BindInteractive(node, nodeData.Interactive);  // 是否可交互
            BindColor(node, nodeData.Color);  // 颜色
        }

        protected override void BindAnimationCurves(GameObject root, AnimationClip clip, GameObject node, ModTimeline<ModNode> timeline)
        {
            var relativePath = AnimationUtility.CalculateTransformPath(node.transform, root.transform);
            EditorCurveBinding GetBinding<T>(string propertyName)
            {
                return new EditorCurveBinding
                {
                    type = typeof(T),
                    path = relativePath,
                    propertyName = propertyName
                };
            }

            foreach (var propertyName in timeline.GetPropertyNames())
            {
                switch (propertyName)
                {
                    case "Rect.Position":
                        {
                            var origin = timeline.GetCurve<ModVector2>(propertyName);
                            BindFloatCurves(clip, origin, new[]{
                                GetBinding<RectTransform>("m_AnchoredPosition.x"),
                                GetBinding<RectTransform>("m_AnchoredPosition.y")
                            }, val =>
                            {
                                var anchoredPosition = GetAnchoredPosition(node, new Vector2(val.X, val.Y));
                                return new[]{
                                    anchoredPosition.x,
                                    anchoredPosition.y,
                                };
                            });
                        }
                        break;
                    case "Scale":
                        {
                            var origin = timeline.GetCurve<ModVector2>(propertyName);
                            BindFloatCurves(clip, origin, new[]{
                                GetBinding<RectTransform>("m_LocalScale.x"),
                                GetBinding<RectTransform>("m_LocalScale.y"),
                                GetBinding<RectTransform>("m_LocalScale.z"),
                            }, val =>
                            {
                                return new[]{
                                    val.X,
                                    val.Y,
                                    1f,
                                };
                            });
                        }
                        break;
                    case "Pivot":
                        {
                            var origin = timeline.GetCurve<ModVector2>(propertyName);
                            BindFloatCurves(clip, origin, new[]{
                                GetBinding<RectTransform>("m_Pivot.x"),
                                GetBinding<RectTransform>("m_Pivot.y"),
                            }, val =>
                            {
                                return new[]{
                                    val.X,
                                    val.Y,
                                };
                            });
                        }
                        break;
                    case "Visible":
                        {
                            var origin = timeline.GetCurve<ModBoolean>(propertyName);
                            BindFloatCurve(clip, origin, GetBinding<GameObject>("m_IsActive"), val => val.Value ? 1f : 0f);
                        }
                        break;
                    case "Skew":
                        {
                            var origin = timeline.GetCurve<ModVector2>(propertyName);
                            BindFloatCurves(clip, origin, new[]{
                                GetBinding<RectTransform>("localEulerAnglesRaw.x"),
                                GetBinding<RectTransform>("localEulerAnglesRaw.y"),
                                GetBinding<RectTransform>("localEulerAnglesRaw.z"),
                                GetBinding<Cocos2Unity.Runtime.MeshTransform>("m_Skew.x"),
                                GetBinding<Cocos2Unity.Runtime.MeshTransform>("m_Skew.y"),
                            }, val =>
                            {
                                if (!SeparateRotationAndSkew(val, out var rotation, out var skew))
                                    if (!node.TryGetComponent<Cocos2Unity.Runtime.MeshTransform>(out var mt))
                                        mt = node.AddComponent<Cocos2Unity.Runtime.MeshTransform>();
                                return new[]{
                                    rotation.x,
                                    rotation.y,
                                    rotation.z,
                                    skew.x,
                                    skew.y,
                                };
                            });
                        }
                        break;
                    case "Filler.Sprite":
                        {
                            var origin = timeline.GetCurve<ModLinkedAsset>(propertyName);
                            BindObjectCurve(clip, origin, GetBinding<Image>("m_Sprite"), val => GetSprite(val.Name));
                        }
                        break;
                    case "Color.A":
                        {
                            var origin = timeline.GetCurve<ModSingle>(propertyName);
                            BindFloatCurve(clip, origin, GetBinding<Image>("m_Color.a"), val => val);
                        }
                        break;
                    default:
                        ProcessLog.Log("CanvasAnimatedGameObjectConvertor: Unsupported property name: " + propertyName);
                        break;
                }
            }
        }

        #endregion


        private void BindName(GameObject node, string name)
        {
            node.name = name;
        }
        private void BindVisible(GameObject node, ModBoolean visible)
        {
            node.SetActive(visible);
        }
        private void BindInteractive(GameObject node, ModBoolean interactive)
        {
            if (interactive)
            {
                if (node.TryGetComponent<Image>(out var image))
                    image.raycastTarget = true;
                else if (node.TryGetComponent<BoxCollider2D>(out var collider))
                    collider.isTrigger = true;
                else
                {
                    collider = node.AddComponent<BoxCollider2D>();
                    collider.isTrigger = true;
                }
            }
            else
            {
                if (node.TryGetComponent<Image>(out var image))
                    image.raycastTarget = false;
                else if (node.TryGetComponent<BoxCollider2D>(out var collider))
                    collider.isTrigger = false;
            }
        }
        private void BindRect(GameObject node, ModRect rect)
        {
            var rt = node.GetComponent<RectTransform>();
            rt.anchoredPosition = GetAnchoredPosition(node, new Vector2(rect.X, rect.Y));
            rt.sizeDelta = new Vector2(rect.Width, rect.Height);
        }
        private void BindPivot(GameObject node, ModVector2 pivot)
        {
            var rt = node.GetComponent<RectTransform>();
            rt.pivot = new Vector2(pivot.X, pivot.Y);
        }
        private void BindAnchor(GameObject node, ModRect anchor)
        {
            var rt = node.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(anchor.Min.X, anchor.Min.Y);
            rt.anchorMax = new Vector2(anchor.Max.X, anchor.Max.Y);
        }
        private void BindSkew(GameObject node, ModVector2 skew)
        {
            var rt = node.GetComponent<RectTransform>();
            if (SeparateRotationAndSkew(skew, out var rotationValue, out var skewValue))
            {
                rt.localEulerAngles = rotationValue;
            }
            else
            {
                if (!node.TryGetComponent<Cocos2Unity.Runtime.MeshTransform>(out var mt))
                {
                    mt = node.AddComponent<Cocos2Unity.Runtime.MeshTransform>();
                }
                mt.Skew = skewValue;
            }
        }
        private void BindScale(GameObject node, ModVector2 scale)
        {
            var rt = node.GetComponent<RectTransform>();
            rt.localScale = new Vector3(scale.X, scale.Y, 1);
        }
        private void BindFiller(GameObject node, ModFiller filler)
        {
            Image image = null;
            switch (filler.Type)
            {
                case ModFiller.ModType.None:
                    break;
                case ModFiller.ModType.Color:
                    if (filler.Color.Type == ModColorVector.ModType.None)
                        break;
                    if (!node.TryGetComponent<Image>(out image))
                        image = node.AddComponent<Image>();
                    var color = filler.Color.Color ?? new ModColor(0, 0, 0, 0);
                    image.color = new Color(color.R, color.G, color.B, color.A);
                    image.sprite = null;
                    break;

                case ModFiller.ModType.Sprite:
                    if (!node.TryGetComponent<Image>(out image))
                        image = node.AddComponent<Image>();
                    image.color = Color.white;
                    image.sprite = GetSprite(filler.Sprite.Name);
                    break;
                case ModFiller.ModType.Node:
                    break;
                default:
                    break;
            }
        }
        private void BindColor(GameObject node, ModColor color)
        {
            var tint = new Color(color.R, color.G, color.B, color.A);
            if (tint != Color.white)
            {
                if (!node.TryGetComponent<Cocos2Unity.Runtime.MeshTransform>(out var mt))
                {
                    mt = node.AddComponent<Cocos2Unity.Runtime.MeshTransform>();
                }
                mt.Color = tint;
            }
        }

        private static Vector2 GetAnchoredPosition(GameObject go, Vector2 position)
        {
            var prt = go.transform.parent as RectTransform;
            var anchoredPosition = position;
            if (prt != null)
            {
                anchoredPosition -= prt.rect.size * prt.anchorMin;
            }
            return anchoredPosition;
        }
        private static bool SeparateRotationAndSkew(ModVector2 rotationSkew, out Vector3 pureRotation, out Vector2 pureSkew)
        {
            bool isPureRotation;
            if (Mathf.Abs(rotationSkew.X - rotationSkew.Y) > 1)
            {
                pureRotation = Vector3.zero;
                pureSkew = new Vector2(-rotationSkew.X, -rotationSkew.Y);
                isPureRotation = false;
            }
            else
            {
                pureRotation = new Vector3(0, 0, -rotationSkew.X);
                pureSkew = Vector2.zero;
                isPureRotation = true;
            }
            return isPureRotation;
        }


        public void BindFloatCurves<T>(AnimationClip clip, ModCurve<T> origin, EditorCurveBinding[] bindings, Func<T, float[]> GetValues) where T : ModBase
        {
            var mapTimeTransition = new Dictionary<float, CubicBezier>();
            var count = bindings.Length;
            var curves = new AnimationCurve[count];
            for (int i = 0; i < count; i++)
            {
                curves[i] = new AnimationCurve();
            }
            foreach (var frame in origin.KeyFrames)
            {
                var time = frame.Time;
                var values = GetValues(frame.Value);
                for (int i = 0; i < count; i++)
                {
                    var value = values[i];
                    if (value != float.NaN)
                    {
                        curves[i].AddKey(time, value);
                    }
                }
                mapTimeTransition.Add(time, frame.Transition);
            }
            for (int i = 0; i < count; i++)
            {
                var curve = curves[i];
                for (int j = 0; j < curve.keys.Length - 1; ++j)
                {
                    var time = curve.keys[j].time;
                    if (mapTimeTransition.TryGetValue(time, out var transition))
                    {
                        SetFreeCubicBezier(curve, j, transition);
                    }
                }
                AnimationUtility.SetEditorCurve(clip, bindings[i], curve);
            }
        }

        public void BindFloatCurve<T>(AnimationClip clip, ModCurve<T> origin, EditorCurveBinding binding, Func<T, float> GetValue) where T : ModBase
        {
            BindFloatCurves(clip, origin, new EditorCurveBinding[] { binding }, value => new float[] { GetValue(value) });
        }

        public void BindObjectCurve<T>(AnimationClip clip, ModCurve<T> origin, EditorCurveBinding binding, Func<T, UnityEngine.Object> GetValue) where T : ModBase
        {
            var keys = new List<ObjectReferenceKeyframe>();
            foreach (var frame in origin.KeyFrames)
            {
                var time = frame.Time;
                var value = GetValue(frame.Value);
                if (value != null)
                {
                    var key = new ObjectReferenceKeyframe();
                    key.time = time;
                    key.value = value;
                    keys.Add(key);
                }
            }
            AnimationUtility.SetObjectReferenceCurve(clip, binding, keys.ToArray());
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
