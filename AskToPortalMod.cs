using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Harmony;
using MelonLoader;
using VRC.Core;

[assembly: MelonInfo(typeof(AskToPortal.AskToPortalMod), "AskToPortal", "2.0.2", "loukylor", "https://github.com/loukylor/AskToPortal")]
[assembly: MelonGame("VRChat", "VRChat")]

namespace AskToPortal
{
    class AskToPortalMod : MelonMod
    {
        public static bool hasTriggered = false;
        public static List<string> blacklistedUserIds = new List<string>();

        public static PropertyInfo photonObject;

        public static MethodBase popupV2;
        public static MethodBase popupV2Small;
        public static MethodBase closePopup;
        public static MethodBase enterPortal;

        public override void OnApplicationStart()
        {
            AskToPortalSettings.RegisterSettings();
            if (MelonHandler.Mods.Where(mod => mod.Info.Name == "Portal Confirmation").Count() > 0)
            {
                MelonLogger.LogWarning("Use of Portal Confirmation by 404 was detected! AskToPortal is NOT Portal Confirmation. AskToPortal is simply a replacement for Portal Confirmation as 404 was BANNED from the VRChat Modding Group. If you wish to use this mod please DELETE Portal Confirmation.");
            }
            else
            {
                if (photonObject == null) photonObject = typeof(Photon.Pun.PhotonView).GetProperties().Where(propInfo => propInfo.Name.StartsWith("prop_Object")).First(); //Dunno how static the name is so getting it during runtime in

                popupV2 = typeof(VRCUiPopupManager).GetMethods()
                    .Where(mb => mb.Name.StartsWith("Method_Public_Void_String_String_String_Action_String_Action_Action_1_VRCUiPopup_") && !mb.Name.Contains("PDM") && CheckMethod(mb, "UserInterface/MenuContent/Popups/StandardPopupV2")).First();
                popupV2Small = typeof(VRCUiPopupManager).GetMethods()
                    .Where(mb => mb.Name.StartsWith("Method_Public_Void_String_String_String_Action_Action_1_VRCUiPopup_") && !mb.Name.Contains("PDM") && CheckMethod(mb, "UserInterface/MenuContent/Popups/StandardPopupV2")).First();
                closePopup = typeof(VRCUiPopupManager).GetMethods()
                    .Where(mb => mb.Name.StartsWith("Method_Public_Void_") && mb.Name.Length <= 21 && !mb.Name.Contains("PDM") && CheckMethod(mb, "POPUP")).First();
                enterPortal = typeof(PortalInternal).GetMethods()
                    .Where(mb => mb.Name.StartsWith("Method_Public_Void_") && mb.Name.Length <= 21 && CheckUsed(mb, "OnTriggerEnter")).First();
                harmonyInstance.Patch(enterPortal, prefix: new HarmonyMethod(typeof(AskToPortalMod).GetMethod("GetConfirmation", BindingFlags.Static | BindingFlags.Public)));

                MelonLogger.Log("Initialized!");
            }

        }
        public override void OnModSettingsApplied()
        {
            AskToPortalSettings.OnModSettingsApplied();
        }
        //This method is practically stolen from https://github.com/BenjaminZehowlt/DynamicBonesSafety/blob/master/DynamicBonesSafetyMod.cs
        public static bool CheckMethod(MethodBase methodBase, string match)
        {
            try
            {
                return UnhollowerRuntimeLib.XrefScans.XrefScanner.XrefScan(methodBase)
                    .Where(instance => instance.Type == UnhollowerRuntimeLib.XrefScans.XrefType.Global && instance.ReadAsObject().ToString().Contains(match)).Any();
            }
            catch { }
            return false;
        }
        public static bool CheckUsed(MethodBase methodBase, string methodName)
        {
            try
            {
                return UnhollowerRuntimeLib.XrefScans.XrefScanner.UsedBy(methodBase)
                    .Where(instance => instance.TryResolve() == null ? false : instance.TryResolve().Name.Contains(methodName)).Any();
            }
            catch { }
            return false;
        }

