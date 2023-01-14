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

    // ADD FIELDS TO STATSAFTERDEALINGDAMAGE
    [Serializable]
    public class StatsAfterDealingDamageAdditionalData
    {
        public float maxhealth_delta;
        public float movementSpeed_delta;
        public float jump_delta;


        public StatsAfterDealingDamageAdditionalData()
        {
            maxhealth_delta = 0f;
            movementSpeed_delta = 0f;
            jump_delta = 0f;
        }
    }
    public static class StatsAfterDealingDamageExtension
    {
        public static readonly ConditionalWeakTable<StatsAfterDealingDamage, StatsAfterDealingDamageAdditionalData> data =
            new ConditionalWeakTable<StatsAfterDealingDamage, StatsAfterDealingDamageAdditionalData>();

        public static StatsAfterDealingDamageAdditionalData GetAdditionalData(this StatsAfterDealingDamage statsAfterDealingDamage)
        {
            return data.GetOrCreateValue(statsAfterDealingDamage);
        }

        public static void AddData(this StatsAfterDealingDamage statsAfterDealingDamage, StatsAfterDealingDamageAdditionalData value)
        {
            try
            {
                data.Add(statsAfterDealingDamage, value);
            }
            catch (Exception) { }
        }
    }

    [Serializable]
    [HarmonyPatch(typeof(StatsAfterDealingDamage), "Update")]
    class StatsAfterDealingDamagePatchUpdate
    {
        // patch for StatsAfterDealingDamage.Update
        private static bool Prefix(StatsAfterDealingDamage __instance)
        {
            CharacterData data = (CharacterData)Traverse.Create(__instance).Field("data").GetValue();

            bool flag = (float)Traverse.Create(data.stats).Field("sinceDealtDamage").GetValue() < __instance.duration;
            if ((bool)Traverse.Create(__instance).Field("isOn").GetValue() != flag)
            {
                float ratio = data.health / data.maxHealth;
                Traverse.Create(__instance).Field("isOn").SetValue(flag);

                Vector3 localScale = __instance.transform.localScale;

                if ((bool)Traverse.Create(__instance).Field("isOn").GetValue())
                {   
                    // save deltas
                    __instance.GetAdditionalData().maxhealth_delta = data.maxHealth * __instance.hpMultiplier - data.maxHealth;
                    __instance.GetAdditionalData().movementSpeed_delta = data.stats.movementSpeed * __instance.movementSpeedMultiplier - data.stats.movementSpeed;
                    __instance.GetAdditionalData().jump_delta = data.stats.jump * __instance.jumpMultiplier - data.stats.jump;

                    // apply deltas
                    data.maxHealth += __instance.GetAdditionalData().maxhealth_delta;
                    data.maxHealth = Mathf.Max(data.maxHealth, 1f);
                    data.health = ratio * data.maxHealth;
                    data.stats.movementSpeed += __instance.GetAdditionalData().movementSpeed_delta;
                    data.stats.jump += __instance.GetAdditionalData().jump_delta;

                    // update player stuff
                    typeof(CharacterStatModifiers).InvokeMember("ConfigureMassAndSize",
                                BindingFlags.Instance | BindingFlags.InvokeMethod |
                                BindingFlags.NonPublic, null, data.stats, new object[] { });
                  
                    // invoke startEvent
                    __instance.startEvent.Invoke();
                    
                    return false; // skip original (BAD IDEA)
                }
                // unapply deltas
                data.maxHealth -= __instance.GetAdditionalData().maxhealth_delta;
                data.maxHealth = Mathf.Max(data.maxHealth, 1f);
                data.health = ratio * data.maxHealth;
                data.stats.movementSpeed -= __instance.GetAdditionalData().movementSpeed_delta;
                data.stats.jump -= __instance.GetAdditionalData().jump_delta;

                // reset deltas
                __instance.GetAdditionalData().maxhealth_delta = 0f;
                __instance.GetAdditionalData().movementSpeed_delta = 0f;
                __instance.GetAdditionalData().jump_delta = 0f;

                // update player stuff
                typeof(CharacterStatModifiers).InvokeMember("ConfigureMassAndSize",
                            BindingFlags.Instance | BindingFlags.InvokeMethod |
                            BindingFlags.NonPublic, null, data.stats, new object[] { });

                // invoke endEvent
                __instance.endEvent.Invoke();
            }

            return false; // skip original method (BAD IDEA)

        }
    }

    [Serializable]
    [HarmonyPatch(typeof(StatsAfterDealingDamage), "Interupt")]
    class StatsAfterDealingDamagePatchInterupt
    {
        // patch for StatsAfterDealingDamage.Interupt
        private static bool Prefix(StatsAfterDealingDamage __instance)
        {
            CharacterData data = (CharacterData)Traverse.Create(__instance).Field("data").GetValue();

            if ((bool)Traverse.Create(__instance).Field("isOn").GetValue())
            {
                // unapply deltas
                float ratio = data.health / data.maxHealth;
                data.maxHealth -= __instance.GetAdditionalData().maxhealth_delta;
                data.maxHealth = Mathf.Max(data.maxHealth, 1f);
                data.health = ratio * data.maxHealth;
                data.stats.movementSpeed -= __instance.GetAdditionalData().movementSpeed_delta;
                data.stats.jump -= __instance.GetAdditionalData().jump_delta;

                // reset deltas
                __instance.GetAdditionalData().maxhealth_delta = 0f;
                __instance.GetAdditionalData().movementSpeed_delta = 0f;
                __instance.GetAdditionalData().jump_delta = 0f;

                // update player stuff
                typeof(CharacterStatModifiers).InvokeMember("ConfigureMassAndSize",
                            BindingFlags.Instance | BindingFlags.InvokeMethod |
                            BindingFlags.NonPublic, null, data.stats, new object[] { });
                
                // invoke endEvent
                __instance.endEvent.Invoke();

                Traverse.Create(__instance).Field("isOn").SetValue(false);
            }

            return false; // skip original method (BAD IDEA)

        }
    }
}