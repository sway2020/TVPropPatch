using CitiesHarmony.API;
using HarmonyLib;
using ICities;
using System;
using System.Reflection;
using UnityEngine;
using ColossalFramework.UI;
using System.Collections.Generic;
using CS_TreeProps;
using System.IO;
using ColossalFramework.IO;
using ColossalFramework;

namespace TVPropPatch
{
    public class Mod : IUserMod
    {
        public string Name => "TV Props Patch 1.6.1";
        public string Description => "Patch the Tree & Vehicle Props mod. Add support for Find It 2";

        public static Dictionary<string, bool> skippedVehicleDictionary = new Dictionary<string, bool>();
        public static Dictionary<string, bool> skippedTreeDictionary = new Dictionary<string, bool>();

        public void OnEnabled()
        {
            HarmonyHelper.DoOnHarmonyReady(() => Patcher.PatchAll());
            XMLUtils.LoadSettings();

            foreach (SkippedEntry entry in Settings.skippedVehicleEntries)
            {
                if (skippedVehicleDictionary.ContainsKey(entry.name)) continue;
                skippedVehicleDictionary.Add(entry.name, entry.skipped);
            }

            foreach (SkippedEntry entry in Settings.skippedTreeEntries)
            {
                if (skippedTreeDictionary.ContainsKey(entry.name)) continue;
                skippedTreeDictionary.Add(entry.name, entry.skipped);
            }
        }

        public void OnDisabled()
        {
            if (HarmonyHelper.IsHarmonyInstalled) Patcher.UnpatchAll();
        }

        public void OnSettingsUI(UIHelperBase helper)
        {
            try
            {
                UIHelper group = helper.AddGroup(Name) as UIHelper;
                UIPanel panel = group.self as UIPanel;

                UICheckBox skipVanillaVehicles = (UICheckBox)group.AddCheckbox("Skip prop conversion for all vanilla vehicles", Settings.skipVanillaVehicles, (b) =>
                {
                    Settings.skipVanillaVehicles = b;
                    XMLUtils.SaveSettings();
                });
                skipVanillaVehicles.tooltip = "Generated vanilla vehicle props will disappear next time when a save file is loaded";
                group.AddSpace(10);

                UICheckBox skipVanillaTrees = (UICheckBox)group.AddCheckbox("Skip prop conversion for all vanilla trees", Settings.skipVanillaTrees, (b) =>
                {
                    Settings.skipVanillaTrees = b;
                    XMLUtils.SaveSettings();
                });
                skipVanillaVehicles.tooltip = "Generated vanilla tree props will disappear next time when a save file is loaded";
                group.AddSpace(10);

                // show path to TVPropPatchConfig.xml
                string path = Path.Combine(DataLocation.executableDirectory, "TVPropPatchConfig.xml");
                UITextField customTagsFilePath = (UITextField)group.AddTextfield("Config File", path, _ => { }, _ => { });
                customTagsFilePath.width = panel.width - 30;
                // from aubergine10's AutoRepair
                if (Application.platform == RuntimePlatform.WindowsPlayer)
                {
                    group.AddButton("Show in File Explorer", () => System.Diagnostics.Process.Start("explorer.exe", "/select," + path));
                }


            }
            catch (Exception e)
            {
                Debug.Log("OnSettingsUI failed");
                Debug.LogException(e);
            }
        }

        public static HashSet<PropInfo> generatedVehicleProp = new HashSet<PropInfo>();
        public static HashSet<PropInfo> generatedTreeProp = new HashSet<PropInfo>();
        public static Dictionary<PropInfo, VehicleInfo> propVehicleInfoTable = new Dictionary<PropInfo, VehicleInfo>();
    }

    public static class Patcher
    {
        private const string HarmonyId = "sway.TVPropPatch";

        private static bool patched = false;

        public static void PatchAll()
        {
            if (patched) return;

            UnityEngine.Debug.Log("TV Props Patch: Patching...");

            patched = true;

            // Harmony.DEBUG = true;
            var harmony = new Harmony("sway.TVPropPatch");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }

