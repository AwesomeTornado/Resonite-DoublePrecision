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

namespace MonkeyLoader.DoublePrecision
{
    public class AssemblyInfo
    {
        internal const string VERSION_CONSTANT = "1.5.0"; //Changing the version here updates it in all locations needed
    }

    public class DataShare
    {
        public static List<World> frooxWorlds = new List<World>();
        public static List<GameObject> unityWorldRoots = new List<GameObject>();
        public static List<Vector3> FrooxCameraPosition = new List<Vector3>();
        public static List<PBS_TriplanarMaterial> FrooxMaterials = new List<PBS_TriplanarMaterial>();
        public static List<MaterialProperty?> MaterialIndexes = new List<MaterialProperty?>();
    }

    [HarmonyPatchCategory(nameof(WorldInitIntercept))]//commment this out, then uncomment after game is loaded
    [HarmonyPatch(typeof(World), MethodType.Constructor, new Type[] { typeof(WorldManager), typeof(bool), typeof(bool) })]
    internal class WorldInitIntercept : ResoniteMonkey<WorldInitIntercept>
    {
        protected override IEnumerable<IFeaturePatch> GetFeaturePatches() => Enumerable.Empty<IFeaturePatch>();

        private static int IsUserspaceInitialized = 1;
        private static bool ManualMutexErrorCatch = false;
        public static void Postfix(World __instance)
        {
            if (ManualMutexErrorCatch)
            {
                Logger.Error(() => "Something went very wrong in the previous world init.");
            }
            ManualMutexErrorCatch = true;
            Logger.Info(() => "Intercepted World Init, attempting to cache World reference.");
            if (IsUserspaceInitialized >= 1)
            {
                WorldConnector? worldConnector = __instance.Connector as WorldConnector;
                if (worldConnector is not null)
                {
                    DataShare.frooxWorlds.Add(__instance);
                    DataShare.unityWorldRoots.Add(worldConnector.WorldRoot);
                    DataShare.FrooxCameraPosition.Add(Vector3.zero);
                }
                else
                {
                    Logger.Error(() => "Unable to cast IWorldConnector to WorldConnector.");
                }
                //Shaders.InitializeWithWorld(__instance);
                //Logger.Info(() => "Shader world init done");
                //Initialize(ShaderName.PBS_Triplanar);
                //Logger.Info(() => "Shader tri");
                //Initialize(ShaderName.PBS_TriplanarSpecular);
                //Logger.Info(() => "Shader tri spec");
                //Initialize(ShaderName.PBS_TriplanarTransparent);
                //Logger.Info(() => "Shader tri trans");
                //Initialize(ShaderName.PBS_TriplanarTransparentSpecular);
                //Logger.Info(() => "Shader tri trans spec");

            }
            else
            {
                Logger.Info(() => "First init, assuming this world is Userspace, and skipping.");
                IsUserspaceInitialized++;
            }

            Logger.Info(() => "Done! There are a total of " + DataShare.frooxWorlds.Count + " world connectors in the list.");
            ManualMutexErrorCatch = false;
        }
    }


    [HarmonyPatchCategory(nameof(Camera_Patches))]
    [HarmonyPatch(typeof(HeadOutput), nameof(HeadOutput.UpdatePositioning))]
    internal class Camera_Patches : ResoniteMonkey<Camera_Patches>
    {
        protected override IEnumerable<IFeaturePatch> GetFeaturePatches() => Enumerable.Empty<IFeaturePatch>();

