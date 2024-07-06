using System;
using System.Numerics;
using Dalamud.Interface.Windowing;
using ImGuiNET;

namespace MitView.Windows;

public class ConfigWindow : Window, IDisposable
{
    private Configuration Configuration;

    public ConfigWindow(MitView plugin) : base("MitView Config###MitView Config")
    {
        Flags = ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar |
                ImGuiWindowFlags.NoScrollWithMouse;

        Size = new Vector2(250, 170);
        SizeCondition = ImGuiCond.Always;

        Configuration = plugin.Configuration;
    }

    public void Dispose() { }

    public override void Draw()
    {
        var XOffsetValue = Configuration.XOffset;
        if (ImGui.InputFloat("X Offset", ref XOffsetValue))
        {
            Configuration.XOffset = XOffsetValue;
            Configuration.Save();
        }

        var YOffsetValue = Configuration.YOffset;
        if (ImGui.InputFloat("Y Offset", ref YOffsetValue))
        {
            Configuration.YOffset = YOffsetValue;
            Configuration.Save();
        }

        var FontSize = Configuration.FontSize;
        if (ImGui.InputFloat("Font Size", ref FontSize))
        {
            Configuration.FontSize = FontSize;
            Configuration.Save();
        }

        var ShowEffectiveHP = Configuration.ShowEffectiveHP;
        if(ImGui.Checkbox("Show Effective HP instead of Shields", ref ShowEffectiveHP)) 
        {
            Configuration.ShowEffectiveHP = ShowEffectiveHP;
            Configuration.Save();
        }

        var ShowDebug = Configuration.ShowDebug;
        if (ImGui.Checkbox("Show debug", ref ShowDebug))
        {
            Configuration.ShowDebug = ShowDebug;
            Configuration.Save();
        }
    }
}
