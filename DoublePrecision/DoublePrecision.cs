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

        public const string resdb_choco = "resdb:///e907ed0ca29b3534896947c4ea0004dbfa8baae96a645f3539a9516f3e9d369f.__Choco__";
        public const string resdb_choco_specular = "resdb:///8500b5e85587ab83a88f1dec000bbbe8a3fc760ede5c1df4242a3b6372273d27.unityshader";
        public const string resdb_choco_transparent = "resdb:///f3d267a56478c4a756e5d3f71195fa68bb091c9486a48d136aa23ca27d042a35.unityshader";
        public const string resdb_choco_transparent_specular = "resdb:///121e7a5a66c70a278e99adeeb2e7ee7090c00064f3d6312cdd5026892f56023b.unityshader";

        public const string resdb_froox = "resdb:///d9d43057b97ff2e71b9947af38c754cde992bfa8ea75ab34d9e43859e0a0f7d3.unityshader";
        public const string resdb_froox_specular = "resdb:///33dcd39d588e92840eb58845d3a0404e75c038e277a92b634721dcecc16dfd9a.unityshader";
        public const string resdb_froox_transparent = "resdb:///abfef7119e75779d7ad31222211acc4dbcced41bacda4fd32bf04fac633c8b1b.unityshader";
        public const string resdb_froox_transparent_specular = "resdb:///205b3cc9c239927986895a41e6cc7323853da09b87d23c5b466a2940b4e4de92.unityshader";
    }

    [HarmonyPatchCategory(nameof(WorldInitIntercept))]
    [HarmonyPatch(typeof(World), MethodType.Constructor, new Type[] { typeof(WorldManager), typeof(bool), typeof(bool) })]
    internal class WorldInitIntercept : ResoniteMonkey<WorldInitIntercept>
    {
        protected override IEnumerable<IFeaturePatch> GetFeaturePatches() => Enumerable.Empty<IFeaturePatch>();

        private static bool IsUserspaceInitialized = false;
        private static void Postfix(World __instance)
        {
#if DEBUG
            if (!__instance.IsUserspace())
            {
                IsUserspaceInitialized = true;
            }
#endif
            Logger.Info(() => "Intercepted World Init, attempting to cache World reference.");
            if (IsUserspaceInitialized)
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
            Vector3 playerMotion = __instance.transform.position - DataShare.FrooxCameraPosition[index];
            DataShare.FrooxCameraPosition[index] = __instance.transform.position;
            Vector3 pos = __instance.transform.position;
            __instance._viewPos -= new float3(pos.x, pos.y, pos.z);
            //Do we really need viewScale?
            DataShare.unityWorldRoots[index].transform.position -= playerMotion;
            __instance.transform.position = Vector3.zero;
            Vector3 rootPos = DataShare.unityWorldRoots[index].transform.position;
            for (int i = 0; i < DataShare.FrooxMaterials.Count; i++)
            {
                if (DataShare.FrooxMaterials[i] is not null)
                {
                    if (DataShare.FrooxMaterials[i].Asset is null)
                    {
                        Logger.Info(() => "FrooxMaterials.Asset is null, index of " + i);
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

    [HarmonyPatchCategory(nameof(TriplanarInitIntercept))]
    [HarmonyPatch(typeof(PBS_TriplanarMaterial), nameof(PBS_TriplanarMaterial.InitializeSyncMembers))]
    internal class TriplanarInitIntercept : ResoniteMonkey<TriplanarInitIntercept>
    {
        protected override IEnumerable<IFeaturePatch> GetFeaturePatches() => Enumerable.Empty<IFeaturePatch>();
        private static void Postfix(PBS_TriplanarMaterial __instance)
        {
            Logger.Info(() => "New Material initialized!");
            DataShare.FrooxMaterials.Add(__instance);
            //    Uri url = __instance.GetShader().AssetURL;
            //    Uri newUri = new Uri("https://example.com");
            //    bool updated = false;
            //    //Logger.Info(() => url);
            //    switch (url.AbsoluteUri)
            //    {
            //        case Shaders.resdb_froox:
            //            updated = true;
            //            newUri = new Uri(Shaders.resdb_choco);
            //            break;
            //        case Shaders.resdb_froox_specular:
            //            updated = true;
            //            newUri = new Uri(Shaders.resdb_choco_specular);
            //            break;
            //        case Shaders.resdb_froox_transparent:
            //            updated = true;
            //            newUri = new Uri(Shaders.resdb_choco_transparent);
            //            break;
            //        case Shaders.resdb_froox_transparent_specular:
            //            updated = true;
            //            newUri = new Uri(Shaders.resdb_choco_transparent_specular);
            //            break;
            //    }
            //    if (updated)
            //    {
            //        FrooxEngine.Shader newShader = new FrooxEngine.Shader();
            //        newShader.SetURL(newUri);
            //        //newShader.InitializeConnector();
            //        __instance._asset.SetURL(newUri);// .SetURL(newUri);
            //    }
            //}
        }
        //protected StaticShader GetSharedShader(Uri url)

        [HarmonyPatchCategory(nameof(MatProvInject))]
        [HarmonyPatch(typeof(MaterialProvider), nameof(MaterialProvider.GetSharedShader))]
        internal class MatProvInject : ResoniteMonkey<MatProvInject>
        {
            protected override IEnumerable<IFeaturePatch> GetFeaturePatches() => Enumerable.Empty<IFeaturePatch>();
            private static void Postfix(MaterialProvider __instance, Uri url, ref StaticShader __result)
            {
                //Logger.Info(() => "Slot");
                bool updated = false;
                switch (url.AbsoluteUri)
                {
                    case Shaders.resdb_froox:
                        updated = true;
                        url = new Uri(Shaders.resdb_choco);
                        break;
                    case Shaders.resdb_froox_specular:
                        updated = true;
                        url = new Uri(Shaders.resdb_choco_specular);
                        break;
                    case Shaders.resdb_froox_transparent:
                        updated = true;
                        url = new Uri(Shaders.resdb_choco_transparent);
                        break;
                    case Shaders.resdb_froox_transparent_specular:
                        updated = true;
                        url = new Uri(Shaders.resdb_choco_transparent_specular);
                        break;
                }


                if (url == null)
                {
                    __result = null;
                }
                if (__instance.IsLocalElement)
                {
                    __result = __instance.World.GetLocalRegisteredComponent<StaticShader>(url.OriginalString, delegate (StaticShader provider)
                    {
                        provider.URL.Value = url;
                    }, true, false);
                }
                StaticShader sharedComponentOrCreate = __instance.World.GetSharedComponentOrCreate(__instance.Cloud.Assets.DBSignature(url, false), delegate (StaticShader provider)
                {
                    provider.URL.Value = url;
                }, 0, true, false, new Func<Slot>(__instance.GetShaderRoot));
                sharedComponentOrCreate.Persistent = false;
                __result = sharedComponentOrCreate;
            }
        }

        //[HarmonyPatchCategory(nameof(ShaderInjector))]
        //[HarmonyPatch(typeof(MaterialConnector), nameof(MaterialConnector.BeginUpload))]
        //internal class ShaderInjector : ResoniteMonkey<ShaderInjector>
        //{
        //    protected override IEnumerable<IFeaturePatch> GetFeaturePatches() => Enumerable.Empty<IFeaturePatch>();
        //    private static void Prefix(MaterialConnector __instance)
        //    {

        //        Uri url = __instance.targetShader.AssetURL;
        //        Uri newUri = new Uri("https://example.com");
        //        bool updated = false;
        //        //Logger.Info(() => url);
        //        switch (url)
        //        {
        //            case new Uri(Shaders.resdb_froox):
        //                updated = true;
        //                newUri = new Uri(Shaders.resdb_choco);
        //                break;
        //            case Shaders.resdb_froox_specular:
        //                updated = true;
        //                newUri = new Uri(Shaders.resdb_choco_specular);
        //                break;
        //            case Shaders.resdb_froox_transparent:
        //                updated = true;
        //                newUri = new Uri(Shaders.resdb_choco_transparent);
        //                break;
        //            case Shaders.resdb_froox_transparent_specular:
        //                updated = true;
        //                newUri = new Uri(Shaders.resdb_choco_transparent_specular);
        //                break;
        //        }
        //        if (updated)
        //        {
        //            FrooxEngine.Shader newShader = new FrooxEngine.Shader();
        //            newShader.SetURL(newUri);
        //            //newShader.InitializeConnector();
        //            __instance.targetShader = newShader;
        //        }

        //    }
        //}

        //[HarmonyPatchCategory(nameof(AssetInjection))]
        //[HarmonyPatch(typeof(AssetInterface), nameof(AssetInterface.GatherAsset))]
        //internal class AssetInjection : ResoniteMonkey<AssetInjection>
        //{
        //    protected override IEnumerable<IFeaturePatch> GetFeaturePatches() => Enumerable.Empty<IFeaturePatch>();
        //    private static void Prefix(AssetInterface __instance, string signature)
        //    {
        //        switch (signature)
        //        {
        //            case Shaders.froox:
        //                signature = Shaders.choco;
        //                break;
        //            case Shaders.froox_specular:
        //                signature = Shaders.choco_specular;
        //                break;
        //            case Shaders.froox_transparent:
        //                signature = Shaders.choco_transparent;
        //                break;
        //            case Shaders.froox_transparent_specular:
        //                signature = Shaders.choco_transparent_specular;
        //                break;
        //        }

        //        Logger.Info(() => "Hash injected from AssetInterface.GatherAsset()");
        //    }
        //}
        ////StaticAssetProvider<Shader, ShaderMetadata, ShaderVariantDescriptor>
        //[HarmonyPatchCategory(nameof(URLInjection))]
        //[HarmonyPatch(typeof(Asset), "AssetURL", MethodType.Getter)]
        //internal class URLInjection : ResoniteMonkey<URLInjection>
        //{
        //    protected override IEnumerable<IFeaturePatch> GetFeaturePatches() => Enumerable.Empty<IFeaturePatch>();
        //    private static void Prefix(ref Uri __result)
        //    {
        //        Logger.Info(() => "Asset URL injection (getter)");
        //        string url = "";
        //        bool initialized = false;
        //        string originalURL = __result.AbsoluteUri;
        //        Logger.Info(() => originalURL);
        //        switch (__result.AbsoluteUri)
        //        {
        //            case Shaders.resdb_froox:
        //                url = Shaders.resdb_choco;
        //                initialized = true;
        //                break;
        //            case Shaders.resdb_froox_specular:
        //                url = Shaders.resdb_choco_specular;
        //                initialized = true;
        //                break;
        //            case Shaders.resdb_froox_transparent:
        //                url = Shaders.resdb_choco_transparent;
        //                initialized = true;
        //                break;
        //            case Shaders.resdb_froox_transparent_specular:
        //                url = Shaders.choco_transparent_specular;
        //                initialized = true;
        //                break;
        //        }
        //        if (initialized)
        //        {
        //            __result = new Uri(url);
        //            Logger.Info(() => "ResDB injected from StaticAssetProvider.URL (getter)");
        //        }

        //    }
        //}


        //[HarmonyPatchCategory(nameof(AssetRefInjection))]
        //[HarmonyPatch(typeof(AssetRef<FrooxEngine.Shader>), "Asset", MethodType.Getter)]
        //internal class AssetRefInjection : ResoniteMonkey<AssetRefInjection>
        //{
        //    protected override IEnumerable<IFeaturePatch> GetFeaturePatches() => Enumerable.Empty<IFeaturePatch>();
        //    public static void Postfix(AssetRef<FrooxEngine.Shader> __instance, ref FrooxEngine.Shader __result)
        //    {
        //        Uri url = __result.AssetURL;
        //        Logger.Info(() => url);
        //    }
        //}

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