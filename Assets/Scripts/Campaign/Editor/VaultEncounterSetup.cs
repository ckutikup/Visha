using UnityEditor;
using UnityEngine;

namespace Visha.Campaign.EditorTools
{
    /// <summary>
    /// Editor-only helper that creates the two-enemy Vault encounter asset with the
    /// agreed starting values (see Docs/VaultEncounter.md). Safe to re-run: an existing
    /// asset is reused and refreshed to the default values rather than duplicated.
    ///
    /// The asset is inert until the two-enemy combat flow consumes VaultEncounter, so
    /// this is provided for reproducibility now and for wiring once that flow lands.
    /// </summary>
    public static class VaultEncounterSetup
    {
        private const string Folder = "Assets/Data/Encounters";
        private const string AssetPath = Folder + "/VaultEncounter_GuardianAndSentinel.asset";

        [MenuItem("Visha/Setup/Create Vault Encounter Asset")]
        public static void CreateVaultEncounterAsset()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Data"))
                AssetDatabase.CreateFolder("Assets", "Data");
            if (!AssetDatabase.IsValidFolder(Folder))
                AssetDatabase.CreateFolder("Assets/Data", "Encounters");

            VaultEncounter asset = AssetDatabase.LoadAssetAtPath<VaultEncounter>(AssetPath);
            bool existed = asset != null;
            if (!existed) asset = ScriptableObject.CreateInstance<VaultEncounter>();

            asset.ApplyDefaultVault();
            EditorUtility.SetDirty(asset);

            if (!existed) AssetDatabase.CreateAsset(asset, AssetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"Vault encounter asset {(existed ? "refreshed" : "created")} at {AssetPath} " +
                      "(Vault Guardian 18 HP + Vault Sentinel 26 HP). Assign sprites during art integration.");
            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
        }
    }
}
