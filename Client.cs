using CitizenFX.Core;
using CitizenFX.Core.Native;
using CitizenFX.Core.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RopeHoistingv2
{
    public class Client : BaseScript
    {

        readonly static string hookModel = "prop_hoist_hook"; //Model for hook

        static string ropeBone;

        static int hook; //Hook ID
        static int rope;
        static int _hook; //Hook net ID

        public Client()
        {
            Tick += OnTick;

            EventHandlers["clientSpawnRope"] += new Action<int, int, int>(SpawnRope);
            EventHandlers["clientDetachRope"] += new Action(DetachRope);
            EventHandlers["clientHoistUp"] += new Action(HoistUpEvent);
            EventHandlers["clientHoistDown"] += new Action(HoistDownEvent);
            EventHandlers["clientPlayAnim"] += new Action<int>(ServerPlayAnim);
            EventHandlers["clientStopAnim"] += new Action<int>(ServerStopAnim);

            API.RegisterCommand("rappel", new Action<int, List<object>, string>((src, args, raw) =>
            {
                SpawnRopeEvent();
            }), false);

            API.RegisterCommand("rappeldetachrope", new Action<int, List<object>, string>((src, args, raw) =>
            {
                DetachRopeEvent();
            }), false);

            API.RegisterCommand("rappelattach", new Action<int, List<object>, string>((src, args, raw) =>
            {
                AttachHoist();
            }), false);

            API.RegisterCommand("rappeldetach", new Action<int, List<object>, string>((src, args, raw) =>
            {
                DetachPed();
            }), false);
        }

        int SpawnHook(int player)
        {
            int vehicle = API.GetVehiclePedIsUsing(API.NetworkGetEntityFromNetworkId(player));
            Vector3 vehiclePos = API.GetEntityCoords(vehicle, false);

            hook = API.CreateObject(API.GetHashKey(hookModel), vehiclePos.X-1f, vehiclePos.Y-1f, vehiclePos.Z-1f, true, false, false);

            API.SetEntityCollision(hook, false, true);
            API.SetObjectPhysicsParams(hook, 1f, 3f, 4f, 0.1f, 0.1f, 9, 0, 0, 0, 0, 1);  //Helps with hook physics, so it doesnt spaz out

            return hook;
        }

        void SpawnRope(int vehicle, int hook, int player)
        {
            int _player = API.NetworkGetEntityFromNetworkId(player); //Host player ID

            int _vehicle = API.NetworkGetEntityFromNetworkId(vehicle); //Hoist player vehicle ID

            _hook = API.NetworkGetEntityFromNetworkId(hook); //Gets hook ID from server
            Vector3 hookPos = API.GetEntityCoords(_hook, false);

            int ropeAttachBone = API.GetEntityBoneIndexByName(API.GetVehiclePedIsUsing(_player), ropeBone);
            Vector3 ropePos = API.GetWorldPositionOfEntityBone(_vehicle, ropeAttachBone); //Get rope attach coords for rope position

            Screen.ShowNotification($"hook {hook}");//Cant get hook for non host client, but sometimes it can?

            int unkPtr = 0;
            rope = API.AddRope(0, 0, 0, 0, 0, 0, 100f, 3, 1000f, 0f, 0f, false, false, false, 0f, false, ref unkPtr);

            API.AttachEntitiesToRope(rope, _hook, _vehicle, hookPos.X, hookPos.Y, hookPos.Z, ropePos.X, ropePos.Y, ropePos.Z, 1000f, false, false, "root", "root");

            API.StartRopeWinding(rope);
            API.RopeForceLength(rope, .5f);
        }

        int HoistVehicle()
        {
            int vehicle = API.GetVehiclePedIsUsing(API.PlayerPedId());

            if (API.GetEntityBoneIndexByName(vehicle, "rope_attach_a") > 0)
            {
                ropeBone = "rope_attach_a";
                return vehicle;
            }
            else if (API.GetEntityBoneIndexByName(vehicle, "rope_attach_b") > 0)
            {
                ropeBone = "rope_attach_b";
                return vehicle;
            }
            else
            {
                Screen.ShowNotification("Vehicle Invalid");
                return 0;
            }
        }

        async void AttachHoist()
        {
            int player = API.PlayerPedId();

            API.TaskLeaveVehicle(player, API.GetVehiclePedIsUsing(player), 16); //Kick ped out of vehicle
            while (API.GetVehiclePedIsUsing(player) > 0) //Wait for ped to leave vehicle
            {
                await BaseScript.Delay(1);
            }
            API.AttachEntityToEntity(player, _hook, 0, 0f, 0f, -1f, 0f, 0f, 180f, false, false, false, false, 0, false); // Attach ped to hook

            API.ClearPedTasksImmediately(player); //Stops any animation

            TriggerServerEvent("serverPlayAnim", player); //Trigger the animation to play on every client
        }

        async void ServerPlayAnim(int player)
        {
            Vector3 PlayerPos = API.GetEntityCoords(player, true);

            API.RequestAnimDict("missrappel");
            while (API.HasAnimDictLoaded("missrappel") == false) //Request for rappel animation to load
            {
                await BaseScript.Delay(0);
            }
            if (API.IsEntityPlayingAnim(player, "missrappel", "rappel_idle", 3) == false)
            {
                API.TaskPlayAnimAdvanced(player, "missrappel", "rappel_idle", PlayerPos.X, PlayerPos.Y, PlayerPos.Z, 0f, 0f, 0f, 8f, 0f, -1, 3, -1, 0, 1);
            }
        }

        void DetachPed()
        {
            int player = API.PlayerPedId();
            API.DetachEntity(player, false, false);

            TriggerServerEvent("serverStopAnim", player);
        }

        void ServerStopAnim(int player)
        {
            if (API.IsEntityPlayingAnim(player, "missrappel", "rappel_idle", 3) == true)
            {
                API.ClearPedTasksImmediately(player); // Stops player animation
            }
        }

        void DetachRope()
        {
            API.DeleteEntity(ref _hook); //Delete hook object

            API.DeleteRope(ref rope); //Delete rope
        }


        void DetachRopeEvent()
        {
            TriggerServerEvent("serverDetachRope");
        }

        void SpawnRopeEvent()
        {
            if (HoistVehicle() > 0)
            {
                int vehicle = API.NetworkGetNetworkIdFromEntity(HoistVehicle());

                int player = API.NetworkGetNetworkIdFromEntity(API.PlayerPedId());

                int hookNet = API.NetworkGetNetworkIdFromEntity(SpawnHook(player));

                TriggerServerEvent("serverSpawnRope", vehicle, hookNet, player);
            }
        }

        void HoistUpEvent()
        {
            float rLength = API.RopeGetDistanceBetweenEnds(rope);
            if (rLength > .3)
            {
                API.StartRopeWinding(rope);
                API.StopRopeUnwindingFront(rope);
                API.RopeForceLength(rope, rLength - 0.03f);
            }
        }

        void HoistDownEvent()
        {
            float rLength = API.RopeGetDistanceBetweenEnds(rope);
            if (rLength < 100)
            {
                API.StopRopeWinding(rope);
                API.StartRopeUnwindingFront(rope);
                API.RopeForceLength(rope, rLength + 0.03f);
            }
        }

        void HoistUpAndDown()
        {
            if (API.IsControlPressed(0, 315)) //Numpad -
            {
                if (API.IsEntityAttachedToEntity(API.PlayerPedId(), _hook) == true)
                {
                    TriggerServerEvent("serverHoistUp");
                }
            }

            if (API.IsControlPressed(0, 314)) //Numpad +
            {
                if (API.IsEntityAttachedToEntity(API.PlayerPedId(), _hook) == true)
                {
                    TriggerServerEvent("serverHoistDown");
                }
            }
        }

        private async Task OnTick()
        {
            HoistUpAndDown();
        }
    }
}
