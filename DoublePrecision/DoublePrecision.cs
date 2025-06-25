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
using SkyFrost.Base;
using FrooxEngine.ProtoFlux;
using System.Security.Policy;
using Elements.Assets;
using static OfficialAssets;
using System.Xml.Linq;
using static MonkeyLoader.DoublePrecision.Shaders;
using LiteDB.Engine;

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

    static class Shaders
    {
        public const string choco = "e907ed0ca29b3534896947c4ea0004dbfa8baae96a645f3539a9516f3e9d369f";
        public const string choco_specular = "8500b5e85587ab83a88f1dec000bbbe8a3fc760ede5c1df4242a3b6372273d27";
        public const string choco_transparent = "f3d267a56478c4a756e5d3f71195fa68bb091c9486a48d136aa23ca27d042a35";
        public const string choco_transparent_specular = "121e7a5a66c70a278e99adeeb2e7ee7090c00064f3d6312cdd5026892f56023b";

        public const string froox = "d9d43057b97ff2e71b9947af38c754cde992bfa8ea75ab34d9e43859e0a0f7d3";
        public const string froox_specular = "33dcd39d588e92840eb58845d3a0404e75c038e277a92b634721dcecc16dfd9a";
        public const string froox_transparent = "abfef7119e75779d7ad31222211acc4dbcced41bacda4fd32bf04fac633c8b1b";
        public const string froox_transparent_specular = "205b3cc9c239927986895a41e6cc7323853da09b87d23c5b466a2940b4e4de92";

        public const string ext_choco = "e907ed0ca29b3534896947c4ea0004dbfa8baae96a645f3539a9516f3e9d369f.__Choco__Shader!";
        public const string ext_choco_specular = "8500b5e85587ab83a88f1dec000bbbe8a3fc760ede5c1df4242a3b6372273d27.__Choco__Shader!";
        public const string ext_choco_transparent = "f3d267a56478c4a756e5d3f71195fa68bb091c9486a48d136aa23ca27d042a35.__Choco__Shader!";
        public const string ext_choco_transparent_specular = "121e7a5a66c70a278e99adeeb2e7ee7090c00064f3d6312cdd5026892f56023b.__Choco__Shader!";

        public const string ext_froox = "d9d43057b97ff2e71b9947af38c754cde992bfa8ea75ab34d9e43859e0a0f7d3.unityshader";
        public const string ext_froox_specular = "33dcd39d588e92840eb58845d3a0404e75c038e277a92b634721dcecc16dfd9a.unityshader";
        public const string ext_froox_transparent = "abfef7119e75779d7ad31222211acc4dbcced41bacda4fd32bf04fac633c8b1b.unityshader";
        public const string ext_froox_transparent_specular = "205b3cc9c239927986895a41e6cc7323853da09b87d23c5b466a2940b4e4de92.unityshader";

        public const string resdb_choco = "resdb:///e907ed0ca29b3534896947c4ea0004dbfa8baae96a645f3539a9516f3e9d369f.__Choco__Shader!";
        public const string resdb_choco_specular = "resdb:///8500b5e85587ab83a88f1dec000bbbe8a3fc760ede5c1df4242a3b6372273d27.__Choco__Shader!";
        public const string resdb_choco_transparent = "resdb:///f3d267a56478c4a756e5d3f71195fa68bb091c9486a48d136aa23ca27d042a35.__Choco__Shader!";
        public const string resdb_choco_transparent_specular = "resdb:///121e7a5a66c70a278e99adeeb2e7ee7090c00064f3d6312cdd5026892f56023b.__Choco__Shader!";

        public const string resdb_froox = "resdb:///d9d43057b97ff2e71b9947af38c754cde992bfa8ea75ab34d9e43859e0a0f7d3.unityshader";
        public const string resdb_froox_specular = "resdb:///33dcd39d588e92840eb58845d3a0404e75c038e277a92b634721dcecc16dfd9a.unityshader";
        public const string resdb_froox_transparent = "resdb:///abfef7119e75779d7ad31222211acc4dbcced41bacda4fd32bf04fac633c8b1b.unityshader";
        public const string resdb_froox_transparent_specular = "resdb:///205b3cc9c239927986895a41e6cc7323853da09b87d23c5b466a2940b4e4de92.unityshader";

        public enum ShaderName
        {
            PBS_Triplanar,
            PBS_TriplanarSpecular,
            PBS_TriplanarTransparent,
            PBS_TriplanarTransparentSpecular,
        }

        private static PBS_TriplanarMetallic PBS_TriplanarMetallic = new PBS_TriplanarMetallic();
        private static PBS_TriplanarSpecular PBS_TriplanarSpecular = new PBS_TriplanarSpecular();

        private static PBS_TriplanarMetallic metallic1 = new PBS_TriplanarMetallic();
        private static PBS_TriplanarMetallic metallic2 = new PBS_TriplanarMetallic();
        private static PBS_TriplanarSpecular specular1 = new PBS_TriplanarSpecular();
        private static PBS_TriplanarSpecular specular2 = new PBS_TriplanarSpecular();

        private static FrooxEngine.Shader? PBS_Triplanar_shader;
        private static FrooxEngine.Shader? PBS_TriplanarSpecular_shader;
        private static FrooxEngine.Shader? PBS_TriplanarTransparent_shader;
        private static FrooxEngine.Shader? PBS_TriplanarTransparentSpecular_shader;

        private static StaticShader? PBS_Triplanar_staticShader;
        private static StaticShader? PBS_TriplanarSpecular_staticShader;
        private static StaticShader? PBS_TriplanarTransparent_staticShader;
        private static StaticShader? PBS_TriplanarTransparentSpecular_staticShader;

        public static StaticShader? GetStaticShaderFromName(ShaderName name)
        {
            switch (name)
            {
                case ShaderName.PBS_Triplanar:
                    return PBS_Triplanar_staticShader;
                case ShaderName.PBS_TriplanarSpecular:
                    return PBS_TriplanarSpecular_staticShader;
                case ShaderName.PBS_TriplanarTransparent:
                    return PBS_TriplanarTransparent_staticShader;
                case ShaderName.PBS_TriplanarTransparentSpecular:
                    return PBS_TriplanarTransparentSpecular_staticShader;
            }
            return null;
        }

        private static FrooxEngine.Shader? GetShaderFromName(ShaderName name)
        {
            switch (name)
            {
                case ShaderName.PBS_Triplanar:
                    return PBS_Triplanar_shader;
                case ShaderName.PBS_TriplanarSpecular:
                    return PBS_TriplanarSpecular_shader;
                case ShaderName.PBS_TriplanarTransparent:
                    return PBS_TriplanarTransparent_shader;
                case ShaderName.PBS_TriplanarTransparentSpecular:
                    return PBS_TriplanarTransparentSpecular_shader;
            }
            return null;
        }

        private static bool SetStaticShaderFromName(ShaderName name, StaticShader staticShader)
        {
            switch (name)
            {
                case ShaderName.PBS_Triplanar:
                    PBS_Triplanar_staticShader = staticShader;
                    return true;
                case ShaderName.PBS_TriplanarSpecular:
                    PBS_TriplanarSpecular_staticShader = staticShader;
                    return true;
                case ShaderName.PBS_TriplanarTransparent:
                    PBS_TriplanarTransparent_staticShader = staticShader;
                    return true;
                case ShaderName.PBS_TriplanarTransparentSpecular:
                    PBS_TriplanarTransparentSpecular_staticShader = staticShader;
                    return true;
            }
            return false;
        }

        private static bool SetShaderFromName(ShaderName name, FrooxEngine.Shader shader)
        {
            switch (name)
            {
                case ShaderName.PBS_Triplanar:
                    PBS_Triplanar_shader = shader;
                    return true;
                case ShaderName.PBS_TriplanarSpecular:
                    PBS_TriplanarSpecular_shader = shader;
                    return true;
                case ShaderName.PBS_TriplanarTransparent:
                    PBS_TriplanarTransparent_shader = shader;
                    return true;
                case ShaderName.PBS_TriplanarTransparentSpecular:
                    PBS_TriplanarTransparentSpecular_shader = shader;
                    return true;
            }
            return false;
        }

        private static Uri GetUriFromName(ShaderName name)
        {
            switch (name)
            {
                case ShaderName.PBS_Triplanar:
                    return new Uri(resdb_froox);
                case ShaderName.PBS_TriplanarSpecular:
                    return new Uri(resdb_froox_specular);
                case ShaderName.PBS_TriplanarTransparent:
                    return new Uri(resdb_froox_transparent);
                case ShaderName.PBS_TriplanarTransparentSpecular:
                    return new Uri(resdb_froox_transparent_specular);
            }
            return null;
        }
        private static StaticShader createShaderComponent(string hash, Uri url, World world)
        {
            StaticShader sharedComponentOrCreate = world.GetSharedComponentOrCreate(hash, delegate (StaticShader provider)
            {
                provider.URL.Value = url;
            }, 0, true, false);
            sharedComponentOrCreate.Persistent = false;
            return sharedComponentOrCreate;
        }

        public static bool InitializeWithWorld(World world)
        {
            PBS_Triplanar_staticShader = createShaderComponent(choco, new Uri(resdb_choco), world);
            PBS_TriplanarSpecular_staticShader = createShaderComponent(choco_specular, new Uri(resdb_choco_specular), world);
            PBS_TriplanarTransparent_staticShader = createShaderComponent(choco_transparent, new Uri(resdb_choco_transparent), world);
            PBS_TriplanarTransparentSpecular_staticShader = createShaderComponent(choco_transparent_specular, new Uri(resdb_choco_transparent_specular), world);


            //metallic1.InitializeSyncMembers();
            //specular1.InitializeSyncMembers();
            //metallic2.InitializeSyncMembers();
            //specular2.InitializeSyncMembers();

            //World world = PBS_Triplanar_staticShader.World;
            //metallic1.World = world;
            //metallic2.World = world;
            //specular1.World = world;
            //specular2.World = world;

            //metallic1._regular.Target = PBS_Triplanar_staticShader;
            //specular1._regular.Target = PBS_TriplanarSpecular_staticShader;
            //metallic2._transparent.Target = PBS_TriplanarTransparent_staticShader;
            //specular2._transparent.Target = PBS_TriplanarTransparentSpecular_staticShader;

            //PBS_Triplanar_staticShader.InitializeWorker(world);
            //PBS_TriplanarSpecular_staticShader.InitializeWorker(world);
            //PBS_TriplanarTransparent_staticShader.InitializeWorker(world);
            //PBS_TriplanarTransparentSpecular_staticShader.InitializeWorker(world);
            return true;
        }

        public static bool Initialize(ShaderName name)
        {


            switch (name)
            {
                case ShaderName.PBS_Triplanar:
                    if (PBS_Triplanar_staticShader is null)
                        PBS_Triplanar_staticShader = PBS_TriplanarMetallic.GetSharedShader(GetUriFromName(name));
                    PBS_Triplanar_shader = PBS_Triplanar_staticShader.Asset;
                    return true;
                case ShaderName.PBS_TriplanarSpecular:
                    if (PBS_TriplanarSpecular_staticShader is null)
                        PBS_TriplanarSpecular_staticShader = PBS_TriplanarSpecular.GetSharedShader(GetUriFromName(name));
                    PBS_TriplanarSpecular_shader = PBS_TriplanarSpecular_staticShader.Asset;
                    return true;
                case ShaderName.PBS_TriplanarTransparent:
                    if (PBS_TriplanarTransparent_staticShader is null)
                        PBS_TriplanarTransparent_staticShader = PBS_TriplanarMetallic.GetSharedShader(GetUriFromName(name));
                    PBS_TriplanarTransparent_shader = PBS_TriplanarTransparent_staticShader.Asset;
                    return true;
                case ShaderName.PBS_TriplanarTransparentSpecular:
                    if (PBS_TriplanarTransparentSpecular_staticShader is null)
                        PBS_TriplanarTransparentSpecular_staticShader = PBS_TriplanarSpecular.GetSharedShader(GetUriFromName(name));
                    PBS_TriplanarTransparentSpecular_shader = PBS_TriplanarTransparentSpecular_staticShader.Asset;
                    return true;
            }
            return false;
        }



        public static FrooxEngine.Shader? GetShader(ShaderName name)
        {
            var cachedShader = GetShaderFromName(name);
            if (cachedShader is not null)
            {
                return cachedShader;
            }
            Initialize(name);
            cachedShader = GetShaderFromName(name);
            if (cachedShader is not null)
            {
                return cachedShader;
            }
            return null;
            //OOOhhhh this could totally be a recursive function, but it could cause some overflow problems if assets don't populate right away
        }

        public static ShaderName? stringToName(string str)
        {
            if (str.Contains(froox))
                return ShaderName.PBS_Triplanar;
            if (str.Contains(froox_specular))
                return ShaderName.PBS_TriplanarSpecular;
            if (str.Contains(froox_transparent))
                return ShaderName.PBS_TriplanarTransparent;
            if (str.Contains(froox_transparent_specular))
                return ShaderName.PBS_TriplanarTransparentSpecular;
            return null;
        }
    }

    [HarmonyPatchCategory(nameof(WorldInitIntercept))]//commment this out, then uncomment after game is loaded
    [HarmonyPatch(typeof(World), MethodType.Constructor, new Type[] { typeof(WorldManager), typeof(bool), typeof(bool) })]
    internal class WorldInitIntercept : ResoniteMonkey<WorldInitIntercept>
    {
        protected override IEnumerable<IFeaturePatch> GetFeaturePatches() => Enumerable.Empty<IFeaturePatch>();

        private static int IsUserspaceInitialized = 1;
        private static void Postfix(World __instance)
        {
#if DEBUG
            //if (!__instance.IsUserspace())
            //{
            //    //IsUserspaceInitialized = true;
            //}
#endif
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
                Shaders.InitializeWithWorld(__instance);
                Logger.Info(() => "Shader world init done");
                Initialize(ShaderName.PBS_Triplanar);
                Logger.Info(() => "Shader tri");
                Initialize(ShaderName.PBS_TriplanarSpecular);
                Logger.Info(() => "Shader tri spec");
                Initialize(ShaderName.PBS_TriplanarTransparent);
                Logger.Info(() => "Shader tri trans");
                Initialize(ShaderName.PBS_TriplanarTransparentSpecular);
                Logger.Info(() => "Shader tri trans spec");

            }
            else
            {
                Logger.Info(() => "First init, assuming this world is Userspace, and skipping.");
                IsUserspaceInitialized++;
            }

            Logger.Info(() => "Done! There are a total of " + DataShare.frooxWorlds.Count + " world connectors in the list.");
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
                //Logger.Error(() => "There are no valid focused worlds! Fatal error, exiting function.");
                return;
            }
            Vector3 xyz = __instance.transform.localScale;
            Vector3 iscl = new Vector3(1 / xyz.x, 1 / xyz.y, 1 / xyz.z);
            Vector3 playerMotion = __instance.transform.position - DataShare.FrooxCameraPosition[index];
            DataShare.FrooxCameraPosition[index] = __instance.transform.position;
            Vector3 pos = __instance.transform.position;
            __instance._viewPos -= new float3(pos.x * iscl.x, pos.y, pos.z * iscl.z);
            //Do we really need viewScale?
            DataShare.unityWorldRoots[index].transform.position -= playerMotion;

            DataShare.unityWorldRoots[index].transform.localScale = iscl;
            float3 vpos = __instance._viewPos;
            float3 fxyzpos = new float3(vpos.x, iscl.y * vpos.y, vpos.z);
            __instance._viewPos = fxyzpos;
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
            Logger.Info(() => "(PBS_TriplanarMetallic.GetShader)");
            if (__instance.Transparent)
            {
                __result = __instance.EnsureSharedShader(__instance._transparent, new Uri(Shaders.resdb_choco_transparent)).Asset;
                return false;
            }
            __result = __instance.EnsureSharedShader(__instance._regular, new Uri(Shaders.resdb_choco)).Asset;
            return false; //never run original function
        }

        [HarmonyPatch(typeof(PBS_TriplanarMetallic), nameof(PBS_TriplanarMetallic.InitializeSyncMembers))]
        private static bool Prefix(PBS_TriplanarMetallic __instance)
        {
            var w = Userspace.UserspaceWorld;
            w.RunInUpdates(5, () =>
            {
                __instance._regular.IsDrivable = true;
                __instance._regular.DriveFrom(__instance._regular, true);
                __instance._transparent.IsDrivable = true;
                __instance._transparent.DriveFrom(__instance._transparent, true);
            });
            return true;
        }
    }

    [HarmonyPatchCategory(nameof(PBS_Tri_Specular_Overhaul))]
    internal class PBS_Tri_Specular_Overhaul : ResoniteMonkey<PBS_Tri_Specular_Overhaul>
    {
        protected override IEnumerable<IFeaturePatch> GetFeaturePatches() => Enumerable.Empty<IFeaturePatch>();

        [HarmonyPatch(typeof(PBS_TriplanarSpecular), nameof(PBS_TriplanarSpecular.GetShader))]
        private static bool Prefix(PBS_TriplanarSpecular __instance, ref FrooxEngine.Shader __result)
        {
            Logger.Info(() => "(PBS_TriplanarSpecular.GetShader)");
            if (__instance.Transparent)
            {
                __result = __instance.EnsureSharedShader(__instance._transparent, new Uri(Shaders.resdb_choco_transparent_specular)).Asset;
                return false;
            }
            __result = __instance.EnsureSharedShader(__instance._regular, new Uri(Shaders.resdb_choco_specular)).Asset;
            return false; //never run original function
        }

        [HarmonyPatch(typeof(PBS_TriplanarSpecular), nameof(PBS_TriplanarSpecular.InitializeSyncMembers))]
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
            Logger.Info(() => "New Material initialized!");
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