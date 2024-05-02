using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.UI;

namespace GearsetHelper
{
    internal class Gearsets
    {
        public static unsafe string List()
        {
            try
            {
                var playerStatePtr = PlayerState.Instance();
                return Utils.ReadString(playerStatePtr->CharacterName);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

            return "";
        }
    }
}