        public static void UnpatchAll()
        {
            if (!patched) return;

            var harmony = new Harmony(HarmonyId);
            harmony.UnpatchAll(HarmonyId);

            patched = false;

            UnityEngine.Debug.Log("TV Props Patch: Reverted...");
        }

    }

    [HarmonyPatch(typeof(PropInstance), "RenderInstance", new Type[]
    {
        typeof(RenderManager.CameraInfo), typeof(PropInfo), typeof(InstanceID), typeof(Vector3), typeof(float), typeof(float), typeof(Color), typeof(Vector4), typeof(bool)
    })]
    public static class RenderInstancePatch
    {
        public static void Prefix(ref bool active, ref PropInfo info)
        {
            if (Mod.generatedVehicleProp.Contains(info))
            {
                active = false;
            }
        }
    }

    [HarmonyPatch]
    public static class TreeToPropPatch
    {
        public static MethodBase TargetMethod()
        {
            return typeof(CS_TreeProps.AssetExtension).GetMethod(nameof(CS_TreeProps.AssetExtension.TreeToProp));
        }

        public static bool Prefix(ref TreeInfo tree)
        {
            if (tree == null) return false;
            if (Settings.skipVanillaTrees && !tree.m_isCustomContent) return false;
            if (Mod.skippedTreeDictionary.ContainsKey(tree.name) && Mod.skippedTreeDictionary[tree.name]) return false;

            PropInfo propInfo = AssetExtension.CloneProp();
            propInfo.name = tree.name.Replace("_Data", "") + " Prop_Data";
            propInfo.m_mesh = tree.m_mesh;
            propInfo.m_material = tree.m_material;
            propInfo.m_Thumbnail = tree.m_Thumbnail;
            propInfo.m_InfoTooltipThumbnail = tree.m_InfoTooltipThumbnail;
            propInfo.m_InfoTooltipAtlas = tree.m_InfoTooltipAtlas;
            propInfo.m_Atlas = tree.m_Atlas;
            propInfo.m_generatedInfo.m_center = tree.m_generatedInfo.m_center;
            propInfo.m_generatedInfo.m_uvmapArea = tree.m_generatedInfo.m_uvmapArea;
            propInfo.m_generatedInfo.m_size = tree.m_generatedInfo.m_size;
            propInfo.m_generatedInfo.m_triangleArea = tree.m_generatedInfo.m_triangleArea;
            propInfo.m_color0 = tree.m_defaultColor;
            propInfo.m_color1 = tree.m_defaultColor;
            propInfo.m_color2 = tree.m_defaultColor;
            propInfo.m_color3 = tree.m_defaultColor;
            CS_TreeProps.Mod.propToTreeCloneMap.Add(propInfo, tree);
            Mod.generatedTreeProp.Add(propInfo);

            if (!Mod.skippedTreeDictionary.ContainsKey(tree.name))
            {
                Settings.skippedTreeEntries.Add(new SkippedEntry(tree.name));
            }

            return false;
        }
    }

    [HarmonyPatch]
    public static class VehicleToPropPatch
    {
        public static MethodBase TargetMethod()
        {
            return typeof(CS_TreeProps.AssetExtension).GetMethod(nameof(CS_TreeProps.AssetExtension.VehicleToProp));
        }

