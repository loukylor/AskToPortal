﻿using MelonLoader;
using System.Reflection;

namespace AskToPortal
{
    class AskToPortalSettings
    {
        private const string catagory = "AskToPortalSettings";

        public static bool enabled;
        public static bool autoAcceptFriends;
        public static bool autoAcceptWorld;
        public static bool autoAcceptHome;
        public static bool autoAcceptSelf;
        public static bool onlyPortalDrop;

        public static void RegisterSettings()
        {
            MelonPrefs.RegisterCategory(catagory, "AskToPortal Settings");

            MelonPrefs.RegisterBool(catagory, nameof(enabled), true, "Enable/disable the mod");
            MelonPrefs.RegisterBool(catagory, nameof(autoAcceptFriends), false, "Automatically enter friends portals");
            MelonPrefs.RegisterBool(catagory, nameof(autoAcceptWorld), false, "Automatically enter portals that aren't player dropped");
            MelonPrefs.RegisterBool(catagory, nameof(autoAcceptHome), false, "Automatically enter home portals");
            MelonPrefs.RegisterBool(catagory, nameof(autoAcceptSelf), true, "Automatically enter your own portals");
            MelonPrefs.RegisterBool(catagory, nameof(onlyPortalDrop), false, "Only ask if a portal dropper is detected");

            OnModSettingsApplied();
        }

        public static void OnModSettingsApplied()
        {
            foreach (FieldInfo fieldInfo in typeof(AskToPortalSettings).GetFields())
            {
                fieldInfo.SetValue(null, MelonPrefs.GetBool(catagory, fieldInfo.Name));
            }
        }
    }
}
