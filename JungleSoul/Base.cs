using System;
using System.Linq;
using System.Collections.Generic;
using EnsoulSharp;
using EnsoulSharp.SDK;
using EnsoulSharp.SDK.Events;
using EnsoulSharp.SDK.MenuUI;
using EnsoulSharp.SDK.MenuUI.Values;
using EnsoulSharp.SDK.Prediction;
using EnsoulSharp.SDK.Utility;
using SharpDX;
using SharpDX.Direct3D9;
using Color = System.Drawing.Color;

namespace JungleSoul
{
    class Base
    {
        public static int slider(Menu submenu, string sig)
        {
            return submenu[sig].GetValue<MenuSlider>().Value;
        }

        public static ColorBGRA m_getcolor(Menu submenu, string sig)
        {
            return submenu[sig].GetValue<MenuColor>();
        }
    }

    public static class Extensions
    {
        public static bool check(this Menu menu, string sig, string sig2 = null)
        {
            return sig2 == null ? menu[sig].GetValue<MenuBool>().Enabled : menu[sig][sig2].GetValue<MenuBool>().Enabled;
        }
    }
}
