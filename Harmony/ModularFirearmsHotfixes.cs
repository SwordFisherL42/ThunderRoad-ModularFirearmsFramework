using System;
using System.Collections;
using System.Reflection;
using HarmonyLib;
using ThunderRoad;
using UnityEngine;

namespace ModularFirearms
{
    public class ModularFirearmsHotfixes : LevelModule
    {
        public bool enabled = false;
        private Harmony harmony;

        public override IEnumerator OnLoadCoroutine(Level levelDefinition)
        {
            Debug.Log("[Fisher-LevelModules] Loading Level: " + levelDefinition.name);

            try
            {
                if (this.enabled)
                {
                    if (!Harmony.HasAnyPatches("Fisher.ModularFirearms.Hotfixes"))
                    {
                        Debug.Log("[Harmony][Fisher.ModularFirearms.Hotfixes] Loading Patches.... ");
                        this.harmony = new Harmony("Fisher.ModularFirearms.Hotfixes");
                        this.harmony.PatchAll(Assembly.GetExecutingAssembly());
                        Debug.Log("[Harmony][Fisher.ModularFirearms.Hotfixes] Patches Loaded !!! ");
                    }
                }

            }
            catch (Exception ex)
            {
                Debug.Log("[Harmony][Fisher.ModularFirearms.Hotfixes][Exception] ERROR with patches: ");
                Debug.Log(ex.StackTrace);
                Debug.Log(ex.Message);
            }
            //do { CheckPlayerForArmBlade(); yield return new WaitForSeconds(0.01f); }
            //while (!playerInitialized);

            yield return null;
        }

        public override void OnUnload(Level level)
        {
            //Debug.Log("[Fisher-BladeArms] Attempting to remove blades from arms...");
            //CheckPlayerForArmBlade(true);
            //playerInitialized = false;
            base.OnUnload(level);
        }

        //[HarmonyPatch(typeof(Item))]
        //[HarmonyPatch("Start")]
        //[HarmonyPatch(new Type[] { typeof(Catalog.Category), typeof(string), typeof(bool) })]
        //static class CatalogGetDataHotfixPatch
        //{
        //    [HarmonyPrefix]
        //    static bool Prefix(Catalog.Category category, string id, ref bool logError)
        //    {
        //        Debug.Log("Patching GetData!");
        //        logError = false;
        //        return true;
        //    }
        //}


        //[HarmonyPatch(typeof(Catalog))]
        //[HarmonyPatch("GetData")]
        //[HarmonyPatch(new Type[] { typeof(Catalog.Category), typeof(string), typeof(bool) })]
        //static class CatalogGetDataHotfixPatch
        //{
        //    [HarmonyPrefix]
        //    static bool Prefix(Catalog.Category category, string id, ref bool logError)
        //    {
        //        Debug.Log("Patching GetData!");
        //        logError = false;
        //        return true;
        //    }
        //}

        //[HarmonyPatch()] // patch Verse.Widgets.IsPartiallyOrFullyTypedNumber
        //class CatalogGetDataHotfixPatch
        //{
        //    static MethodInfo TargetMethod()
        //    {
        //        return typeof(Catalog)
        //            .GetMethod("GetData")
        //            .MakeGenericMethod(typeof(ItemData));
        //    }
        //    static bool Prefix(string id, ref bool logError)
        //    {
        //        Debug.Log("Patching GetData!");
        //        logError = false;
        //        return true;
        //    }
        //}

        //[HarmonyPatch(typeof(Catalog))]
        //[HarmonyPatch("GetData")]
        //private static class CatalogGetDataHotfixPatch
        //{
        //    [HarmonyPrefix]
        //    public static bool Prefix(string id, ref bool logError)
        //    {
        //        logError = false;

        //        return true;
        //    }
        //    [HarmonyFinalizer]
        //    public static Exception Finalizer(Exception __exception)
        //    {
        //        if (__exception != null)
        //        {
        //            Debug.Log("[Harmony][Fisher.ModularFirearms.Hotfixes] Finalizer Caught Catalog.GetData Exception!");
        //            Debug.Log(__exception.StackTrace);
        //            Debug.Log(__exception.Message);
        //        }
        //        return null;
        //    }
        //}

        //public override void Update(Level level)
        //{
        //    if (level.loaded)
        //    {
        //        try
        //        {
        //            // Nothing :)
        //        }
        //        catch
        //        {
        //            Debug.LogError("[Fisher-LevelModules] EXCEPTION IN LEVEL UPDATES");
        //        }
        //    }
        //    base.Update(level);
        //}
    }
}
