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
        internal const string VERSION_CONSTANT = "1.0.1"; //Changing the version here updates it in all locations needed
    }
    public class DataShare
    {
        public static Vector3 CameraPosition = Vector3.zero;
    };

    [HarmonyPatchCategory(nameof(Slot_Patches))]
    [HarmonyPatch(typeof(SlotConnector), nameof(SlotConnector.UpdateData))]
    internal class Slot_Patches : ResoniteMonkey<Slot_Patches>
    {
        protected override IEnumerable<IFeaturePatch> GetFeaturePatches() => Enumerable.Empty<IFeaturePatch>();

        private static void Postfix(SlotConnector __instance)
        {
            //Logger.Info(() => "Slot");
            __instance._transform.position += DataShare.CameraPosition;
            DataShare.CameraPosition = Vector3.zero;
        }
    }
    [HarmonyPatchCategory(nameof(Camera_Patches))]
    [HarmonyPatch(typeof(HeadOutput), nameof(HeadOutput.UpdateOverridenView))]
    internal class Camera_Patches : ResoniteMonkey<Camera_Patches>
    {
        protected override IEnumerable<IFeaturePatch> GetFeaturePatches() => Enumerable.Empty<IFeaturePatch>();

        private static void Postfix(HeadOutput __instance)
        {

            switch (__instance.Type)
            {
                case HeadOutput.HeadOutputType.VR:
                    {
                        DataShare.CameraPosition = __instance.transform.position;
                        __instance.CameraRoot.position += __instance.transform.position;
                        __instance.transform.position = Vector3.zero;
                        
                        break;
                    }
                case HeadOutput.HeadOutputType.Screen:
                    {
                        DataShare.CameraPosition = __instance.transform.position;
                        __instance.transform.position = Vector3.zero;
                        return;
                    }
            }
        }
    }

}
