using System;
using System.Numerics;
using Dalamud.Interface.Windowing;
using ImGuiNET;

namespace MitView.Windows;

public class MainWindow : Window, IDisposable
{
    public MainWindow()
        : base("MainWindow##MitView MainWindow", ImGuiWindowFlags.NoScrollbar)
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(375, 330),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };
    }

    public void Dispose() 
    {

    }

    public override void Draw() 
    {
    }
}
