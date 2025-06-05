using Elements.Core;
using FrooxEngine;
using HarmonyLib;
using MonkeyLoader.Patching;
using MonkeyLoader.Resonite;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityFrooxEngineRunner;

namespace MonkeyLoader.DoublePrecision
{

    public class AssemblyInfo
    {
        internal const string VERSION_CONSTANT = "1.1.0"; //Changing the version here updates it in all locations needed
    }

    public class DataShare
    {
        public static List<WorldConnector> worldConnectors = new List<WorldConnector>();
        public static Vector3 FrooxEngineCameraPosition = Vector3.zero;
        public static Vector3 UnityEngineWorldRootOffset = Vector3.zero;
        public static bool IsUserspaceInitialized = false;
    }


    [HarmonyPatchCategory(nameof(WorldConnectorInit))]
    [HarmonyPatch(typeof(WorldConnector), nameof(WorldConnector.Initialize))]
    internal class WorldConnectorInit : ResoniteMonkey<WorldConnectorInit>
    {
        protected override IEnumerable<IFeaturePatch> GetFeaturePatches() => Enumerable.Empty<IFeaturePatch>();

        private static void Postfix(WorldConnector __instance)
        {
            Logger.Info(() => "Called function to init world");
            if (DataShare.IsUserspaceInitialized)
            {
                DataShare.worldConnectors.Add(__instance);
            }
            else
            {
                Logger.Info(() => "First init, assuming this world is Userspace, and skipping.");
                DataShare.IsUserspaceInitialized = true;
            }

            Logger.Info(() => "There are a total of " + DataShare.worldConnectors.Count + " world connectors in the list.");

        }
    }


    [HarmonyPatchCategory(nameof(Slot_Patches))]
    [HarmonyPatch(typeof(SlotConnector), nameof(SlotConnector.UpdateData))]
    internal class Slot_Patches : ResoniteMonkey<Slot_Patches>
    {
        protected override IEnumerable<IFeaturePatch> GetFeaturePatches() => Enumerable.Empty<IFeaturePatch>();
        private static void Postfix(SlotConnector __instance)
        {
            foreach (WorldConnector worldConnector in DataShare.worldConnectors)
            {
                worldConnector.WorldRoot.transform.position -= DataShare.UnityEngineWorldRootOffset;
            }
            DataShare.UnityEngineWorldRootOffset = Vector3.zero;
        }
    }


    [HarmonyPatchCategory(nameof(Camera_Patches))]
    [HarmonyPatch(typeof(HeadOutput), nameof(HeadOutput.UpdatePositioning))]
    internal class Camera_Patches : ResoniteMonkey<Camera_Patches>
    {
        protected override IEnumerable<IFeaturePatch> GetFeaturePatches() => Enumerable.Empty<IFeaturePatch>();

        private static void Postfix(HeadOutput __instance)
        {

            switch (__instance.Type)
            {
                case HeadOutput.HeadOutputType.VR:
                    {
                        //Logger.Info(() => "begin debug step");
                        DataShare.UnityEngineWorldRootOffset = __instance.transform.position - DataShare.FrooxEngineCameraPosition;
                        DataShare.FrooxEngineCameraPosition = __instance.transform.position;
                        __instance.transform.position = Vector3.zero;
                        break;
                    }
                case HeadOutput.HeadOutputType.Screen:
                    {
                        __instance.transform.position = Vector3.zero;
                        break;
                    }
            }
        }
    }
}

/*
 * IMPORTANT THINGS THAT I HAVE LEARNED!!!!
 * 
 * __Instance.CameraRoot does NOT cause floating point errors, no matter how far out it is. (In the DASH)
 * __Instance.ViewPos does NOT cause floating point errors, no matter how far out it is. (In the DASH)
 * 
 * __Instance.transform.position DOES cause floating point errors! (In the DASH)
 * __Instance.transform.position is VERY IMPORTANT for VR Camera positioning.
 *  It seems to controll the player root position? playspace motion still works, but controller motion does not.
 *  
 *  ViewPos -= Transform.position makes screen camera stay still, while avatar moves (Only in screen mode?)
 *  ViewPos += Transform.position makes avatar stay still, while player camera moves (Only in screen mode?)
 * 
 * 
 */

//basic patch example.

//[HarmonyPatchCategory(nameof(Slot_Patches))]
//[HarmonyPatch(typeof(SlotConnector), nameof(SlotConnector.UpdateData))]
//internal class Slot_Patches : ResoniteMonkey<Slot_Patches>
//{
//    protected override IEnumerable<IFeaturePatch> GetFeaturePatches() => Enumerable.Empty<IFeaturePatch>();
//    private static void Postfix(SlotConnector __instance)
//    {
//        //Logger.Info(() => "Slot");
//    }
//}