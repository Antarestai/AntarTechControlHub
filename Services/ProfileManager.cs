using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using AntarTechControl.Models;

namespace AntarTechControl.Services
{
    public class ProfileManager
    {
        private static readonly string AppDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AntarTechControl", "Profiles");

        public ProfileManager()
        {
            if (!Directory.Exists(AppDataPath))
            {
                Directory.CreateDirectory(AppDataPath);
            }
        }

        public string GetProfilePath(Guid deviceId)
        {
            return Path.Combine(AppDataPath, $"{deviceId}.json");
        }

        public void SaveAutoProfile(Guid deviceId, IEnumerable<AxisInfo> axes)
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(axes, options);
                File.WriteAllText(GetProfilePath(deviceId), json);
            }
            catch { /* Log error in future */ }
        }

        public List<AxisInfo>? LoadAutoProfile(Guid deviceId)
        {
            string path = GetProfilePath(deviceId);
            if (File.Exists(path))
            {
                try
                {
                    string json = File.ReadAllText(path);
                    return JsonSerializer.Deserialize<List<AxisInfo>>(json);
                }
                catch { }
            }
            return null;
        }

        public void ExportProfile(string filePath, IEnumerable<AxisInfo> axes)
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(axes, options);
            File.WriteAllText(filePath, json);
        }

        public List<AxisInfo>? ImportProfile(string filePath)
        {
            if (File.Exists(filePath))
            {
                try
                {
                    string json = File.ReadAllText(filePath);
                    return JsonSerializer.Deserialize<List<AxisInfo>>(json);
                }
                catch { }
            }
            return null;
        }
    }
}
