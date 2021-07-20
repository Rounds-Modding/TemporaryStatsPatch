using BepInEx; // requires BepInEx.dll and BepInEx.Harmony.dll
using UnityEngine; // requires UnityEngine.dll, UnityEngine.CoreModule.dll
using HarmonyLib; // requires 0Harmony.dll
using System.Collections;
using Photon.Pun;
using System.Reflection;
using System.Linq;
using System;
using System.Runtime.CompilerServices;
using Sonigon;
// requires Assembly-CSharp.dll

namespace TemporaryStatsPatch
{

    // ADD FIELDS TO STATSWHENFULLHP
    [Serializable]
    public class StatsWhenFullHPAdditionalData
    {
        public float health_delta;
        public float maxhealth_delta;
        public float size_delta;


        public StatsWhenFullHPAdditionalData()
        {
            health_delta = 0f;
            maxhealth_delta = 0f;
            size_delta = 0f;
        }
    }
    public static class StatsWhenFullHPExtension
    {
        public static readonly ConditionalWeakTable<StatsWhenFullHP, StatsWhenFullHPAdditionalData> data =
            new ConditionalWeakTable<StatsWhenFullHP, StatsWhenFullHPAdditionalData>();

        public static StatsWhenFullHPAdditionalData GetAdditionalData(this StatsWhenFullHP statsWhenFullHP)
        {
            return data.GetOrCreateValue(statsWhenFullHP);
        }

        public static void AddData(this StatsWhenFullHP statsWhenFullHP, StatsWhenFullHPAdditionalData value)
        {
            try
            {
                data.Add(statsWhenFullHP, value);
            }
            catch (Exception) { }
        }
    }

    [Serializable]
    [HarmonyPatch(typeof(StatsWhenFullHP), "Update")]
    class StatsWhenFullHPPatchUpdate
    {
        // patch for StatsWhenFullHP.Update
        private static bool Prefix(StatsWhenFullHP __instance)
        {
            CharacterData data = (CharacterData)Traverse.Create(__instance).Field("data").GetValue();

            bool flag = data.health / data.maxHealth >= __instance.healthThreshold;
            if (flag != (bool)Traverse.Create(__instance).Field("isOn").GetValue())
            {
                Traverse.Create(__instance).Field("isOn").SetValue(flag);
                if ((bool)Traverse.Create(__instance).Field("isOn").GetValue())
                {
                    if (__instance.playSound)
                    {
                        SoundManager.Instance.PlayAtPosition(__instance.soundPristineGrow, SoundManager.Instance.GetTransform(), __instance.transform);
                    }
                    // save deltas
                    __instance.GetAdditionalData().health_delta = data.health * __instance.healthMultiplier - data.health;
                    __instance.GetAdditionalData().maxhealth_delta = data.maxHealth * __instance.healthMultiplier - data.maxHealth;
                    __instance.GetAdditionalData().size_delta = data.stats.sizeMultiplier * __instance.sizeMultiplier - data.stats.sizeMultiplier;

                    // apply deltas
                    data.health += __instance.GetAdditionalData().health_delta;
                    data.maxHealth += __instance.GetAdditionalData().maxhealth_delta;
                    data.stats.sizeMultiplier += __instance.GetAdditionalData().size_delta;

                    // update player stuff
                    typeof(CharacterStatModifiers).InvokeMember("ConfigureMassAndSize",
                                BindingFlags.Instance | BindingFlags.InvokeMethod |
                                BindingFlags.NonPublic, null, data.stats, new object[] { });
                    return false; // skip original method (BAD IDEA)
                }
                if (__instance.playSound)
                {
                    SoundManager.Instance.PlayAtPosition(__instance.soundPristineShrink, SoundManager.Instance.GetTransform(), __instance.transform);
                }

                // unapply deltas
                data.health -= __instance.GetAdditionalData().health_delta;
                data.maxHealth -= __instance.GetAdditionalData().maxhealth_delta;
                data.stats.sizeMultiplier -= __instance.GetAdditionalData().size_delta;

                // reset deltas
                __instance.GetAdditionalData().health_delta = 0f;
                __instance.GetAdditionalData().maxhealth_delta = 0f;
                __instance.GetAdditionalData().size_delta = 0f;

                // update player stuff
                typeof(CharacterStatModifiers).InvokeMember("ConfigureMassAndSize",
                            BindingFlags.Instance | BindingFlags.InvokeMethod |
                            BindingFlags.NonPublic, null, data.stats, new object[] { });
            }
            return false; // skip original method (BAD IDEA)
        }
    }
}