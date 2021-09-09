using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Cocos2Unity
{
    public class UINodeConvertor : Convertor
    {
        protected override GameObject ObjectConvert(CsdNode node, GameObject parent = null)
        {
            // ! do not change the order!!
            var go = CreateFromPrefab(node.Prefab);
            if (!go.TryGetComponent<RectTransform>(out var rt))
                rt = go.AddComponent<RectTransform>();
            if (parent != null)
            {
                go.transform.SetParent(parent.transform, false);
            }
            ConvertCanvasGameObject_Name(go, node.Name);
            ConvertCanvasGameObject_isActive(go, node.isActive);
            ConvertCanvasGameObject_Size(go, node.Size);
            ConvertCanvasGameObject_Position(go, node.Position);
            ConvertCanvasGameObject_Rotation(go, node.Rotation);
            ConvertCanvasGameObject_Scale(go, node.Scale);
            ConvertCanvasGameObject_Pivot(go, node.Pivot);
            ConvertCanvasGameObject_Anchor(go, node.Anchor);
            ConvertCanvasGameObject_Image(go, node.Image);
            ConvertCanvasGameObject_Color(go, node.Color);
            ConvertCanvasGameObject_BackgroundColor(go, node.BackgroundColor);
            ConvertCanvasGameObject_isInteractive(go, node.isInteractive);
            ConvertCanvasGameObject_Children(go, node.Children);
            return go;
        }
        private Action<GameObject, string> ConvertCanvasGameObject_Name = (go, val) => go.name = val;
        private Action<GameObject, bool> ConvertCanvasGameObject_isActive = (go, val) => go.SetActive(val);
        private Action<GameObject, CsdSize> ConvertCanvasGameObject_Size = (go, val) => go.GetComponent<RectTransform>().sizeDelta = new Vector2(val.X, val.Y);
        private Action<GameObject, CsdSize> ConvertCanvasGameObject_Position = (go, val) =>
        {
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.zero;
            rt.anchoredPosition3D = new Vector3(val.X, val.Y, val.Z);
        };

        private Action<GameObject, CsdSize> ConvertCanvasGameObject_Rotation = (go, val) => go.GetComponent<RectTransform>().Rotate(val.X, val.Y, val.Z);
        private Action<GameObject, CsdScale> ConvertCanvasGameObject_Scale = (go, val) => go.GetComponent<RectTransform>().localScale = new Vector3(val.X, val.Y, val.Z);
        private Action<GameObject, CsdScale> ConvertCanvasGameObject_Pivot = (go, val) => go.GetComponent<RectTransform>().pivot = new Vector2(val.X, val.Y);
        private Action<GameObject, CsdScale> ConvertCanvasGameObject_Anchor = (go, val) =>
        {
            var rt = go.GetComponent<RectTransform>();
            var pos = rt.localPosition;
            var sizeDelta = rt.sizeDelta;
            rt.anchorMin = new Vector2(val.X, val.Y);
            rt.anchorMax = new Vector2(val.X, val.Y);
            rt.localPosition = pos;
            rt.sizeDelta = sizeDelta;
        };
        private Action<GameObject, CsdScale> ConvertCanvasGameObject_AnchorMax = (go, val) => go.GetComponent<RectTransform>().anchorMax = new Vector2(val.X, val.Y);
        private void ConvertCanvasGameObject_Image(GameObject go, CsdFile val) { if (val != null) LoadCanvasImage(go, val); }
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
                    var color = val.FromColor;
                    image.color = new Color(color.R, color.G, color.B, color.A);
                }
            }
        }
        private void ConvertCanvasGameObject_Children(GameObject go, List<CsdNode> val) { if (val != null) foreach (var child in val) ObjectConvert(child, go); }

        private void LoadCanvasImage(GameObject go, CsdFile imageData)
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
                    image.transform.Rotate(0, 0, 90);
                    var rt = image.GetComponent<RectTransform>();
                    rt.sizeDelta = new Vector2(rt.sizeDelta.y, rt.sizeDelta.x);
                    rt.pivot = new Vector2(rt.pivot.y, 1 - rt.pivot.x);
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


    }
}
