using System;
using System.Reflection;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using PaintDotNet;
using PaintDotNet.Effects;
using PaintDotNet.Imaging;
using PaintDotNet.IndirectUI;
using PaintDotNet.PropertySystem;
using PaintDotNet.Rendering;
using IntSliderControl = System.Int32;
using System.Runtime.CompilerServices;

[assembly: AssemblyTitle("Dither plugin for Paint.NET")]
[assembly: AssemblyDescription("Dither selected pixels")]
[assembly: AssemblyConfiguration("dither")]
[assembly: AssemblyCompany("Lord Wolfenstein")]
[assembly: AssemblyProduct("Dither")]
[assembly: AssemblyCopyright("Copyright ©2024 by Lord Wolfenstein")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]
[assembly: ComVisible(false)]
[assembly: AssemblyVersion("1.0.*")]
[assembly: AssemblyMetadata("BuiltByCodeLab", "Version=6.13.9087.35650")]
[assembly: SupportedOSPlatform("Windows")]

namespace DitherEffect
{
    public class DitherSupportInfo : IPluginSupportInfo
    {
        public string Author => base.GetType().Assembly.GetCustomAttribute<AssemblyCopyrightAttribute>().Copyright;
        public string Copyright => base.GetType().Assembly.GetCustomAttribute<AssemblyDescriptionAttribute>().Description;
        public string DisplayName => base.GetType().Assembly.GetCustomAttribute<AssemblyProductAttribute>().Product;
        public Version Version => base.GetType().Assembly.GetName().Version;
        public Uri WebsiteUri => new Uri("https://www.getpaint.net/redirect/plugins.html");
    }

    [PluginSupportInfo<DitherSupportInfo>(DisplayName = "Dither")]
    public class DitherEffectPlugin : PropertyBasedBitmapEffect
    {
        public static string StaticName => "Dither";
        public static System.Drawing.Image StaticIcon => new System.Drawing.Bitmap(typeof(DitherEffectPlugin), "Dither.png");
        public static string SubmenuName => null;

        public DitherEffectPlugin()
            : base(StaticName, StaticIcon, SubmenuName, BitmapEffectOptions.Create() with { IsConfigurable = true })
        {
        }

        public enum PropertyNames
        {
            Message
        }

        #region Random Number Support
        private readonly uint RandomNumberInstanceSeed;
        private uint RandomNumberRenderSeed = 0;

        internal static class RandomNumber
        {
            public static uint InitializeSeed(uint iSeed, float x, float y)
            {
                return CombineHashCodes(
                    iSeed,
                    CombineHashCodes(
                        Hash(Unsafe.As<float, uint>(ref x)),
                        Hash(Unsafe.As<float, uint>(ref y))));
            }

            public static uint InitializeSeed(uint instSeed, Point2Int32 scenePos)
            {
                return CombineHashCodes(
                    instSeed,
                    CombineHashCodes(
                        Hash(unchecked((uint)scenePos.X)),
                        Hash(unchecked((uint)scenePos.Y))));
            }

            public static uint Hash(uint input)
            {
                uint state = input * 747796405u + 2891336453u;
                uint word = ((state >> (int)((state >> 28) + 4)) ^ state) * 277803737u;
                return (word >> 22) ^ word;
            }

            public static float NextFloat(ref uint seed)
            {
                seed = Hash(seed);
                return (seed >> 8) * 5.96046448E-08f;
            }

            public static int NextInt32(ref uint seed)
            {
                seed = Hash(seed);
                return unchecked((int)seed);
            }

            public static int NextInt32(ref uint seed, int maxValue)
            {
                seed = Hash(seed);
                return unchecked((int)(seed & 0x80000000) % maxValue);
            }

            public static int Next(ref uint seed)
            {
                seed = Hash(seed);
                return unchecked((int)seed);
            }

            public static int Next(ref uint seed, int maxValue)
            {
                seed = Hash(seed);
                return unchecked((int)(seed & 0x80000000) % maxValue);
            }

            public static byte NextByte(ref uint seed)
            {
                seed = Hash(seed);
                return (byte)(seed & 0xFF);
            }

