//  *data from: https://easings.net/

//        ┌──────────────────────────────────┐(1,1)
//        │                  ◄──────────────────┤
//        │            (x2,y2)           xxxxxxxxxx│
//        │                          xxxxx         │
//        │                        xxx             │
//        │                      xx                │
//        │                     x                  │
//        │                    x                   │
//        │                   x                    │
//        │                   x                    │
//        │                   x                    │
//        │                  x                     │
//        │                 x                      │
//        │                x                       │
//        │              xx                        │
//        │            xxx                         │
//        │        xxxxx                           │
//        │xxxxxxxxx            (x1,y1)            │
//        ├──────────────────►                  │
//   (0,0)└──────────────────────────────────┘

using System;

namespace Wynncs.Entry
{
    public struct CubicBezier
    {
        public float x1;
        public float y1;
        public float x2;
        public float y2;

        public CubicBezier(float x1, float y1, float x2, float y2)
        {
            this.x1 = x1;
            this.y1 = y1;
            this.x2 = x2;
            this.y2 = y2;
        }

        public CubicBezier(double x1, double y1, double x2, double y2)
        {
            this.x1 = (float)x1;
            this.y1 = (float)y1;
            this.x2 = (float)x2;
            this.y2 = (float)y2;
        }

        private static float FAST_CBRT(float x)
        {
            return (float)(((x) < 0) ? -Math.Exp(Math.Log(-(x)) / 3.0f) : Math.Exp(Math.Log(x) / 3.0f));
        }

        private static void CHECK_CUBIC(float val, string err = "ERROR")
        {
            if (val < 0 || val > 1)
            {
                throw new Exception(err);
            }
        }

        /// <summary>
        /// 根据 x 的值，算出所处的时间 t
        /// </summary>
        /// <param name="x"></param>
        /// <returns>时间 t</returns>
        public float SolveT(float x)
        {
            CHECK_CUBIC(x);
            var d = -x;
            var c = x1 * 3f;
            var b = x2 * 3f - c - c;
            var a = 1f - b - c;

            float t;
            if (a != 0)
            {
                // Solution of a t^3 + b t^2 + c t + d = 0 :
                // https://math.stackexchange.com/questions/1908861/using-trig-identity-to-solve-a-cubic-equation
                // let x = A*cos t + B
                float p = -b / (3.0F * a);
                float p2 = p * p;
                float p3 = p2 * p;

                float q = p3 + (b * c - 3.0F * a * d) / (6.0F * a * a);
                float q2 = q * q;

                float r = c / (3.0F * a);
                float rmp2 = r - p2;

                float s = q2 + rmp2 * rmp2 * rmp2;

                if (s < 0.0F)
                {
                    float ssi = (float)Math.Sqrt(-s);
                    float r_1 = (float)Math.Sqrt(-s + q2);
                    float phi = (float)Math.Atan2(ssi, q);

                    float r_3 = FAST_CBRT(r_1);
                    float phi_3 = phi / 3.0F;

                    // Extract cubic roots.
                    float u1 = 2.0F * r_3 * (float)Math.Cos(phi_3) + p;
                    float u2 = 2.0F * r_3 * (float)Math.Cos(phi_3 + 2.0F * (float)Math.PI / 3.0f) + p;
                    float u3 = 2.0F * r_3 * (float)Math.Cos(phi_3 - 2.0F * (float)Math.PI / 3.0f) + p;

                    if (u1 >= 0.0F && u1 <= 1.0F)
                    {
                        return u1;
                    }
                    else if (u2 >= 0.0F && u2 <= 1.0F)
                    {
                        return u2;
                    }
                    else if (u3 >= 0.0F && u3 <= 1.0F)
                    {
                        return u3;
                    }
                    else
                    {
                        // 应付精度带来的问题
                        t = (x < 0.5F) ? 0.0F : 1.0F;
                    }
                }
                else
                {
                    float ss = (float)Math.Sqrt(s);
                    float u = FAST_CBRT(q + ss) + FAST_CBRT(q - ss) + p;

                    if (u >= 0.0F && u <= 1.0F)
                    {
                        return u;
                    }
                    else
                    {
                        // 应付精度带来的问题
                        t = (x < 0.5F) ? 0.0F : 1.0F;
                    }
                }
            }
            else if (b != 0)
            {
                // Solution of b t^2 + c t + d = 0 :
                // t = ±(sqrt(c^2 - 4 b d) - c)/(2 b)
                var exp1 = 2 * b;
                var exp2 = c * c - 4 * b * d;
                var exp3 = -c / exp1;
                if (exp2 == 0)
                {
                    t = exp3;
                }
                else if (exp2 > 0)
                {
                    var exp4 = (float)Math.Sqrt(exp2) / exp1;
                    var t1 = exp3 + exp4;
                    var t2 = exp3 - exp4;
                    if ((t1 < 0 || t1 > 1) && (t2 >= 0 || t2 <= 1)) t = t2;
                    else if ((t2 < 0 || t2 > 1) && (t1 >= 0 || t1 <= 1)) t = t1;
                    else
                    {
                        // 应付精度带来的问题
                        t = (x < 0.5F) ? 0.0F : 1.0F;
                    }
                }
                else
                {
                    // 应付精度带来的问题
                    t = (x < 0.5F) ? 0.0F : 1.0F;
                }
            }
            else if (c != 0)
            {
                // Solution c t + d = 0 :
                // t = -d/c
                t = -d / c;
            }
            else
            {
                throw new Exception("Why?-03");
            }
            return t;
        }

