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
        internal const string VERSION_CONSTANT = "1.0.1"; //Changing the version here updates it in all locations needed
    }
    //public class DataShare
    //{
    //    public static Vector3 RootSlotOffset = Vector3.zero;
    //    public static Vector3 GlobalOffset = Vector3.zero;
    //    public static float3 ViewPos = float3.Zero;
    //};

    //[HarmonyPatchCategory(nameof(Slot_Patches))]
    //[HarmonyPatch(typeof(SlotConnector), nameof(SlotConnector.UpdateData))]
    //internal class Slot_Patches : ResoniteMonkey<Slot_Patches>
    //{
    //    protected override IEnumerable<IFeaturePatch> GetFeaturePatches() => Enumerable.Empty<IFeaturePatch>();

    //    private static void Postfix(SlotConnector __instance)
    //    {
    //        //Logger.Info(() => "Slot");
    //        //__instance._transform.position += DataShare.RootSlotOffset;
    //        //DataShare.RootSlotOffset = Vector3.zero;
    //    }
    //}
    //[HarmonyPatchCategory(nameof(Camera_Patches))]
    //[HarmonyPatch(typeof(HeadOutput), nameof(HeadOutput.UpdatePositioning))]
    //internal class Camera_Patches : ResoniteMonkey<Camera_Patches>
    //{
    //    protected override IEnumerable<IFeaturePatch> GetFeaturePatches() => Enumerable.Empty<IFeaturePatch>();

    //    private static void Postfix(HeadOutput __instance)
    //    {
    //        DataShare.GlobalOffset += new Vector3(0, 10f, 0);
    //        DataShare.RootSlotOffset = DataShare.GlobalOffset;
    //        float x = __instance.transform.position.x;
    //        float y = __instance.transform.position.y;
    //        float z = __instance.transform.position.z;
    //        float3 xyz = new float3(x,y,z);
    //        //DataShare.RootSlotOffset = __instance.transform.position;
    //        __instance._viewPos += xyz;
    //        //__instance.transform.position = DataShare.RootSlotOffset;
    //        //__instance.transform.position = Vector3.zero;
            
            
    //        //__instance._viewPos = DataShare.RootSlotOffset.ToEngine();
    //        return;

    //        switch (__instance.Type)
    //        {
    //            case HeadOutput.HeadOutputType.VR:
    //                {
    //                    DataShare.RootSlotOffset = DataShare.GlobalOffset - __instance.transform.position;
    //                    __instance.CameraRoot.position += __instance.transform.position;
    //                    __instance.transform.position = Vector3.zero;
                        
    //                    break;
    //                }
    //            case HeadOutput.HeadOutputType.Screen:
    //                {
    //                    /* DataShare.RootSlotOffset = DataShare.GlobalOffset - __instance.transform.position;
    //                     __instance.transform.position = Vector3.zero;*/
    //                    //__instance._viewPos = DataShare.ViewPos;
    //                    //DataShare.ViewPos = new float3(0, 1f, 0);
    //                    //DataShare.RootSlotOffset = new Vector3(0, 00010000, 0);
    //                    //__instance.transform.position = DataShare.RootSlotOffset;
    //                    //__instance.CameraRoot.position = DataShare.RootSlotOffset;
    //                    return;
    //                }
    //        }
    //    }
    //}

    //[HarmonyPatchCategory(nameof(World_Tracking_Offsets))]
    //[HarmonyPatch(typeof(World), nameof(World.UpdateLocalUserOutputPositions))]
    //internal class World_Tracking_Offsets : ResoniteMonkey<World_Tracking_Offsets>
    //{
    //    protected override IEnumerable<IFeaturePatch> GetFeaturePatches() => Enumerable.Empty<IFeaturePatch>();

    //    private static void Postfix(World __instance)
    //    {
    //        float x = DataShare.RootSlotOffset.x;
    //        float y = DataShare.RootSlotOffset.y;
    //        float z = DataShare.RootSlotOffset.z;
    //        float3 xyz = new float3 (x, y, z);
    //        //float3 xyz = new float3 (1, 0, 0);
    //        __instance.InputInterface.CustomTrackingOffset = xyz;
    //        //DataShare.RootSlotOffset = Vector3.zero;
    //    }
    //}


    public class DataShare
    {
        public static List<WorldConnector> worldConnectors = new List<WorldConnector>();
        public static Vector3 FrooxEngineCameraPosition = Vector3.zero;
        public static Vector3 UnityEngineWorldRootOffset = Vector3.zero;
    }


    [HarmonyPatchCategory(nameof(WorldConnectorInit))]
    [HarmonyPatch(typeof(WorldConnector), nameof(WorldConnector.Initialize))]
    internal class WorldConnectorInit : ResoniteMonkey<WorldConnectorInit>
    {
        protected override IEnumerable<IFeaturePatch> GetFeaturePatches() => Enumerable.Empty<IFeaturePatch>();

        private static void Postfix(WorldConnector __instance)
        {
            Logger.Info(() => "Called function to init world");
            DataShare.worldConnectors.Add(__instance);
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
            if(DataShare.worldConnectors.Count == 1)
            {
                return;
            }
            //foreach (WorldConnector connector in DataShare.worldConnectors)
            //{
            //    connector.WorldRoot.transform.position -= DataShare.UnityEngineWorldRootOffset;
            //    //Logger.Info(() => connector.WorldRoot.transform.position);
            //}

            DataShare.worldConnectors[1].WorldRoot.transform.position -= DataShare.UnityEngineWorldRootOffset;
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
                        return;
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