            private static uint CombineHashCodes(uint hash1, uint hash2)
            {
                uint result = hash1;
                result = ((result << 5) + result) ^ hash2;
                return result;
            }
        }
        #endregion


        protected override PropertyCollection OnCreatePropertyCollection()
        {
            List<Property> props = new List<Property>();

            props.Add(new StringProperty(PropertyNames.Message, "Apply dithering effect?"));

            return new PropertyCollection(props);
        }

        protected override ControlInfo OnCreateConfigUI(PropertyCollection props)
        {
            ControlInfo configUI = CreateDefaultConfigUI(props);

            configUI.SetPropertyControlType(PropertyNames.Message, PropertyControlType.Label);
            configUI.SetPropertyControlValue(PropertyNames.Message, ControlInfoPropertyNames.DisplayName, string.Empty);

            return configUI;
        }

        protected override void OnCustomizeConfigUIWindowProperties(PropertyCollection props)
        {
            // Change the effect's window title
            props[ControlInfoPropertyNames.WindowTitle].Value = "Dither";
            // Add help button to effect UI
            props[ControlInfoPropertyNames.WindowHelpContentType].Value = WindowHelpContentType.PlainText;
            props[ControlInfoPropertyNames.WindowHelpContent].Value = "Dither v1,0\nCopyright ©2024 by Lord Wolfenstein\nAll rights reserved.";
            base.OnCustomizeConfigUIWindowProperties(props);
        }

        protected override void OnInitializeRenderInfo(IBitmapEffectRenderInfo renderInfo)
        {
            base.OnInitializeRenderInfo(renderInfo);
        }

        protected override void OnSetToken(PropertyBasedEffectConfigToken newToken)
        {
            base.OnSetToken(newToken);
        }

        #region User Entered Code
        // Name:
        // Submenu:
        // Author:
        // Title:
        // Version:
        // Desc:
        // Keywords:
        // URL:
        // Help:

        // For help writing a Bitmap plugin: https://boltbait.com/pdn/CodeLab/help/tutorial/bitmap/

        #region UICode
        IntSliderControl Amount1 = 0; // [0,100] Slider 1 Description
        IntSliderControl Amount2 = 0; // [0,100] Slider 2 Description
        IntSliderControl Amount3 = 0; // [0,100] Slider 3 Description
        #endregion

        protected override void OnRender(IBitmapEffectOutput output)
        {
            using IEffectInputBitmap<ColorBgra32> sourceBitmap = Environment.GetSourceBitmapBgra32();
            using IBitmapLock<ColorBgra32> sourceLock = sourceBitmap.Lock(new RectInt32(0, 0, sourceBitmap.Size));

            RectInt32 outputBounds = output.Bounds;
            using IBitmapLock<ColorBgra32> outputLock = output.LockBgra32();
            RegionPtr<ColorBgra32> outputSubRegion = outputLock.AsRegionPtr();
            var outputRegion = outputSubRegion.OffsetView(-outputBounds.Location);

            ColorBgra32 primaryColor = Environment.PrimaryColor.GetBgra32(sourceBitmap.ColorContext);
            ColorBgra32 secondaryColor = Environment.SecondaryColor.GetBgra32(sourceBitmap.ColorContext);
            var selection = Environment.Selection.RenderBounds;

            for(int y = outputBounds.Top; y < outputBounds.Bottom; ++y)
            {
                if(IsCancelRequested) return;

                for(int x = outputBounds.Left; x < outputBounds.Right; ++x)
                {
                    if(selection.Contains(x, y))
                    {
                        if(x % 2 == 0)
                        {
                            if(y % 2 == 0)
                            {
                                outputRegion[x, y] = primaryColor;
                            }
                            else
                            {
                                outputRegion[x, y] = secondaryColor;
                            }
                        }
                        else
                        {
                            if(y % 2 == 0)
                            {
                                outputRegion[x, y] = secondaryColor;
                            }
                            else
                            {
                                outputRegion[x, y] = primaryColor;
                            }
                        }
                    }
                }
            }
        }

        #endregion
    }
}
