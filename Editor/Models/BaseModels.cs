using System;
using System.Collections.Generic;

namespace Unity2DAdapter.Models
{
    public abstract class ModBase
    {

    }

    public class ModBoolean : ModBase
    {
        public bool Value { get; set; }

        private ModBoolean()
        {
        }

        public static implicit operator bool(ModBoolean t)
        {
            return t.Value;
        }

        public static implicit operator ModBoolean(bool t)
        {
            return new ModBoolean { Value = t };
        }
    }

    public class ModInt32 : ModBase
    {
        public int Value { get; set; }

        private ModInt32()
        {
        }

        public static implicit operator int(ModInt32 t)
        {
            return t.Value;
        }

        public static implicit operator ModInt32(int t)
        {
            return new ModInt32 { Value = t };
        }
    }

    public class ModSingle : ModBase
    {
        public float Value { get; set; }

        private ModSingle()
        {
        }

        public static implicit operator float(ModSingle t)
        {
            return t.Value;
        }

        public static implicit operator ModSingle(float t)
        {
            return new ModSingle { Value = t };
        }
    }

    public class ModString : ModBase
    {
        public string Value { get; set; }

        private ModString()
        {
        }

        public static implicit operator string(ModString t)
        {
            return t.Value;
        }

        public static implicit operator ModString(string t)
        {
            return new ModString { Value = t };
        }
    }

    public class ModVector2 : ModBase
    {
        private float x = 0f;
        private float y = 0f;
        public float X { get => x; set => x = value; }
        public float Y { get => y; set => y = value; }

        public ModVector2() : this(0f, 0f)
        {
        }

        public ModVector2(float x, float y)
        {
            X = x;
            Y = y;
        }

        public static ModVector2 operator +(ModVector2 a, ModVector2 b)
        {
            return new ModVector2(a.X + b.X, a.Y + b.Y);
        }
        public static ModVector2 operator -(ModVector2 a, ModVector2 b)
        {
            return new ModVector2(a.X - b.X, a.Y - b.Y);
        }
        public static ModVector2 operator *(ModVector2 a, ModVector2 b)
        {
            return new ModVector2(a.X * b.X, a.Y * b.Y);
        }
        public static ModVector2 operator /(ModVector2 a, ModVector2 b)
        {
            return new ModVector2(a.X / b.X, a.Y / b.Y);
        }
    }

    public class ModVector3 : ModBase
    {
        private float x = 0f;
        private float y = 0f;
        private float z = 0f;
        public float X { get => x; set => x = value; }
        public float Y { get => y; set => y = value; }
        public float Z { get => z; set => z = value; }

        public ModVector3() : this(0f, 0f, 0f)
        {
        }

        public ModVector3(float x, float y, float z)
        {
            X = x;
            Y = y;
            Y = z;
        }
    }

    public class ModRect : ModBase
    {
        private ModVector2 position;
        private ModVector2 size;

        public ModVector2 Position { get => position; set => position = value; }
        public ModVector2 Size { get => size; set => size = value; }
        public ModVector2 Min { get => position; set => position = value; }
        public ModVector2 Max { get => position + size; set => size = value - position; }
        public float X { get => position.X; set => position.X = value; }
        public float Y { get => position.Y; set => position.Y = value; }
        public float Width { get => size.X; set => size.X = value; }
        public float Height { get => size.Y; set => size.Y = value; }

        public ModRect() : this(new ModVector2(), new ModVector2())
        {
        }

        public ModRect(ModVector2 position, ModVector2 size)
        {
            Position = position;
            Size = size;
        }

        public ModRect(float minX, float minY, float maxX, float maxY)
        {
            Min = new ModVector2(minX, minY);
            Max = new ModVector2(maxX, maxY);
        }
    }

