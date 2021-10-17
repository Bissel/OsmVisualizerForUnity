using System;
using UnityEngine;

namespace OsmVisualizer.Data.Utils
{
    public static class Colors
    {
        public static readonly Color Maroon = new Color(.5f, .0f, .0f);
        public static readonly Color Olive  = new Color(.5f, .5f, .0f);
        public static readonly Color Green  = new Color(.0f, .5f, .0f);
        public static readonly Color Teal   = new Color(.0f, .5f, .5f);
        public static readonly Color Navy   = new Color(.0f, .0f, .5f);
        public static readonly Color Purple = new Color(.5f, .0f, .5f);
        
        public static readonly Color Silver = new Color(.7f, .7f, .7f);


        /**
         * https://wiki.openstreetmap.org/wiki/DE:Key:colour
         */
        public static Color ToColor(this string color)
        {
            switch (color)
            {
                case "black":   return Color.black;
                case "gray":    return Color.gray;
                case "maroon":  return Maroon;
                case "olive":   return Olive;
                case "green":   return Green;
                case "teal":    return Teal;
                case "navy":    return Navy;
                case "purple":  return Purple;
                
                case "white":   return Color.white;
                case "silver":  return Silver;
                case "red":     return Color.red;
                case "yellow":  return Color.yellow;
                case "lime":    return Color.green;
                case "aqua":
                case "cyan":    return Color.cyan;
                case "blue":    return Color.blue;
                case "fuchsia": 
                case "magenta": return Color.magenta;
                
                default:        return color.StartsWith("#") 
                                    ? FromHex(color) 
                                    : Color.gray;
            }
        }

        public static Color FromHex(string hex)
        {
            if(hex.Length > 6)
            {
                if (hex.StartsWith("#")) hex = hex.Substring(1);
                else return Color.gray;
            }
            
            var r = int.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
            var g = int.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
            var b = int.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);

            return new Color(r / 255f, g / 255f, b / 255f);
        }
    }
}
