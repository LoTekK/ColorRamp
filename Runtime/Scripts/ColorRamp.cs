using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TeckArtist.Tools
{
    public class ColorRamp : ScriptableObject
    {
        [System.Serializable]
        public struct Ramp
        {
            public Gradient Gradient;
            public bool Smoothstep;
        }
        public enum GradientTypes
        {
            Horizontal,
            Vertical,
            Radial
        }
        public Ramp[] Ramps;
        public AnimationCurve TransitionCurve;
        public bool NormalizeCurve;
        public GradientMode VerticalTransitionMode;
        public Vector2Int Size = new Vector2Int(64, 4);
        public GradientTypes GradientType;
        public Texture2D Texture;
        public bool isLinear;

        public Color Evaluate(float u, float v)
        {
            if (Texture == null)
            {
                return Color.white;
            }
            return Texture.GetPixelBilinear(u, v);
        }

        public Color Evaluate(Vector2 coord)
        {
            return Evaluate(coord.x, coord.y);
        }

        public Color Evaluate(float u)
        {
            return Evaluate(u, 0.5f);
        }
    }
}