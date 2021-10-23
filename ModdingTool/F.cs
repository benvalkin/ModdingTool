using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace ModdingTool
{
    public static class F
    {
        public static Color ToColor(this string hex)
        {
            return (Color)ColorConverter.ConvertFromString(hex);
        }
    }
}