        private static void Postfix(HeadOutput __instance)
        {
            int index = -1;
            for (int i = 0; i < DataShare.unityWorldRoots.Count; i++)
            {
                if (DataShare.frooxWorlds[i] is null || DataShare.frooxWorlds[i].IsDestroyed || DataShare.frooxWorlds[i].IsDisposed)
                {
                    DataShare.frooxWorlds.RemoveAt(i);
                    DataShare.unityWorldRoots.RemoveAt(i);
                    DataShare.FrooxCameraPosition.RemoveAt(i);
                }
                else if (DataShare.frooxWorlds[i].Focus == World.WorldFocus.Focused)
                {
                    index = i;
                }
            }
            if (index == -1)
            {
                Logger.Error(() => "There are no valid focused worlds! Fatal error, Attepting worlds list regeneration.");
                World w = Userspace.UserspaceWorld;
                var worldsList = w.WorldManager.Worlds;
                foreach (World world in worldsList)
                {
                    WorldInitIntercept.Postfix(world);
                }
                return;
            }
            //Vector3 xyz = __instance.transform.localScale;
            //Vector3 iscl = new Vector3(1 / xyz.x, 1 / xyz.y, 1 / xyz.z);
            Vector3 playerMotion = __instance.transform.position - DataShare.FrooxCameraPosition[index];
            DataShare.FrooxCameraPosition[index] = __instance.transform.position;
            Vector3 pos = __instance.transform.position;
            __instance._viewPos -= new float3(pos.x, pos.y, pos.z);
            //Do we really need viewScale?
            DataShare.unityWorldRoots[index].transform.position -= playerMotion;

            //DataShare.unityWorldRoots[index].transform.localScale = iscl;
            //float3 vpos = __instance._viewPos;
            //float3 fxyzpos = new float3(vpos.x, iscl.y * vpos.y, vpos.z);
            //__instance._viewPos = fxyzpos;
            //__instance._viewScl = new float3(10,10,10);
            __instance.transform.position = Vector3.zero;
            Vector3 rootPos = DataShare.unityWorldRoots[index].transform.position;
            for (int i = 0; i < DataShare.FrooxMaterials.Count; i++)
            {
                if (DataShare.FrooxMaterials[i] is not null)
                {
                    if (DataShare.FrooxMaterials[i].Asset is null)

                    {
                        //Logger.Info(() => "FrooxMaterials.Asset is null, index of " + i);
                        //DataShare.FrooxMaterials.RemoveAt(i);//TODO: Don't remove it, I think it takes a bit to initialize.
                    }
                    else
                    {
                        //Logger.Info(() => DataShare.MaterialIndexes[i] + i);
                        int end = DataShare.FrooxMaterials[i]._asset.GetUnity().shader.GetPropertyCount();
                        DataShare.FrooxMaterials[i].Asset.GetUnity().SetVector("_WorldOffset", rootPos);
                        //EWWWWWW HIGHLY NESTED STATEMENT WARNING, TODO: FIX THIS
                    }
                }
                else
                {
                    Logger.Info(() => "FrooxMaterials is null, index of " + i);
                }
            }
        }
    }

    [HarmonyPatchCategory(nameof(PBS_Tri_Metal_Overhaul))]
    internal class PBS_Tri_Metal_Overhaul : ResoniteMonkey<PBS_Tri_Metal_Overhaul>
    {


        protected override IEnumerable<IFeaturePatch> GetFeaturePatches() => Enumerable.Empty<IFeaturePatch>();

        [HarmonyPatch(typeof(PBS_TriplanarMetallic), nameof(PBS_TriplanarMetallic.GetShader))]
        private static bool Prefix(PBS_TriplanarMetallic __instance, ref FrooxEngine.Shader __result)
        {
            Uri URL;
            Logger.Debug(() => "PBS_TriplanarMetallic.GetShader");
            if (__instance.Transparent)
            {
                URL = new Uri(Shaders.resdb_choco_transparent);
                __result = __instance.EnsureSharedShader(__instance._transparent, URL).Asset;
                ((StaticShader)__instance._transparent.Target).URL.DriveFrom(((StaticShader)__instance._transparent.Target).URL, true);
                if (((StaticShader)__instance._transparent.Target).URL != URL)
                {
                    Logger.Warn(() => "EnsureSharedShader returned the wrong url, attempting to manually change after applying local drive w/ writeback");
                    ((StaticShader)__instance._transparent.Target).URL.ForceSet(URL);
                }
                return false;
            }
            URL = new Uri(Shaders.resdb_choco);
            __result = __instance.EnsureSharedShader(__instance._regular, URL).Asset;
            ((StaticShader)__instance._regular.Target).URL.DriveFrom(((StaticShader)__instance._regular.Target).URL, true);
            if (((StaticShader)__instance._regular.Target).URL != URL)
            {
                Logger.Warn(() => "EnsureSharedShader returned the wrong url, attempting to manually change after applying local drive w/ writeback");
                ((StaticShader)__instance._regular.Target).URL.ForceSet(URL);
            }
            return false; //never run original function
        }

