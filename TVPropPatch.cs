using CitiesHarmony.API;
using HarmonyLib;
using ICities;
using System;
using System.Reflection;
using UnityEngine;
using ColossalFramework;
using System.Collections.Generic;
using CS_TreeProps;

namespace TVPropPatch
{
    public class Mod : IUserMod
    {
        public string Name => "TV Props Patch 1.3";
        public string Description => "Patch the Tree & Vehicle Props mod. Add support for Find It 2";

        public void OnEnabled()
        {
            HarmonyHelper.DoOnHarmonyReady(() => Patcher.PatchAll());
        }

        public void OnDisabled()
        {
            if (HarmonyHelper.IsHarmonyInstalled) Patcher.UnpatchAll();
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
                Mod.propVehicleInfoTable.Add(key, value);
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
                    key2.m_generatedInfo.m_size = Vector3.one * (Math.Max(key2.m_mesh.bounds.extents.x, Math.Max(key2.m_mesh.bounds.extents.y, key2.m_mesh.bounds.extents.z)) * 2f - 1f);
                }
                if (key2.m_material != null)
                {
                    key2.m_material.SetColor("_ColorV0", key2.m_color0);
                    key2.m_material.SetColor("_ColorV1", key2.m_color1);
                    key2.m_material.SetColor("_ColorV2", key2.m_color2);
                    key2.m_material.SetColor("_ColorV3", key2.m_color3);
                }
                if (CS_TreeProps.Mod.configuration.TreesShouldNotSway)
                {
                    try
                    {
                        if (key2.m_material.shader.name == "Custom/Trees/Default")
                        {
                            Debug.Log("[Tree and Vehicle Props] Cleared vertex colors for " + key2.name);
                            Color[] array2 = new Color[key2.m_mesh.vertices.Length];
                            for (int i = 0; i < array2.Length; i++)
                            {
                                array2[i] = new Color(0f, 0f, 0f, 0f);
                            }
                            key2.m_mesh.colors = array2;
                        }
                    }
                    catch (Exception)
                    {
                        Debug.Log("[Tree and Vehicle Props] Vanilla tree prop skipped.");
                    }
                }
                Mod.generatedTreeProp.Add(key2);
            }
            
            return false;
        }
    }

}