        /// <summary>
        /// 根据时间 t 算出 x 的采样值
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public float SampleX(float t)
        {
            CHECK_CUBIC(t);
            var c = x1 * 3f;
            var b = x2 * 3f - c - c;
            var a = 1f - b - c;
            var x = ((a * t + b) * t + c) * t;
            return x;
        }

        /// <summary>
        /// 根据时间 t 算出 y 的采样值
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public float SampleY(float t)
        {
            CHECK_CUBIC(t);
            var c = y1 * 3f;
            var b = y2 * 3f - c - c;
            var a = 1f - b - c;
            var y = ((a * t + b) * t + c) * t;
            return y;
        }

        /// <summary>
        /// 根据 x 的值算出 y 的插值
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        public float Lerp(float x)
        {
            CHECK_CUBIC(x);
            var t = SolveT(x);
            var y = SampleY(t);
            return y;
        }

        /// <summary>
        /// 根据 x 的值算出 val1 到 val2 的插值
        /// </summary>
        /// <param name="val1"></param>
        /// <param name="val2"></param>
        /// <param name="x"></param>
        /// <returns></returns>
        public float Lerp(float val1, float val2, float x)
        {
            var y = Lerp(x);
            return val1 + (val2 - val1) * y;
        }

        /// <summary>
        /// 获取预设
        /// </summary>
        /// <param name="presetName">预设名</param>
        /// <param name="bezier">输出值</param>
        /// <returns>是否正确获取</returns>
        public static bool GetPreset(string presetName, out CubicBezier bezier)
        {
            switch (presetName)
            {
                case "Constant":                                   bezier = Constant;                   return true;
                // case "Costum":                                  bezier = Costum;                     return true;
                case "Linear":                                     bezier = Linear;                     return true;
                case "EaseInSine":                                 bezier = EaseInSine;                 return true;
                case "EaseOutSine":                                bezier = EaseOutSine;                return true;
                case "EaseInOutSine":                              bezier = EaseInOutSine;              return true;
                case "EaseInQuad":                                 bezier = EaseInQuad;                 return true;
                case "EaseOutQuad":                                bezier = EaseOutQuad;                return true;
                case "EaseInOutQuad":                              bezier = EaseInOutQuad;              return true;
                case "EaseInCubic":                                bezier = EaseInCubic;                return true;
                case "EaseOutCubic":                               bezier = EaseOutCubic;               return true;
                case "EaseInOutCubic":                             bezier = EaseInOutCubic;             return true;
                case "EaseInQuart":                                bezier = EaseInQuart;                return true;
                case "EaseOutQuart":                               bezier = EaseOutQuart;               return true;
                case "EaseInOutQuart":                             bezier = EaseInOutQuart;             return true;
                case "EaseInQuint":                                bezier = EaseInQuint;                return true;
                case "EaseOutQuint":                               bezier = EaseOutQuint;               return true;
                case "EaseInOutQuint":                             bezier = EaseInOutQuint;             return true;
                case "EaseInExpo":                                 bezier = EaseInExpo;                 return true;
                case "EaseOutExpo":                                bezier = EaseOutExpo;                return true;
                case "EaseInOutExpo":                              bezier = EaseInOutExpo;              return true;
                case "EaseInCirc":                                 bezier = EaseInCirc;                 return true;
                case "EaseOutCirc":                                bezier = EaseOutCirc;                return true;
                case "EaseInOutCirc":                              bezier = EaseInOutCirc;              return true;
                // case "EaseInElastic":                           bezier = EaseInElastic;              return true;
                // case "EaseOutElastic":                          bezier = EaseOutElastic;             return true;
                // case "EaseInOutElastic":                        bezier = EaseInOutElastic;           return true;
                case "EaseInBack":                                 bezier = EaseInBack;                 return true;
                case "EaseOutBack":                                bezier = EaseOutBack;                return true;
                case "EaseInOutBack":                              bezier = EaseInOutBack;              return true;
                // case "EaseInBounce":                            bezier = EaseInBounce;               return true;
                // case "EaseOutBounce":                           bezier = EaseOutBounce;              return true;
                // case "EaseInOutBounce":                         bezier = EaseInOutBounce;            return true;
                default:                                           bezier = default;                    return false;
            }
        }

