using Elements.Core;
using FrooxEngine;
using HarmonyLib;
using MonkeyLoader.Patching;
using MonkeyLoader.Resonite;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityFrooxEngineRunner;
using System;
using UnityEngine.Assertions;

namespace MonkeyLoader.DoublePrecision
{
    public class AssemblyInfo
    {
        internal const string VERSION_CONSTANT = "1.2.0"; //Changing the version here updates it in all locations needed
    }

    public class DataShare
    {
        public static List<World> frooxWorlds = new List<World>();
        public static List<GameObject> unityWorldRoots = new List<GameObject>();
        public static List<Vector3> worldOffset = new List<Vector3>();
        //public static Vector3 FrooxEngineCameraPosition = Vector3.zero; //this may need to be added back in as a list if there are offset problems when switching worlds while moving.
    }

    [HarmonyPatchCategory(nameof(WorldInitIntercept))]
    [HarmonyPatch(typeof(World), MethodType.Constructor, new Type[] { typeof(WorldManager), typeof(bool), typeof(bool) })]
    internal class WorldInitIntercept : ResoniteMonkey<WorldInitIntercept>
    {
        protected override IEnumerable<IFeaturePatch> GetFeaturePatches() => Enumerable.Empty<IFeaturePatch>();

        private static bool IsUserspaceInitialized = false;
        private static void Postfix(World __instance)
        {
            Logger.Info(() => "Intercepted World Init, attempting to cache World reference.");
            if (IsUserspaceInitialized)
            {
                WorldConnector? worldConnector = __instance.Connector as WorldConnector;
                if (worldConnector is not null)
                {
                    DataShare.frooxWorlds.Add(__instance);
                    DataShare.unityWorldRoots.Add(worldConnector.WorldRoot);
                    DataShare.worldOffset.Add(Vector3.zero);
                    //possibly add in FrooxEngineCameraPosition list init here if needed.
                }
                else
                {
                    Logger.Error(() => "Unable to cast IWorldConnector to WorldConnector.");
                }
            }
            else
            {
                Logger.Info(() => "First init, assuming this world is Userspace, and skipping.");
                IsUserspaceInitialized = true;
            }

            Logger.Info(() => "Done! There are a total of " + DataShare.frooxWorlds.Count + " world connectors in the list.");
        }
    }


    [HarmonyPatchCategory(nameof(Camera_Patches))]
    [HarmonyPatch(typeof(HeadOutput), nameof(HeadOutput.UpdatePositioning))]
    internal class Camera_Patches : ResoniteMonkey<Camera_Patches>
    {
        protected override IEnumerable<IFeaturePatch> GetFeaturePatches() => Enumerable.Empty<IFeaturePatch>();

        private static Vector3 FrooxEngineCameraPosition = Vector3.zero;

        private static HeadOutput.HeadOutputType? prevOutputMode = null;

        private static void Postfix(HeadOutput __instance)
        {
            int index = -1;
            for (int i = 0; i < DataShare.unityWorldRoots.Count; i++)
            {
                if (DataShare.frooxWorlds[i] is null || DataShare.frooxWorlds[i].IsDestroyed || DataShare.frooxWorlds[i].IsDisposed)
                {
                    DataShare.frooxWorlds.RemoveAt(i);
                    DataShare.unityWorldRoots.RemoveAt(i);
                }
                else if (DataShare.frooxWorlds[i].Focus == World.WorldFocus.Focused)
                {
                    index = i;
                }
            }
            if (index == -1)
            {
                Logger.Error(() => "There are no valid focused worlds! Fatal error, exiting function.");
                return;
            }
            if (prevOutputMode is null) {
                prevOutputMode = __instance.Type;
            }
            Vector3 playerMotion = playerMotion = __instance.transform.position - FrooxEngineCameraPosition;
            FrooxEngineCameraPosition = __instance.transform.position;
            switch (__instance.Type)
            {
                case HeadOutput.HeadOutputType.VR:
                    {
                        if (prevOutputMode != HeadOutput.HeadOutputType.VR)
                        {
                            prevOutputMode = HeadOutput.HeadOutputType.VR;//reset prev output mode
                            DataShare.unityWorldRoots[index].transform.position = DataShare.worldOffset[index];
                            DataShare.worldOffset[index] = Vector3.zero;
                        }
                        DataShare.unityWorldRoots[index].transform.position -= playerMotion;
                        break;
                    }
                case HeadOutput.HeadOutputType.Screen:
                    {
                        if (prevOutputMode != HeadOutput.HeadOutputType.Screen)
                        {
                            prevOutputMode = HeadOutput.HeadOutputType.Screen;//reset prev output mode
                            DataShare.worldOffset[index] = DataShare.unityWorldRoots[index].transform.position;
                            DataShare.unityWorldRoots[index].transform.position = Vector3.zero;
                        }
                        DataShare.worldOffset[index] -= playerMotion;//move this to record where the world *should* be, instead of moving the world.
                        break;
                    }
            }
            prevOutputMode = __instance.Type;
            __instance.transform.position = Vector3.zero;
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