        public static bool Prefix(ref VehicleInfo vehicle)
        {
            if (vehicle == null || vehicle.name == "Vortex") return false;
            if (Settings.skipVanillaVehicles && !vehicle.m_isCustomContent) return false;
            if (Mod.skippedVehicleDictionary.ContainsKey(vehicle.name) && Mod.skippedVehicleDictionary[vehicle.name]) return false;

            PropInfo propInfo = AssetExtension.CloneProp();
            propInfo.name = vehicle.name.Replace("_Data", "") + " Prop_Data";
            propInfo.m_mesh = vehicle.m_mesh;
            propInfo.m_material = UnityEngine.Object.Instantiate<Material>(vehicle.m_material);
            Shader shader = Shader.Find("Custom/Props/Prop/Default");
            bool flag2 = propInfo.m_material != null;
            if (flag2)
            {
                propInfo.m_material.shader = shader;
            }
            propInfo.m_Thumbnail = vehicle.m_Thumbnail;
            propInfo.m_InfoTooltipThumbnail = vehicle.m_InfoTooltipThumbnail;
            propInfo.m_InfoTooltipAtlas = vehicle.m_InfoTooltipAtlas;
            propInfo.m_Atlas = vehicle.m_Atlas;
            propInfo.m_color0 = vehicle.m_color0;
            propInfo.m_color1 = vehicle.m_color1;
            propInfo.m_color2 = vehicle.m_color2;
            propInfo.m_color3 = vehicle.m_color3;
            CS_TreeProps.Mod.propToVehicleCloneMap.Add(propInfo, vehicle);
            Mod.propVehicleInfoTable.Add(propInfo, vehicle);

            if (!Mod.skippedVehicleDictionary.ContainsKey(vehicle.name))
            {
                Settings.skippedVehicleEntries.Add(new SkippedEntry(vehicle.name));
            }

            return false;
        }
    }


    [HarmonyPatch]
    public static class LoadingExtensionPatch
    {
        public static MethodBase TargetMethod()
        {
            return typeof(CS_TreeProps.LoadingExtension).GetMethod(nameof(CS_TreeProps.LoadingExtension.OnLevelLoaded));
        }

