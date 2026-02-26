using CitizenFX.Core;
using CitizenFX.Core.Native;

using System;
using System.Collections.Generic;
using System.Linq;

namespace TR.DoorControl.Client
{
    public class Client : BaseScript
    {
        #region Constant Variables

        internal readonly IReadOnlyList<string> VEHICLE_DOOR_BONES = new List<string>()
        {
            "door_dside_f",
            "door_pside_f",
            "door_dside_r",
            "door_pside_r",
            "bonnet",
            "boot"
        };

        #endregion

        #region Constructor

        public Client()
        {
            API.RegisterKeyMapping("driverDoor", "Open the driver door of your vehicle", "KEYBOARD", "");
            API.RegisterKeyMapping("passengerDoor", "Open the front right passenger door of your vehicle", "KEYBOARD", "");
            API.RegisterKeyMapping("backLeftPassengerDoor", "Open the back left passenger door of your vehicle", "KEYBOARD", "");
            API.RegisterKeyMapping("backRightPassengerDoor", "Open the back right passenger door of your vehicle", "KEYBOARD", "");
            API.RegisterKeyMapping("openNearestDoor", "Open the nearest vehicle door", "KEYBOARD", "");
        }

        #endregion

        #region Command Handlers

        [Command("driverDoor")]
        internal async void OnDriverCommand() => ControlDoorCommand(VehicleSeat.Driver);

        [Command("passengerDoor")]
        internal async void OnPassengerCommand() => ControlDoorCommand(VehicleSeat.Passenger);

        [Command("backLeftPassengerDoor")]
        internal async void OnBackLftPassengerCommand() => ControlDoorCommand(VehicleSeat.LeftRear);

        [Command("backRightPassengerDoor")]
        internal async void OnBackRghtPassengerCommand() => ControlDoorCommand(VehicleSeat.RightRear);

        [Command("openNearestDoor")]
        internal async void OnOpenNearestDoorCommand()
        {
            if (!IsControlPressedRegardless(Control.Sprint))
            {
                return;
            }

            GetClosestDoor(out VehicleDoorIndex? doorIdx, out Vehicle nearestVeh);

            ControlDoor(null, doorIdx, nearestVeh);
        }

        #endregion

        #region Event Handlers

        [EventHandler("TR:Client:DoorControl:OpenDoor")]
        internal void OnOpenDoor(VehicleSeat seat) => ControlDoor(seat);

        #endregion

        #region Helper Methods

        internal bool IsControlPressedRegardless(Control c, int inputGroup = 0) => (Game.IsControlPressed(inputGroup, c) || Game.IsDisabledControlPressed(inputGroup, c)) && API.UpdateOnscreenKeyboard() != 0;

        internal void ControlDoorCommand(VehicleSeat seat)
        {
            if (!IsControlPressedRegardless(Control.Sprint))
            {
                return;
            }

            ControlDoor(seat);
        }

        internal void GetClosestDoor(out VehicleDoorIndex? closestDoor, out Vehicle nearestVeh)
        {
            Ped plyrPed = Game.PlayerPed;

            nearestVeh = World.GetAllVehicles()
                .Where(v => v != null && v.Position.DistanceToSquared(plyrPed.Position) < 25f)
                .OrderBy(v => v.Position.DistanceToSquared(plyrPed.Position))
                .FirstOrDefault();

            if (nearestVeh is null)
            {
                closestDoor = null;

                return;
            }

            Vector3 pedPos = plyrPed.Position;

            closestDoor = null;

            float closestDistance = float.MaxValue;

            for (int doorIdx = 0; doorIdx < VEHICLE_DOOR_BONES.Count; doorIdx++)
            {
                int boneIdx = nearestVeh.Bones[VEHICLE_DOOR_BONES[doorIdx]].Index;

                if (boneIdx != -1)
                {
                    Vector3 doorPos = nearestVeh.Bones[boneIdx].Position;

                    float distance = pedPos.DistanceToSquared(doorPos);

                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestDoor = (VehicleDoorIndex)doorIdx;
                    }
                }
            }
        }

        internal async void ControlDoor(VehicleSeat? seat = null, VehicleDoorIndex? doorIdx = null, Vehicle targetVeh = null)
        {
            Ped plyrPed = Game.PlayerPed;
            Vehicle pedVeh = targetVeh ?? plyrPed.CurrentVehicle;

            if (pedVeh is null)
            {
                return;
            }

            VehicleDoorIndex? targetDoor = doorIdx;

            if (targetDoor is null && seat != null)
            {
                switch (seat)
                {
                    case VehicleSeat.Driver:
                        targetDoor = VehicleDoorIndex.FrontLeftDoor;
                        break;

                    case VehicleSeat.Passenger:
                        targetDoor = VehicleDoorIndex.FrontRightDoor;
                        break;

                    case VehicleSeat.LeftRear:
                        targetDoor = VehicleDoorIndex.BackLeftDoor;
                        break;

                    case VehicleSeat.RightRear:
                        targetDoor = VehicleDoorIndex.BackRightDoor;
                        break;
                }
            }

            if (targetDoor is null)
            {
                return;
            }

            if (pedVeh.Doors[(VehicleDoorIndex)targetDoor].IsOpen || pedVeh.Doors[(VehicleDoorIndex)targetDoor].IsFullyOpen)
            {
                pedVeh.Doors[(VehicleDoorIndex)targetDoor].Close();
            }
            else if (pedVeh.Speed < 30f)
            {
                pedVeh.Doors[(VehicleDoorIndex)targetDoor].Open();
            }
        }

        #endregion
    }
}
