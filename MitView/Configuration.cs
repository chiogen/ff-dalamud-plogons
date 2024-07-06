using Dalamud.Configuration;
using Dalamud.Plugin;
using System;

namespace MitView;

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 0;

    public float XOffset { get; set; } = 0f;
    public float YOffset { get; set; } = 0f;
    public float FontSize { get; set; } = 1f;
    public bool ShowEffectiveHP { get; set; } = false;
    public bool ShowDebug { get; set; } = false;

    // the below exist just to make saving less cumbersome
    [NonSerialized]
    private IDalamudPluginInterface? PluginInterface;

    public void Initialize(IDalamudPluginInterface pluginInterface)
    {
        PluginInterface = pluginInterface;
    }

    public void Save()
    {
        PluginInterface!.SavePluginConfig(this);
    }
}