        [HarmonyPrefix]
        public static bool GetConfirmation(PortalInternal __instance)
        {
            if (!AskToPortalSettings.enabled) return true;

            Photon.Pun.PhotonView photonView = __instance.gameObject.GetComponent<Photon.Pun.PhotonView>();
            APIUser dropper;
            if (photonView == null)
            {
                dropper = new APIUser(displayName: "Not Player Dropped", id: "");
            }
            else
            {
                var photonObjectValue = photonObject.GetValue(photonView); //Some random photon object with player who dropped portal
                dropper = ((VRC.Player)photonObjectValue.GetType().GetProperty("field_Public_Player_0").GetValue(photonObjectValue, null)).field_Private_APIUser_0;
            }

            if (blacklistedUserIds.Contains(dropper.id)) return false;

            RoomInfo roomInfo = new RoomInfo();
            if (__instance.field_Private_String_1 == null)
            {
                roomInfo.instanceType = "Unknown";
            }
            else
            {
                roomInfo = ParseRoomId(__instance.field_Private_String_1);
            }

            //If portal dropper is not owner of private instance but still dropped the portal or world id is the public ban world or if the population is in the negatives or is above 80
            if ((roomInfo.ownerId != "" && roomInfo.ownerId != dropper.id) || __instance.field_Private_ApiWorld_0.id == "wrld_5b89c79e-c340-4510-be1b-476e9fcdedcc" || __instance.field_Private_Int32_0 < 0 || __instance.field_Private_Int32_0 > 80) roomInfo.isPortalDropper = true;

            if (roomInfo.isPortalDropper && !(AskToPortalSettings.autoAcceptSelf && dropper.id == APIUser.CurrentUser.id) && !hasTriggered)
            {
                popupV2.Invoke(VRCUiPopupManager.prop_VRCUiPopupManager_0, new object[7] { "Portal Dropper Detected!!!",
                    $"This portal was likely dropped by someone malicious! Only go into this portal if you trust {dropper.displayName}. Pressing \"Leave and Blacklist\" will blacklist {dropper.displayName}'s portals until the game restarts",
                    "Enter", (Il2CppSystem.Action) new Action(() =>
                    {
                        closePopup.Invoke(VRCUiPopupManager.prop_VRCUiPopupManager_0, null);
                        if (__instance == null)
                        {
                            popupV2Small.Invoke(VRCUiPopupManager.prop_VRCUiPopupManager_0, new object[5] {"Notice", "This portal has closed and cannot be entered anymore", "Ok", (Il2CppSystem.Action) new Action(() => closePopup.Invoke(VRCUiPopupManager.prop_VRCUiPopupManager_0, null)) ,null });
                            return;
                        }
                        hasTriggered = true;
                        try
                        {
                            enterPortal.Invoke(__instance, null);
                        }
                        catch {}
                    }), "Leave and Blacklist", (Il2CppSystem.Action) new Action(() => { closePopup.Invoke(VRCUiPopupManager.prop_VRCUiPopupManager_0, null); blacklistedUserIds.Add(dropper.id); }), null });
                return false;
            }
            else if (!hasTriggered && ShouldCheckUserPortal(dropper))
            {
                popupV2.Invoke(VRCUiPopupManager.prop_VRCUiPopupManager_0, new object[7] { "Enter This Portal?",
                    $"Do you want to enter this portal?{Environment.NewLine}World Name: {__instance.field_Private_ApiWorld_0.name}{Environment.NewLine}Dropper: {dropper.displayName}{Environment.NewLine}Instance Type: {roomInfo.instanceType}",
                    "Yes", (Il2CppSystem.Action) new Action(() =>
                    {
                        closePopup.Invoke(VRCUiPopupManager.prop_VRCUiPopupManager_0, null);
                        if (__instance == null)
                        {
                            popupV2Small.Invoke(VRCUiPopupManager.prop_VRCUiPopupManager_0, new object[5] {"Notice", "This portal has closed and cannot be entered anymore", "Ok", (Il2CppSystem.Action) new Action(() => closePopup.Invoke(VRCUiPopupManager.prop_VRCUiPopupManager_0, null)) ,null });
                            return;
                        }
                        hasTriggered = true;
                        try
                        {
                            enterPortal.Invoke(__instance, null);
                        }
                        catch {}
                    }), "No", (Il2CppSystem.Action) new Action(() => closePopup.Invoke(VRCUiPopupManager.prop_VRCUiPopupManager_0, null)), null });
                return false;
            }
            else
            {
                hasTriggered = false;
                return true;
            }
        }
        public static RoomInfo ParseRoomId(string roomId)
        {
            //Example invite room id: instanceId~private(someones user id here)~nonce(Long hex code here)
            //Example invite+ room id: instanceId~private(someones user id here)~canRequestInvite~nonce(Long hex code here)
            //Example friends room id: instanceId~friend(someones user id here)~nonce(Long hex code here)
            //Example friends+ room id: instanceId~hidden(someones user id here)~nonce(Long hex code here)
            //Example public room id: instanceId
            RoomInfo roomInfo = new RoomInfo();
            
            IEnumerator splitString = roomId.Split(new char[1] { '~' }).GetEnumerator();
            splitString.MoveNext();
            roomInfo.instanceId = (string) splitString.Current;
            try
            {
                int instanceId = int.Parse(roomInfo.instanceId);
                if (instanceId > 99998 || instanceId < 1) throw new Exception();
            }
            catch
            {
                roomInfo.isPortalDropper = true;
                return roomInfo;
            }
            if (splitString.MoveNext())
            {
                string[] tempString = ((string)splitString.Current).Split(new char[1] { '(' });
                
                switch (tempString[0])
                {
                    case "private":
                        roomInfo.instanceType = "Invite Only";
                        break;
                    case "friends":
                        roomInfo.instanceType = "Friends Only";
                        break;
                    case "hidden":
                        roomInfo.instanceType = "Friends+";
                        break;
                    default:
                        roomInfo.isPortalDropper = true;
                        return roomInfo;
                }
                try
                {
                    roomInfo.ownerId = tempString[1].TrimEnd(new char[1] { ')' });
                }
                catch (IndexOutOfRangeException) 
                {
                    roomInfo.isPortalDropper = true;
                    return roomInfo;
                }

                if (!splitString.MoveNext())
                {
                    roomInfo.isPortalDropper = true;
                    return roomInfo;
                }
                if ((string) splitString.Current == "canRequestInvite")
                {
                    roomInfo.instanceType = "Invite+";
                    splitString.MoveNext();
                }

                try
                {
                    roomInfo.nonce = ((string)splitString.Current).Split(new char[1] { '(' })[1].TrimEnd(new char[1] { ')' });
                }
                catch
                {
                    roomInfo.isPortalDropper = true;
                    return roomInfo;
                }
            }
            else
            {
                roomInfo.instanceType = "Public";
            }

            return roomInfo;
        }
        public static bool ShouldCheckUserPortal(APIUser dropper)
        {
            if ((APIUser.IsFriendsWith(dropper.id) && AskToPortalSettings.autoAcceptFriends) || (dropper.IsSelf && AskToPortalSettings.autoAcceptSelf) || (dropper.id == "" && AskToPortalSettings.autoAcceptWorld))
            {
                return false;
            }
            return true;
        }

        public class RoomInfo
        {
            public string instanceId = "";
            public string instanceType = "";
            public string ownerId = "";
            public string nonce = "";
            public bool isPortalDropper = false;
        }
    }
}