        [HarmonyPatch(typeof(PBS_TriplanarMetallic), nameof(PBS_TriplanarMetallic.InitializeSyncMembers))]//TODO: this entire patch can likely be remvoed.
        private static void Postfix(PBS_TriplanarMetallic __instance)
        {
            var w = Userspace.UserspaceWorld;
            w.RunInUpdates(5, () =>
            {
                __instance._regular.IsDrivable = true;
                __instance._regular.DriveFrom(__instance._regular, true);
                __instance._transparent.IsDrivable = true;
                __instance._transparent.DriveFrom(__instance._transparent, true);
            });
        }
    }

    [HarmonyPatchCategory(nameof(PBS_Tri_Specular_Overhaul))]
    internal class PBS_Tri_Specular_Overhaul : ResoniteMonkey<PBS_Tri_Specular_Overhaul>
    {
        protected override IEnumerable<IFeaturePatch> GetFeaturePatches() => Enumerable.Empty<IFeaturePatch>();

        [HarmonyPatch(typeof(PBS_TriplanarSpecular), nameof(PBS_TriplanarSpecular.GetShader))]
        private static bool Prefix(PBS_TriplanarSpecular __instance, ref FrooxEngine.Shader __result)
        {
            Uri URL;
            Logger.Debug(() => "PBS_TriplanarSpecular.GetShader");
            if (__instance.Transparent)
            {
                URL = new Uri(Shaders.resdb_choco_transparent_specular);
                __result = __instance.EnsureSharedShader(__instance._transparent, URL).Asset;
                ((StaticShader)__instance._transparent.Target).URL.DriveFrom(((StaticShader)__instance._transparent.Target).URL, true);
                if (((StaticShader)__instance._transparent.Target).URL != URL)
                {
                    Logger.Warn(() => "EnsureSharedShader returned the wrong url, attempting to manually change after applying local drive w/ writeback");
                    ((StaticShader)__instance._transparent.Target).URL.ForceSet(URL);
                }
                return false;
            }
            URL = new Uri(Shaders.resdb_choco_specular);
            __result = __instance.EnsureSharedShader(__instance._regular, URL).Asset;
            ((StaticShader)__instance._regular.Target).URL.DriveFrom(((StaticShader)__instance._regular.Target).URL, true);
            if (((StaticShader)__instance._regular.Target).URL != URL)
            {
                Logger.Warn(() => "EnsureSharedShader returned the wrong url, attempting to manually change after applying local drive w/ writeback");
                ((StaticShader)__instance._regular.Target).URL.ForceSet(URL);
            }
            return false; //never run original function
        }

        [HarmonyPatch(typeof(PBS_TriplanarSpecular), nameof(PBS_TriplanarSpecular.InitializeSyncMembers))]//TODO: this entire patch can likely be removed.
        private static void Postfix(PBS_TriplanarSpecular __instance)
        {
            var w = Userspace.UserspaceWorld;
            w.RunInUpdates(5, () =>
            {
                __instance._regular.IsDrivable = true;
                __instance._regular.DriveFrom(__instance._regular, true);
                __instance._transparent.IsDrivable = true;
                __instance._transparent.DriveFrom(__instance._transparent, true);
            });
        }
    }

    [HarmonyPatchCategory(nameof(TriplanarInitIntercept))]
    [HarmonyPatch(typeof(PBS_TriplanarMaterial), nameof(PBS_TriplanarMaterial.InitializeSyncMembers))]
    internal class TriplanarInitIntercept : ResoniteMonkey<TriplanarInitIntercept>
    {
        protected override IEnumerable<IFeaturePatch> GetFeaturePatches() => Enumerable.Empty<IFeaturePatch>();
        private static void Postfix(PBS_TriplanarMaterial __instance)
        {
            //Logger.Debug(() => "New Material initialized!");
            DataShare.FrooxMaterials.Add(__instance);
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
 *  ViewPos doesn't seem to do anything in VR mode.
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