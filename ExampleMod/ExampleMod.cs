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

namespace MonkeyLoader.ExampleMod
{

    public class AssemblyInfo
    {
        //setup instructions (in no particular order):
        //Find and replace all instances of "ExampleMod" with your mod name
        //Find and replace all instances of "ExampleAuthor" with your author
        //Find and replace all instances of "ExampleDescription" with your mod description
        //Rename the Project file (.sln), project folder, the other project file (.csproj), and your source file (.cs
        //Replace ExampleURL with your source repo url
        internal const string VERSION_CONSTANT = "1.0.0"; //Changing the version here updates it in all locations needed
    }

    [HarmonyPatchCategory(nameof(ExampleMod))]
    [HarmonyPatch(typeof(ProtoFluxTool), nameof(ProtoFluxTool.OnAttach))]
    internal class ExampleMod : ResoniteMonkey<ExampleMod>
    {
        // The options for these should be provided by your game's game pack.
        protected override IEnumerable<IFeaturePatch> GetFeaturePatches()
        {
            yield return new FeaturePatch<ProtofluxTool>(PatchCompatibility.HookOnly);
        }

        private static void Postfix()
        {
            Logger.Info(() => "Postfix for ProtoFluxTool.OnAttach()!");
        }
    }
}