    public class ModColor : ModBase
    {
        private float r;
        private float g;
        private float b;
        private float a;
        public float R { get => r; set => r = Math.Min(Math.Max(value, 0f), 1f); }
        public float G { get => g; set => g = Math.Min(Math.Max(value, 0f), 1f); }
        public float B { get => b; set => b = Math.Min(Math.Max(value, 0f), 1f); }
        public float A { get => a; set => a = Math.Min(Math.Max(value, 0f), 1f); }


        public ModColor() : this(1f, 1f, 1f, 1f)
        {
        }

        public ModColor(float r, float g, float b, float a = 1f)
        {
            R = r;
            G = g;
            B = b;
            A = a;
        }
        public ModColor(int r, int g, int b, int a = 255) : this(r / 255f, g / 255f, b / 255f, a / 255f) { }
    }

    public class ModColorVector : ModBase
    {
        public enum ModType
        {
            None,
            Solid,
            Gradient,
        }
        private ModType type;
        private ModColor color1;
        private ModColor color2;
        private ModVector2 direction;
        public ModType Type { get => type; set => type = value; }
        public ModColor Color { get => color1; set => color1 = value; }
        public ModColor Color1 { get => color1; set => color1 = value; }
        public ModColor Color2 { get => color2; set => color2 = value; }
        public ModVector2 Direction { get => direction; set => direction = value; }

        public ModColorVector()
        {
            Type = ModType.None;
            Color1 = new ModColor();
            Color2 = new ModColor();
            Direction = new ModVector2();
        }

        public ModColorVector(ModColor color)
        {
            Type = ModType.Solid;
            Color1 = color;
            Color2 = new ModColor();
            Direction = new ModVector2();
        }

        public ModColorVector(ModColor color1, ModColor color2)
        {
            Type = ModType.Gradient;
            Color1 = color1;
            Color2 = color2;
            Direction = new ModVector2();
        }

        public ModColorVector(ModColor color1, ModColor color2, ModVector2 direction)
        {
            Type = ModType.Gradient;
            Color1 = color1;
            Color2 = color2;
            Direction = direction;
        }
    }

    public class ModLinkedAsset : ModBase
    {
        private string name;
        private string path;
        public string Name { get => name; set => name = value; }
        public string Path { get => path ?? name; set => path = value; }
        public ModLinkedAsset(string name, string path = null)
        {
            Name = name;
            Path = path;
        }
    }

    public class ModFiller : ModBase
    {
        public enum ModType
        {
            None,
            Color,
            Sprite,
            Node,
        }
        private ModType type;
        private object filler;

        public ModType Type { get => type; private set => type = value; }
        public ModColorVector Color
        {
            get { return Type == ModType.Color ? (ModColorVector)filler : null; }
            set { Type = ModType.Color; filler = value; }
        }
        public ModLinkedAsset Sprite
        {
            get { return Type == ModType.Sprite ? (ModLinkedAsset)filler : null; }
            set { Type = ModType.Sprite; filler = value; }
        }
        public ModLinkedAsset Node
        {
            get { return Type == ModType.Node ? (ModLinkedAsset)filler : null; }
            set { Type = ModType.Node; filler = value; }
        }

        public ModFiller(ModType type, object filler = null)
        {
            Type = type;
            switch (type)
            {
                case ModType.None:
                    break;
                case ModType.Color:
                    Color = (ModColorVector)filler;
                    break;
                case ModType.Sprite:
                    Sprite = (ModLinkedAsset)filler;
                    break;
                case ModType.Node:
                    Node = (ModLinkedAsset)filler;
                    break;
            }
        }

        public object GetFiller()
        {
            switch (Type)
            {
                case ModType.None:
                    return null;
                case ModType.Color:
                    return Color;
                case ModType.Sprite:
                    return Sprite;
                case ModType.Node:
                    return Node;
                default:
                    return null;
            }
        }

        public void SetFiller(object filler)
        {
            switch (Type)
            {
                case ModType.None:
                    break;
                case ModType.Color:
                    Color = (ModColorVector)filler;
                    break;
                case ModType.Sprite:
                    Sprite = (ModLinkedAsset)filler;
                    break;
                case ModType.Node:
                    Node = (ModLinkedAsset)filler;
                    break;
            }
        }
    }

