
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ColossalFramework;
using UnityEngine;
namespace TVPropPatch
{
    public class Enumerations
    {
        public static IEnumerator FixIlluminationMaps()
        {
            yield return 0;
        }
        public static IEnumerator InitializeAndBindClones()
        {
            HashSet<string> customPropNames = new HashSet<string>();
            int count = PrefabCollection<PropInfo>.LoadedCount();
            for (uint i = 0; i < count; i++)
            {
                PropInfo prop = PrefabCollection<PropInfo>.GetLoaded(i);
                if (!prop.m_isCustomContent) continue;
                customPropNames.Add(prop.name);
            }

            HashSet<PropInfo> DuplicateTreeProps = new HashSet<PropInfo>();
            foreach (var i in CS_TreeProps.Mod.propToTreeCloneMap)
            {
                if (customPropNames.Contains(i.Key.name)) DuplicateTreeProps.Add(i.Key);
            }
            foreach (var i in DuplicateTreeProps)
            {
                if (CS_TreeProps.Mod.propToTreeCloneMap.ContainsKey(i)) CS_TreeProps.Mod.propToTreeCloneMap.Remove(i);
            }

            HashSet<PropInfo> DuplicateVehicleProps = new HashSet<PropInfo>();
            foreach (var i in CS_TreeProps.Mod.propToVehicleCloneMap)
            {
                if (customPropNames.Contains(i.Key.name)) DuplicateVehicleProps.Add(i.Key);
            }
            foreach (var i in DuplicateVehicleProps)
            {
                if (CS_TreeProps.Mod.propToVehicleCloneMap.ContainsKey(i)) CS_TreeProps.Mod.propToVehicleCloneMap.Remove(i);
            }
            yield return null;

            PrefabCollection<PropInfo>.InitializePrefabs("Tree to Prop", CS_TreeProps.Mod.propToTreeCloneMap.Select((KeyValuePair<PropInfo, TreeInfo> k) => k.Key).ToArray(), null);
            yield return null;
            PrefabCollection<PropInfo>.InitializePrefabs("Vehicle to Prop", CS_TreeProps.Mod.propToVehicleCloneMap.Select((KeyValuePair<PropInfo, VehicleInfo> k) => k.Key).ToArray(), null);
            yield return null;
            PrefabCollection<PropInfo>.BindPrefabs();
            Debug.Log((object)"[Tree and Vehicle Props] Bound tree and vehicle prefabs.");
            yield return null;
            Singleton<LoadingManager>.instance.QueueLoadingAction(FixIlluminationMaps());
        }
    }
}
