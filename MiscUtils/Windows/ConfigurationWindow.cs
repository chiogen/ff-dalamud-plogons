using System;
using System.Numerics;
using Dalamud.Interface.Internal;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;
using ImGuiNET;

namespace MiscUtils.Windows;


internal class ConfigurationWindow : Window
{
    private static readonly Vector4 Green = new(0.0f, 1.0f, 0.0f, 1.0f);
    private static readonly Vector4 Orange = new(1.0f, 165.0f / 255.0f, 0.0f, 1.0f);
    private static readonly Vector4 Red = new(1.0f, 0.0f, 0.0f, 1.0f);

    public ConfigurationWindow() : base("MiscUtils - Configuration")
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(345, 230),
            MaximumSize = new Vector2(345, 230)
        };

        Flags |= ImGuiWindowFlags.NoResize;
    }

    public override void Draw()
    {
        
    }
}
