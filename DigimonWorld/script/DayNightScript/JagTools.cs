using Microsoft.Xna.Framework;

namespace Bakin
{
    internal static class JagTools
    {
        public static float EaseInOutQuad(float t)
        {
            // Esta es una función de easing cuadrática que ralentiza al principio y al final
            return t < 0.5 ? 2 * t * t : -1 + (4 - 2 * t) * t;
        }

        public static Color GetStringRGBColor(string value, out bool fail)
        {
            var colorArray = value.Split(',');

            if (colorArray.Length != 3) fail = true;
            fail = false;
            var colorValues = new int[colorArray.Length];

            for (int i = 0; i < colorValues.Length; i++)
            {
                if (int.TryParse(colorArray[i], out colorValues[i])) continue;

                fail = true;
            }

            return new Color(colorValues[0], colorValues[1], colorValues[2]);
        }

        public static System.Drawing.Color ConvertirColorIntAColorNormal(int colorInt)
        {
            // Convierte el color entero a un objeto Color
            System.Drawing.Color colorNormal = System.Drawing.Color.FromArgb(
                (colorInt >> 16) & 0xFF,   // Componente Rojo
                (colorInt >> 8) & 0xFF,    // Componente Verde
                colorInt & 0xFF             // Componente Azul
            );

            return colorNormal;
        }
    }
}
