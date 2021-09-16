//  * https://easings.net/

namespace Cocos2Unity
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

        public CubicBezier FromMode(string mode)
        {
            switch (mode)
            {
                case "Constant":                                return Constant;
                // case "Costum":                                  return Costum;
                case "Linear":                                  return Linear;
                case "EaseInSine":                              return EaseInSine;
                case "EaseOutSine":                             return EaseOutSine;
                case "EaseInOutSine":                           return EaseInOutSine;
                case "EaseInQuad":                              return EaseInQuad;
                case "EaseOutQuad":                             return EaseOutQuad;
                case "EaseInOutQuad":                           return EaseInOutQuad;
                case "EaseInCubic":                             return EaseInCubic;
                case "EaseOutCubic":                            return EaseOutCubic;
                case "EaseInOutCubic":                          return EaseInOutCubic;
                case "EaseInQuart":                             return EaseInQuart;
                case "EaseOutQuart":                            return EaseOutQuart;
                case "EaseInOutQuart":                          return EaseInOutQuart;
                case "EaseInQuint":                             return EaseInQuint;
                case "EaseOutQuint":                            return EaseOutQuint;
                case "EaseInOutQuint":                          return EaseInOutQuint;
                case "EaseInExpo":                              return EaseInExpo;
                case "EaseOutExpo":                             return EaseOutExpo;
                case "EaseInOutExpo":                           return EaseInOutExpo;
                case "EaseInCirc":                              return EaseInCirc;
                case "EaseOutCirc":                             return EaseOutCirc;
                case "EaseInOutCirc":                           return EaseInOutCirc;
                // case "EaseInElastic":                           return EaseInElastic;
                // case "EaseOutElastic":                          return EaseOutElastic;
                // case "EaseInOutElastic":                        return EaseInOutElastic;
                case "EaseInBack":                              return EaseInBack;
                case "EaseOutBack":                             return EaseOutBack;
                case "EaseInOutBack":                           return EaseInOutBack;
                // case "EaseInBounce":                            return EaseInBounce;
                // case "EaseOutBounce":                           return EaseOutBounce;
                // case "EaseInOutBounce":                         return EaseInOutBounce;
                default:                                        return new CubicBezier(0, 0, 0, 0);
            }
        }


        public static CubicBezier Constant                      => new CubicBezier(0, float.NegativeInfinity, 1, float.PositiveInfinity);
        // public static CubicBezier Costum                        => new CubicBezier(0, 0, 0, 0);
        public static CubicBezier Linear                        => new CubicBezier(0.25, 0.25, 0.75, 0.75);
        public static CubicBezier EaseInSine                    => new CubicBezier(0.12, 0, 0.39, 0);
        public static CubicBezier EaseOutSine                   => new CubicBezier(0.61, 1, 0.88, 1);
        public static CubicBezier EaseInOutSine                 => new CubicBezier(0.37, 0, 0.63, 1);
        public static CubicBezier EaseInQuad                    => new CubicBezier(0.11, 0, 0.5, 0);
        public static CubicBezier EaseOutQuad                   => new CubicBezier(0.5, 1, 0.89, 1);
        public static CubicBezier EaseInOutQuad                 => new CubicBezier(0.45, 0, 0.55, 1);
        public static CubicBezier EaseInCubic                   => new CubicBezier(0.32, 0, 0.67, 0);
        public static CubicBezier EaseOutCubic                  => new CubicBezier(0.33, 1, 0.68, 1);
        public static CubicBezier EaseInOutCubic                => new CubicBezier(0.65, 0, 0.35, 1);
        public static CubicBezier EaseInQuart                   => new CubicBezier(0.5, 0, 0.75, 0);
        public static CubicBezier EaseOutQuart                  => new CubicBezier(0.25, 1, 0.5, 1);
        public static CubicBezier EaseInOutQuart                => new CubicBezier(0.76, 0, 0.24, 1);
        public static CubicBezier EaseInQuint                   => new CubicBezier(0.64, 0, 0.78, 0);
        public static CubicBezier EaseOutQuint                  => new CubicBezier(0.22, 1, 0.36, 1);
        public static CubicBezier EaseInOutQuint                => new CubicBezier(0.83, 0, 0.17, 1);
        public static CubicBezier EaseInExpo                    => new CubicBezier(0.7, 0, 0.84, 0);
        public static CubicBezier EaseOutExpo                   => new CubicBezier(0.16, 1, 0.3, 1);
        public static CubicBezier EaseInOutExpo                 => new CubicBezier(0.87, 0, 0.13, 1);
        public static CubicBezier EaseInCirc                    => new CubicBezier(0.55, 0, 1, 0.45);
        public static CubicBezier EaseOutCirc                   => new CubicBezier(0, 0.55, 0.45, 1);
        public static CubicBezier EaseInOutCirc                 => new CubicBezier(0.85, 0, 0.15, 1);
        // public static CubicBezier EaseInElastic                 => new CubicBezier(0, 0, 0, 0);
        // public static CubicBezier EaseOutElastic                => new CubicBezier(0, 0, 0, 0);
        // public static CubicBezier EaseInOutElastic              => new CubicBezier(0, 0, 0, 0);
        public static CubicBezier EaseInBack                    => new CubicBezier(0.36, 0, 0.66, -0.56);
        public static CubicBezier EaseOutBack                   => new CubicBezier(0.34, 1.56, 0.64, 1);
        public static CubicBezier EaseInOutBack                 => new CubicBezier(0.68, -0.6, 0.32, 1.6);
        // public static CubicBezier EaseInBounce                  => new CubicBezier(0, 0, 0, 0);
        // public static CubicBezier EaseOutBounce                 => new CubicBezier(0, 0, 0, 0);
        // public static CubicBezier EaseInOutBounce               => new CubicBezier(0, 0, 0, 0);

    }
}