        public static bool Prefix()
        {
            foreach (KeyValuePair<PropInfo, VehicleInfo> keyValuePair in CS_TreeProps.Mod.propToVehicleCloneMap)
            {
                PropInfo key = keyValuePair.Key;
                VehicleInfo value = keyValuePair.Value;
                if (value.m_lodMesh != null)
                {
                    key.m_lodMesh = UnityEngine.Object.Instantiate<Mesh>(value.m_lodMesh);
                }
                if (value.m_lodMaterial != null)
                {
                    key.m_lodMaterial = UnityEngine.Object.Instantiate<Material>(value.m_lodMaterial);
                    key.m_lodMaterial.shader = Shader.Find("Custom/Props/Prop/Default");
                    key.m_lodMaterialCombined = UnityEngine.Object.Instantiate<Material>(value.m_lodMaterialCombined);
                    key.m_lodMaterialCombined.shader = Shader.Find("Custom/Props/Prop/Default");
                    key.m_lodColors = value.m_lodColors;
                }
                if (value.m_lodMeshCombined1 != null)
                {
                    key.m_lodMeshCombined1 = UnityEngine.Object.Instantiate<Mesh>(value.m_lodMeshCombined1);
                }
                if (value.m_lodMeshCombined4 != null)
                {
                    key.m_lodMeshCombined4 = UnityEngine.Object.Instantiate<Mesh>(value.m_lodMeshCombined4);
                }
                if (value.m_lodMeshCombined8 != null)
                {
                    key.m_lodMeshCombined8 = UnityEngine.Object.Instantiate<Mesh>(value.m_lodMeshCombined8);
                }
                if (value.m_lodMeshCombined16 != null)
                {
                    key.m_lodMeshCombined16 = UnityEngine.Object.Instantiate<Mesh>(value.m_lodMeshCombined16);
                }
                key.m_lodRenderDistance = value.m_lodRenderDistance;
                key.m_maxRenderDistance = value.m_maxRenderDistance;
                key.m_isCustomContent = value.m_isCustomContent;
                key.m_dlcRequired = value.m_dlcRequired;
                key.m_generatedInfo = UnityEngine.Object.Instantiate<PropInfoGen>(key.m_generatedInfo);
                key.m_generatedInfo.name = key.name;
                key.m_generatedInfo.m_propInfo = key;
                key.m_generatedInfo.m_uvmapArea = value.m_generatedInfo.m_uvmapArea;
                key.m_generatedInfo.m_triangleArea = value.m_generatedInfo.m_triangleArea;
                if (key.m_mesh != null)
                {
                    key.m_generatedInfo.m_size = Vector3.one * (Math.Max(key.m_mesh.bounds.extents.x, Math.Max(key.m_mesh.bounds.extents.y, key.m_mesh.bounds.extents.z)) * 2f - 1f);
                }
                if (key.m_material != null)
                {
                    key.m_material.SetColor("_ColorV0", key.m_color0);
                    key.m_material.SetColor("_ColorV1", key.m_color1);
                    key.m_material.SetColor("_ColorV2", key.m_color2);
                    key.m_material.SetColor("_ColorV3", key.m_color3);
                }
                if (key.m_lodMaterial != null)
                {
                    key.m_lodMaterial.SetColor("_ColorV0", key.m_color0);
                    key.m_lodMaterial.SetColor("_ColorV1", key.m_color1);
                    key.m_lodMaterial.SetColor("_ColorV2", key.m_color2);
                    key.m_lodMaterial.SetColor("_ColorV3", key.m_color3);
                }

                Mod.generatedVehicleProp.Add(key);
            }
            foreach (KeyValuePair<PropInfo, TreeInfo> keyValuePair2 in CS_TreeProps.Mod.propToTreeCloneMap)
            {
                PropInfo key2 = keyValuePair2.Key;
                TreeInfo value2 = keyValuePair2.Value;
                key2.m_lodMesh = value2.m_mesh;
                key2.m_lodMaterial = value2.m_material;
                key2.m_color0 = value2.m_defaultColor;
                key2.m_color1 = value2.m_defaultColor;
                key2.m_color2 = value2.m_defaultColor;
                key2.m_color3 = value2.m_defaultColor;
                key2.m_lodObject = key2.gameObject;
                key2.m_generatedInfo = UnityEngine.Object.Instantiate<PropInfoGen>(key2.m_generatedInfo);
                key2.m_generatedInfo.name = key2.name;
                key2.m_isCustomContent = value2.m_isCustomContent;
                key2.m_dlcRequired = value2.m_dlcRequired;
                key2.m_generatedInfo.m_propInfo = key2;
                if (key2.m_mesh != null)
                {
                    if (key2.m_isCustomContent)
                    {
                        key2.m_mesh = UnityEngine.Object.Instantiate<Mesh>(value2.m_mesh);
                    }
                    key2.m_generatedInfo.m_size = Vector3.one * (Math.Max(key2.m_mesh.bounds.extents.x, Math.Max(key2.m_mesh.bounds.extents.y, key2.m_mesh.bounds.extents.z)) * 2f - 1f);
                }
                if (key2.m_material != null)
                {
                    key2.m_material.SetColor("_ColorV0", key2.m_color0);
                    key2.m_material.SetColor("_ColorV1", key2.m_color1);
                    key2.m_material.SetColor("_ColorV2", key2.m_color2);
                    key2.m_material.SetColor("_ColorV3", key2.m_color3);
                }
                if (CS_TreeProps.Mod.configuration.TreesShouldNotSway && key2.m_isCustomContent)
                {
                    try
                    {
                        if (key2.m_material.shader.name == "Custom/Trees/Default")
                        {
                            // Debug.Log("[Tree and Vehicle Props] Cleared vertex colors for " + key2.name);
                            Color[] array2 = new Color[key2.m_mesh.vertices.Length];
                            for (int i = 0; i < array2.Length; i++)
                            {
                                array2[i] = new Color(0f, 0f, 0f, 0f);
                            }
                            key2.m_mesh.colors = array2;
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.Log($"{ex.Message}");
                    }
                }
            }

            XMLUtils.SaveSettings();
            return false;
        }
    }


    [HarmonyPatch]
    public static class LoadingHookPatch
    {
        public static MethodBase TargetMethod()
        {
            return typeof(CS_TreeProps.LoadingHook).GetMethod(nameof(CS_TreeProps.LoadingHook.Prefix));
        }
        public static bool Prefix()
        {
            if (!CS_TreeProps.Mod.PrefabsInitialized)
            {
                CS_TreeProps.Mod.PrefabsInitialized = true;
                Singleton<LoadingManager>.instance.QueueLoadingAction(CS_TreeProps.Enumerations.CreateClones());
                Singleton<LoadingManager>.instance.QueueLoadingAction(Enumerations.InitializeAndBindClones());
            }
            return false;
        }
    }
}
