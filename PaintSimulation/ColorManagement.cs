using System;
using System.Drawing;

namespace PaintSimulation
{
    public class ColorManagement
    {
        public static Color IntToRgb(int value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            return Color.FromArgb(bytes[2], bytes[1], bytes[0]);
        }

        public static int RGBtoInt(int red, int green, int blue)
        {
            return BitConverter.ToInt32(new byte[] { (byte)blue, (byte)green, (byte)red, 0x00 }, 0);
        }
        public static int RGBtoInt(Color color)
        {
            return BitConverter.ToInt32(new byte[] { color.B, color.G, color.R, 0x00 }, 0);
        }
    }
}