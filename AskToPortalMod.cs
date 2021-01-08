using System;
using System.Linq;
using System.Reflection;
using MelonLoader;
using Harmony;
using System.IO;

[assembly: MelonInfo(typeof(AskToPortal.AskToPortalMod), "AskToPortal", "1.0.0", "loukylor", "https://github.com/loukylor/AskToPortal")]
[assembly: MelonGame("VRChat", "VRChat")]

namespace AskToPortal
{
    class AskToPortalMod : MelonMod
    {
        public static bool hasTriggered = false;
        public static MethodBase popupV1;
        public static MethodBase closePopup;
        public static MethodBase enterPortal;
        public override void OnApplicationStart()
        {
            if (MelonHandler.Mods.Where(mod => mod.Info.Name == "Portal Confirmation").Count() > 0)
            {
                MelonLogger.LogWarning("Use of Portal Confirmation by 404 was detected! AskToPortal is NOT Portal Confirmation. AskToPortal is simply a replacement for Portal Confirmation as 404 was BANNED from the VRChat Modding Group for making malicious mods. If you wish to use this mod please DELETE Portal Confirmation.");
            }
            else
            {
                popupV1 = typeof(VRCUiPopupManager).GetMethods()
                    .Where(mb => mb.Name.StartsWith("Method_Public_Void_String_String_String_Action_String_Action_Action_1_VRCUiPopup_") && !mb.Name.Contains("PDM") && CheckMethod(mb, "UserInterface/MenuContent/Popups/StandardPopupV2")).First();
                closePopup = typeof(VRCUiPopupManager).GetMethods()
                    .Where(mb => mb.Name.StartsWith("Method_Public_Void_") && mb.Name.Length <= 21 && !mb.Name.Contains("PDM") && CheckMethod(mb, "POPUP")).First();
                enterPortal = typeof(PortalInternal).GetMethods()
                    .Where(mb => mb.Name.StartsWith("Method_Public_Void_") && mb.Name.Length <= 21 && CheckUsed(mb, "OnTriggerEnter")).First();

                HarmonyInstance harmonyInstance = HarmonyInstance.Create("AskToPortal Patch");
                harmonyInstance.Patch(enterPortal, prefix: new HarmonyMethod(typeof(AskToPortalMod).GetMethod("GetConfirmation", BindingFlags.Static | BindingFlags.Public)));

                MelonLogger.Log("Initialized!");
            }

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
            if (!hasTriggered)
            {
                string dropper;
                string instanceType;
                using (StringReader sr = new StringReader(__instance.transform.Find("NameTag").GetComponentInChildren<TMPro.TextMeshPro>().text)) //IDK why but I just cant get String.Split to work
                {
                    sr.ReadLine();
                    dropper = sr.ReadLine();
                    dropper = dropper == "" ? "Not Player Dropped" : dropper;
                    instanceType = sr.ReadLine();
                    instanceType = instanceType == "" ? "Unknown" : instanceType;
                }
                popupV1.Invoke(VRCUiPopupManager.prop_VRCUiPopupManager_0, new object[7] { "Notice:",
                    $"Do you want to enter this portal?{Environment.NewLine}World Name: {__instance.field_Private_ApiWorld_0.name}{Environment.NewLine}Dropper: {dropper}{Environment.NewLine}Instance Type: {instanceType}",
                    "Yes", (Il2CppSystem.Action) new Action(() => 
                    {
                        closePopup.Invoke(VRCUiPopupManager.prop_VRCUiPopupManager_0, null);
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
    }
}
