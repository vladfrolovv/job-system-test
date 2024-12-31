using UnityEngine;

namespace Utils.Extensions
{
    public static class NumberExtensions
    {
        public static float SetPrecision(this float number, int precision)
        {
            float power = Mathf.Pow(10f, precision);
            return (int) (number * power) / power;
        }

        public static string ForceAfterCommaLength(this float number, int length)
        {
            float power = Mathf.Pow(10f, length);
            int integer = (int) (number * power);
            string s = integer.ToString();
            if (s.Length > length)
            {
                return s.Insert(s.Length - length, ".");
            }

            string prefix = "0.";
            for (int i = 0; i < length - s.Length; i++)
            {
                prefix += "0";
            }

            return prefix + s;
        }
    }
}