using Elements.Core;
using FrooxEngine;
using FrooxEngine.ProtoFlux;
using HarmonyLib;
using MonkeyLoader.Patching;
using MonkeyLoader.Resonite;
using MonkeyLoader.Resonite.Features.FrooxEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityFrooxEngineRunner;

namespace MonkeyLoader.DoublePrecision
{

    public class AssemblyInfo
    {
        //setup instructions (in no particular order):
        //Replace ExampleURL with your source repo url
        internal const string VERSION_CONSTANT = "1.0.0"; //Changing the version here updates it in all locations needed
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
            DataShare.CameraPosition = __instance.transform.position;
            __instance.transform.position = Vector3.zero;
            if (__instance.Type == HeadOutput.HeadOutputType.VR)
            {
                DataShare.CameraPosition += __instance.CameraRoot.position;
                __instance.CameraRoot.position = Vector3.zero;
            }
        }
    }
}
