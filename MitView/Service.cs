using Dalamud.Game.ClientState.Objects;
using Dalamud.IoC;
using Dalamud.Plugin.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dalamud.Plugin;
using Dalamud.Hooking;

namespace MitView
{
    internal class Service
    {
        [PluginService] public static IDalamudPluginInterface PluginInterface { get; private set; }
        [PluginService] public static IClientState ClientState { get; private set; }
        [PluginService] public static ICommandManager CommandManager { get; private set; }
        [PluginService] public static IPartyList PartyList { get; private set; }
        [PluginService] public static IGameGui GameGui { get; private set; }
        [PluginService] public static IFramework Framework { get; private set; }
        [PluginService] public static ITargetManager TargetManager { get; private set; }
        [PluginService] public static IGameNetwork GameNetwork { get; private set; }
        [PluginService] public static IDataManager DataManager { get; private set; }
        [PluginService] public static IGameInteropProvider GameInteropProvider { get; private set; }
    }
}
