using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Visha.Campaign;

namespace Visha.Combat.Flow.EditorTools
{
    [InitializeOnLoad]
    public static class Report3Review
    {
        private const string ReviewKey = "Visha.Report3.VaultReview";
        static Report3Review() { EditorApplication.playModeStateChanged += OnPlay; }
        [MenuItem("Visha/Report 3/Play Vault Review")]
        public static void PlayVault()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene("Assets/Scenes/Campaign_Demo.unity");
            SessionState.SetBool(ReviewKey,true);
            EditorApplication.isPlaying=true;
        }
        private static void OnPlay(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(ReviewKey,false)) return;
            SessionState.SetBool(ReviewKey,false);
            EditorApplication.delayCall+=() =>
            {
                var campaign=Object.FindAnyObjectByType<CampaignController>();
                if (campaign == null || campaign.EncounterCount<3) return;
                campaign.StartCampaign();
                // Exercise campaign progression without changing the saved scene or encounter data.
                var enemy=Object.FindAnyObjectByType<Visha.Enemies.EnemyPatternRunner>().GetComponent<Health>();
                enemy.TakeDamage(enemy.MaxHealth);
                campaign.NextEncounter();
                enemy.TakeDamage(enemy.MaxHealth);
                campaign.GetComponent<TempleMapView>().Open();
                campaign.GetComponent<TempleMapView>().SelectNode(2);
                campaign.GetComponent<TempleMapView>().EnterSelected();
            };
        }
    }
}
