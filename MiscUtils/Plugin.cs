using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using System.IO;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using MiscUtils.Windows;

namespace MiscUtils;

public sealed class Plugin : IDalamudPlugin
{
    private const string CommandName = "/miscutils";

    public Plugin(IDalamudPluginInterface pluginInterface, IFramework framework)
    {
        pluginInterface.Create<Service>();

        Service.Settings = pluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        Service.Settings.Initialize(pluginInterface);

        Service.Commands.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "Open Configuration Window\n"
        });

        Service.WindowSystem.AddWindow(new ConfigurationWindow());

        Service.PluginInterface.UiBuilder.Draw += Service.WindowSystem.Draw;
        Service.PluginInterface.UiBuilder.OpenConfigUi += () => Service.WindowSystem.Windows[0].IsOpen = true;
    }

    public string Name => "Misc Utils";

    public void Dispose()
    {
        Service.Commands.RemoveHandler(CommandName);
        Service.WindowSystem.RemoveAllWindows();
    }

    private void OnCommand(string command, string args)
    {
        var configWindow = Service.WindowSystem.Windows[0];

        switch (args)
        {
            case "":
                configWindow.IsOpen = !configWindow.IsOpen;
                break;
        }
    }
}
