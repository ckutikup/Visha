using System;
using System.IO;
using UnityEngine;

namespace Visha.Campaign
{
    [Serializable]
    public class CampaignSave
    {
        public int version = 1;
        public int highestCompleted = -1;
        public int resumeEncounter;

        public bool IsValid(int count) => version == 1 && count > 0 && highestCompleted >= -1 &&
            highestCompleted < count && resumeEncounter >= 0 && resumeEncounter < count &&
            resumeEncounter <= highestCompleted + 1;

        public static CampaignSave Load(string path, int count)
        {
            if (!File.Exists(path)) return null;
            try
            {
                var save = JsonUtility.FromJson<CampaignSave>(File.ReadAllText(path));
                return save != null && save.IsValid(count) ? save : null;
            }
            catch (Exception error) when (error is IOException || error is ArgumentException || error is UnauthorizedAccessException)
            {
                Debug.LogWarning("Could not read campaign checkpoint: " + error.Message);
                return null;
            }
        }

        public bool Write(string path)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                string temporary = path + ".tmp";
                File.WriteAllText(temporary, JsonUtility.ToJson(this));
                if (File.Exists(path)) File.Replace(temporary, path, null);
                else File.Move(temporary, path);
                return true;
            }
            catch (Exception error) when (error is IOException || error is UnauthorizedAccessException)
            {
                Debug.LogWarning("Could not save campaign checkpoint: " + error.Message);
                return false;
            }
        }
    }
}
