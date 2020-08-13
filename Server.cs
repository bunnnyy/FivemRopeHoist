using CitizenFX.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RopeHoistingServer
{
    public class Server : BaseScript
    {
        public Server()
        {
            EventHandlers["serverSpawnRope"] += new Action<Player, int, int, int>(ServerSpawnHook);
            EventHandlers["serverDetachRope"] += new Action(ServerDetachHook);
            EventHandlers["serverHoistUp"] += new Action(ServerHoistUp);
            EventHandlers["serverHoistDown"] += new Action(ServerHoistDown);
            EventHandlers["serverPlayAnim"] += new Action<Player, int>(ServerPlayAnim);
            EventHandlers["serverStopAnim"] += new Action<Player, int>(ServerStopAnim);
        }

        static void ServerSpawnHook([FromSource] Player source, int vehicle, int hook, int player)
        {
            TriggerClientEvent("clientSpawnRope", vehicle, hook, player);
        }

        static void ServerDetachHook()
        {
            TriggerClientEvent("clientDetachRope");
        }

        static void ServerHoistUp()
        {
            TriggerClientEvent("clientHoistUp");
        }
        static void ServerHoistDown()
        {
            TriggerClientEvent("clientHoistDown");
        }

        static void ServerPlayAnim([FromSource] Player source, int player)
        {
            TriggerClientEvent("clientPlayAnim", player);
        }

        static void ServerStopAnim([FromSource] Player source, int player)
        {
            TriggerClientEvent("clientStopAnim", player);
        }
    }
}
