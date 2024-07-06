using Dalamud.Game.Text.SeStringHandling;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiscUtils;

internal static class Chat
{
    public static void Print(string message)
    {
        var stringBuilder = new SeStringBuilder();
        stringBuilder.AddUiForeground(45);
        stringBuilder.AddText($"[MiscUtils] ");
        stringBuilder.AddUiForegroundOff();
        stringBuilder.AddText(message);

        Service.Chat.Print(stringBuilder.BuiltString);
    }
    public static void Error(string message)
    {
        var stringBuilder = new SeStringBuilder();
        stringBuilder.AddUiForeground(45);
        stringBuilder.AddText($"[MiscUtils] [Error] ");
        stringBuilder.AddUiForegroundOff();
        stringBuilder.AddText(message);

        Service.Chat.Print(stringBuilder.BuiltString);
    }
}