        public static CubicBezier Constant                         => new CubicBezier(0, float.NegativeInfinity, 1, float.PositiveInfinity);
        // public static CubicBezier Costum                        => new CubicBezier(0, 0, 0, 0);
        public static CubicBezier Linear                           => new CubicBezier(0.25, 0.25, 0.75, 0.75);
        public static CubicBezier EaseInSine                       => new CubicBezier(0.12, 0, 0.39, 0);
        public static CubicBezier EaseOutSine                      => new CubicBezier(0.61, 1, 0.88, 1);
        public static CubicBezier EaseInOutSine                    => new CubicBezier(0.37, 0, 0.63, 1);
        public static CubicBezier EaseInQuad                       => new CubicBezier(0.11, 0, 0.5, 0);
        public static CubicBezier EaseOutQuad                      => new CubicBezier(0.5, 1, 0.89, 1);
        public static CubicBezier EaseInOutQuad                    => new CubicBezier(0.45, 0, 0.55, 1);
        public static CubicBezier EaseInCubic                      => new CubicBezier(0.32, 0, 0.67, 0);
        public static CubicBezier EaseOutCubic                     => new CubicBezier(0.33, 1, 0.68, 1);
        public static CubicBezier EaseInOutCubic                   => new CubicBezier(0.65, 0, 0.35, 1);
        public static CubicBezier EaseInQuart                      => new CubicBezier(0.5, 0, 0.75, 0);
        public static CubicBezier EaseOutQuart                     => new CubicBezier(0.25, 1, 0.5, 1);
        public static CubicBezier EaseInOutQuart                   => new CubicBezier(0.76, 0, 0.24, 1);
        public static CubicBezier EaseInQuint                      => new CubicBezier(0.64, 0, 0.78, 0);
        public static CubicBezier EaseOutQuint                     => new CubicBezier(0.22, 1, 0.36, 1);
        public static CubicBezier EaseInOutQuint                   => new CubicBezier(0.83, 0, 0.17, 1);
        public static CubicBezier EaseInExpo                       => new CubicBezier(0.7, 0, 0.84, 0);
        public static CubicBezier EaseOutExpo                      => new CubicBezier(0.16, 1, 0.3, 1);
        public static CubicBezier EaseInOutExpo                    => new CubicBezier(0.87, 0, 0.13, 1);
        public static CubicBezier EaseInCirc                       => new CubicBezier(0.55, 0, 1, 0.45);
        public static CubicBezier EaseOutCirc                      => new CubicBezier(0, 0.55, 0.45, 1);
        public static CubicBezier EaseInOutCirc                    => new CubicBezier(0.85, 0, 0.15, 1);
        // public static CubicBezier EaseInElastic                 => new CubicBezier(0, 0, 0, 0);
        // public static CubicBezier EaseOutElastic                => new CubicBezier(0, 0, 0, 0);
        // public static CubicBezier EaseInOutElastic              => new CubicBezier(0, 0, 0, 0);
        public static CubicBezier EaseInBack                       => new CubicBezier(0.36, 0, 0.66, -0.56);
        public static CubicBezier EaseOutBack                      => new CubicBezier(0.34, 1.56, 0.64, 1);
        public static CubicBezier EaseInOutBack                    => new CubicBezier(0.68, -0.6, 0.32, 1.6);
        // public static CubicBezier EaseInBounce                  => new CubicBezier(0, 0, 0, 0);
        // public static CubicBezier EaseOutBounce                 => new CubicBezier(0, 0, 0, 0);
        // public static CubicBezier EaseInOutBounce               => new CubicBezier(0, 0, 0, 0);
    }
}
