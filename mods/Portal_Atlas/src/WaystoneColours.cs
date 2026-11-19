using System;
using UnityEngine;
namespace OctoberJemini.PortalAtlas
{
    internal static class WaystoneColours
    {
        internal static Color32 StoneGrey(Color32 pixel)
        {
            // Keep the painted orange insets and warm light spill exactly intact.
            if(pixel.r>150 && pixel.r-pixel.b>85 && pixel.g-pixel.b>25)return pixel;
            float value=(.2126f*pixel.r+.7152f*pixel.g+.0722f*pixel.b)*1.12f;
            // Chromatic balance follows normal Valheim stone, retaining the
            // original painted luminance variation, joints and carved shading.
            return new Color32(Byte(value*1.03f),Byte(value),Byte(value*.93f),pixel.a);
        }
        private static byte Byte(float value){return (byte)Math.Max(0,Math.Min(255,(int)Math.Round(value)));}
    }
}
