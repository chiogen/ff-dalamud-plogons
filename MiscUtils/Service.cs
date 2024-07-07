using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin.Services;
using Dalamud.Plugin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dalamud.Game.ClientState.Objects;

namespace MiscUtils;

internal class Service
{
    public static Configuration Settings = null!;
    public static WindowSystem WindowSystem = new("HighFPSPhysics");

    [PluginService] public static IDalamudPluginInterface PluginInterface { get; private set; }
    [PluginService] public static IClientState ClientState { get; private set; }
    [PluginService] public static ICommandManager CommandManager { get; private set; }
    [PluginService] public static IPartyList PartyList { get; private set; }
    [PluginService] public static IGameGui GameGui { get; private set; }
    [PluginService] public static IFramework Framework { get; private set; }
    [PluginService] public static ITargetManager TargetManager { get; private set; }
    [PluginService] public static IDataManager DataManager { get; private set; }
    [PluginService] public static IGameInteropProvider GameInteropProvider { get; private set; }
}
