using System;
using UnityEngine;

namespace Azipa.DistanceFade
{
    public enum TargetMode { All, Exclude, IncludeOnly }
    public enum FadeMode { Vertex = 0, ObjectPosition = 1 }

    [Serializable]
    public sealed class DistanceFadeSettings
    {
        [Tooltip("Override _DistanceFade XYZ / 開始距離・終了距離・強度を上書き")]
        public bool overrideParameters = true;
        [Tooltip("Override _DistanceFade W independently / 裏面を影にする設定を個別に上書き")]
        public bool overrideBackface;
        [Tooltip("Off sets strength (Z) to zero / 無効時は強度Zを0にします")]
        public bool fadeEnabled = true;
        [Tooltip("X: start, Y: end, Z: strength, W: backface force shadow (lilToon 2.3.4)")]
        public Vector4 parameters = new Vector4(0.1f, 0.01f, 1f, 0f);
        public bool overrideColor;
        [ColorUsage(true, true)] public Color color = new Color(0, 0, 0, 1);
        public bool overrideMode;
        public FadeMode mode;
        public bool overrideRimColor;
        [ColorUsage(true, true)] public Color rimColor = new Color(0, 0, 0, 0);
        public bool overrideRimPower;
        [Range(0.01f, 50f)] public float rimPower = 5f;

        public Vector4 EffectiveParameters
        {
            get { var value = parameters; if (!fadeEnabled) value.z = 0; return value; }
        }

        public string ValidationError()
        {
            if (overrideParameters)
            {
                for (var i = 0; i < 3; i++)
                    if (!Finite(parameters[i])) return "Distance parameters must be finite / 距離パラメータに無効な数値があります";
                if (parameters.x < 0 || parameters.y < 0 || parameters.x == parameters.y)
                    return "Distances must be nonnegative and different; start > end is valid / 距離は0以上で異なる値にしてください（開始 > 終了も有効）";
                if (parameters.z < 0 || parameters.z > 1)
                    return "Strength must be 0–1 / 強度は0〜1です";
            }
            if (overrideBackface && (!Finite(parameters.w) || parameters.w < 0 || parameters.w > 1))
                return "Backface force must be 0–1 / 裏面設定は0〜1です";
            if (overrideMode && !Enum.IsDefined(typeof(FadeMode), mode)) return "Invalid fade mode / モードが不正です";
            if (overrideRimPower && (!Finite(rimPower) || rimPower < 0.01f || rimPower > 50)) return "Rim power must be 0.01–50 / リム強度は0.01〜50です";
            if ((overrideColor && !Finite(color)) || (overrideRimColor && !Finite(rimColor))) return "Color must be finite / カラーに無効な数値があります";
            return null;
        }
        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        private static bool Finite(Color value) => Finite(value.r) && Finite(value.g) && Finite(value.b) && Finite(value.a);
    }
}