    public class ModFrame<ModType> : ModBase where ModType : ModBase
    {
        private float time;
        private ModType value;
        private float frameRate = 60f;
        private CubicBezier transition = CubicBezier.Linear;
        public float Time { get => time; set => time = value; }
        public ModType Value { get => value; set => this.value = value; }
        public int Index { get => (int)(time * frameRate + 0.5); set => time = value / frameRate; }
        public CubicBezier Transition { get => transition; set => transition = value; }
        public float FrameRate { get => frameRate; private set => frameRate = value; }
        public ModFrame(float time, ModType value, float frameRate = 60f)
        {
            FrameRate = frameRate;
            Time = time;
            Value = value;
        }
        public ModFrame(int index, ModType value, float frameRate = 60f)
        {
            FrameRate = frameRate;
            Index = index;
            Value = value;
        }
    }

    public interface IModTimeInterval
    {
        float Duration { get; }
    }

    public class ModCurve<ModType> : ModBase, IModTimeInterval where ModType : ModBase
    {
        private float frameRate = 60f;
        private SortedList<float, ModFrame<ModType>> keyFrames;
        public float FrameRate { get => frameRate; private set => frameRate = value; }
        public IList<ModFrame<ModType>> KeyFrames{ get => keyFrames.Values; }
        public float Duration
        {
            get
            {
                float duration = 0f;
                foreach (ModFrame<ModType> frame in KeyFrames)
                {
                    duration = Math.Max(duration, frame.Time);
                }
                return duration;
            }
        }
        public ModCurve(float frameRate = 60f)
        {
            FrameRate = frameRate;
            keyFrames = new SortedList<float, ModFrame<ModType>>();
        }

        public ModFrame<ModType> AddFrame(int frameIndex, ModType value)
        {
            var frame = new ModFrame<ModType>(frameIndex, value, FrameRate);
            keyFrames[frame.Index] = frame;
            return frame;
        }

        public ModFrame<ModType> AddFrame(float time, ModType value)
        {
            var frame = new ModFrame<ModType>(time, value, FrameRate);
            keyFrames[frame.Index] = frame;
            return frame;
        }

        public void RemoveFrame(ModFrame<ModType> frame)
        {
            keyFrames.Remove(frame.Index);
        }

        public void ClearFrames()
        {
            keyFrames.Clear();
        }
    }

    public interface IModTimeline
    {
        // TODO
    }

    public class ModTimeline<T> : ModBase where T : IModTimeline
    {
        private float frameRate = 60f;
        private Dictionary<string, IModTimeInterval> curves;
        public float FrameRate { get => frameRate; private set => frameRate = value; }

        public float Duration
        {
            get
            {
                float duration = 0f;
                foreach (IModTimeInterval interval in curves.Values)
                {
                    duration = Math.Max(duration, interval.Duration);
                }
                return duration;
            }
        }

        public ModTimeline(float frameRate = 60f)
        {
            FrameRate = frameRate;
            curves = new Dictionary<string, IModTimeInterval>();
        }
        public ModCurve<ModType> AddCurve<ModType>(string propertyName) where ModType : ModBase
        {
            if (curves.ContainsKey(propertyName))
                return (ModCurve<ModType>)curves[propertyName];
            var curve = new ModCurve<ModType>(FrameRate);
            curves[propertyName] = curve;
            return curve;
        }

        public void RemoveCurve(string propertyName)
        {
            curves.Remove(propertyName);
        }

        public void ClearCurves()
        {
            curves.Clear();
        }

        public ModCurve<ModType> GetCurve<ModType>(string propertyName) where ModType : ModBase
        {
            return (ModCurve<ModType>)curves[propertyName];
        }
         public IEnumerable<string> GetPropertyNames()
         {
             return curves.Keys;
         }
    }
}
