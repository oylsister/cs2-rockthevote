using CounterStrikeSharp.API;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace cs2_rockthevote.Core
{
    public class MapCooldown : IPluginDependency<Plugin, Config>
    {
        public Dictionary<string, int> mapsOnCoolDown = new();
        private ushort InCoolDown = 0;
        private string cooldownFilePath = string.Empty;

        public event EventHandler<Map[]>? EventCooldownRefreshed;

        public MapCooldown(MapLister mapLister)
        {
            //this is called on map start
            mapLister.EventMapsLoaded += (e, maps) =>
            {
                var map = Server.MapName;
                if(map is not null)
                {
                    if (InCoolDown == 0)
                    {
                        mapsOnCoolDown.Clear();
                        return;
                    }

                    List<string> mapsToRemove = new();

                    // decrement cooldowns
                    foreach (var mapdata in mapsOnCoolDown)
                    {
                        if (mapdata.Value > 0)
                            mapsOnCoolDown[mapdata.Key] -= 1;

                        // if cooldown is 0, add to remove list
                        if (mapsOnCoolDown[mapdata.Key] <= 0)
                            mapsToRemove.Add(mapdata.Key);
                    }

                    // remove maps that are no longer in the cooldown list
                    if(mapsToRemove.Count > 0)
                    {
                        foreach (var mapToRemove in mapsToRemove)
                            mapsOnCoolDown.Remove(mapToRemove);
                    }

                    mapsOnCoolDown.Add(map.Trim().ToLower(), InCoolDown);
                    EventCooldownRefreshed?.Invoke(this, maps);
                    SaveCooldownData();
                }
            };
        }

        public void OnLoad(Plugin plugin)
        {
            // create cooldown data file.
            var moduleDirectory = Path.Combine(plugin.ModuleDirectory, "data");
            Directory.CreateDirectory(moduleDirectory);
            cooldownFilePath = Path.Combine(moduleDirectory, "cooldown.json");

            if (!File.Exists(cooldownFilePath))
            {
                File.WriteAllText(cooldownFilePath, "{}");
            }
        }

        public void LoadCooldownData()
        {
            var jsonData = File.ReadAllText(cooldownFilePath);

            if (string.IsNullOrEmpty(jsonData))
                return;

            mapsOnCoolDown = JsonConvert.DeserializeObject<Dictionary<string, int>>(jsonData) ?? [];
        }

        public void SaveCooldownData()
        {
            var jsonData = JsonConvert.SerializeObject(mapsOnCoolDown, Formatting.Indented);
            File.WriteAllText(cooldownFilePath, jsonData);
        }

        public void OnConfigParsed(Config config)
        {
            InCoolDown = config.MapsInCoolDown;
        }

        public bool IsMapInCooldown(string map)
        {
            return mapsOnCoolDown[map] > -1;
        }
    }
}
