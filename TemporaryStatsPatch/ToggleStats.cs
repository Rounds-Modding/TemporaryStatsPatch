using BepInEx; // requires BepInEx.dll and BepInEx.Harmony.dll
using UnityEngine; // requires UnityEngine.dll, UnityEngine.CoreModule.dll
using HarmonyLib; // requires 0Harmony.dll
using System.Collections;
using Photon.Pun;
using System.Reflection;
using System.Linq;
using System;
using System.Runtime.CompilerServices;
// requires Assembly-CSharp.dll

namespace TemporaryStatsPatch
{

    // ADD FIELDS TO TOGGLESTATS
    [Serializable]
    public class ToggleStatsAdditionalData
    {
        public float health_delta;
        public float maxhealth_delta;
        public float movementSpeed_delta;


        public ToggleStatsAdditionalData()
        {
            health_delta = 0f;
            maxhealth_delta = 0f;
            movementSpeed_delta = 0f;
        }
    }
    public static class ToggleStatsExtension
    {
        public static readonly ConditionalWeakTable<ToggleStats, ToggleStatsAdditionalData> data =
            new ConditionalWeakTable<ToggleStats, ToggleStatsAdditionalData>();

        public static ToggleStatsAdditionalData GetAdditionalData(this ToggleStats toggleStats)
        {
            return data.GetOrCreateValue(toggleStats);
        }

        public static void AddData(this ToggleStats toggleStats, ToggleStatsAdditionalData value)
        {
            try
            {
                data.Add(toggleStats, value);
            }
            catch (Exception) { }
        }
    }

    [Serializable]
    [HarmonyPatch(typeof(ToggleStats), "TurnOn")]
    class ToggleStatsPatchTurnOn
    {
        // patch for ToggleStats.TurnOn
        private static bool Prefix(ToggleStats __instance)
        {
            CharacterData data = (CharacterData)Traverse.Create(__instance).Field("data").GetValue();

            // save deltas
            __instance.GetAdditionalData().health_delta = data.health * __instance.hpMultiplier - data.health;
            __instance.GetAdditionalData().maxhealth_delta = data.maxHealth * __instance.hpMultiplier - data.maxHealth;
            __instance.GetAdditionalData().movementSpeed_delta = data.stats.movementSpeed * __instance.movementSpeedMultiplier - data.stats.movementSpeed;

            // apply deltas
            data.health += __instance.GetAdditionalData().health_delta;
            data.maxHealth += __instance.GetAdditionalData().maxhealth_delta;
            data.stats.movementSpeed += __instance.GetAdditionalData().movementSpeed_delta;

            // update player stuff
            typeof(CharacterStatModifiers).InvokeMember("ConfigureMassAndSize",
                        BindingFlags.Instance | BindingFlags.InvokeMethod |
                        BindingFlags.NonPublic, null, data.stats, new object[] { });

            return false; // skip original method (BAD IDEA)

        }
    }

    [Serializable]
    [HarmonyPatch(typeof(ToggleStats), "TurnOff")]
    class ToggleStatsPatchTurnOff
    {
        // patch for ToggleStats.TurnOff
        private static bool Prefix(ToggleStats __instance)
        {
            CharacterData data = (CharacterData)Traverse.Create(__instance).Field("data").GetValue();

            // unapply deltas
            data.health -= __instance.GetAdditionalData().health_delta;
            data.maxHealth -= __instance.GetAdditionalData().maxhealth_delta;
            data.stats.movementSpeed -= __instance.GetAdditionalData().movementSpeed_delta;

            // reset deltas
            __instance.GetAdditionalData().health_delta = 0f;
            __instance.GetAdditionalData().maxhealth_delta = 0f;
            __instance.GetAdditionalData().movementSpeed_delta = 0f;

            // update player stuff
            typeof(CharacterStatModifiers).InvokeMember("ConfigureMassAndSize",
                        BindingFlags.Instance | BindingFlags.InvokeMethod |
                        BindingFlags.NonPublic, null, data.stats, new object[] { });

            return false; // skip original method (BAD IDEA)

        }
    }
}