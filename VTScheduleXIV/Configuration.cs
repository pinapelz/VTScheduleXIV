using Dalamud.Configuration;
using Dalamud.Plugin;
using System;

namespace VTScheduleXIV
{
    [Serializable]
    public class Configuration : IPluginConfiguration
    {
        public int Version { get; set; } = 0;
        public bool RefreshUpcomingVideos { get; set; } = true;
        public bool RefreshLiveVideos { get; set; } = true;
        public string Channels { get; set; } = "";
        public string Organizations { get; set; } = "";
        public string HolodexAPIKey { get; set; } = "";

        // the below exist just to make saving less cumbersome
        [NonSerialized]
        private DalamudPluginInterface? PluginInterface;

        public void Initialize(DalamudPluginInterface pluginInterface)
        {
            this.PluginInterface = pluginInterface;
        }

        public void Save()
        {
            this.PluginInterface!.SavePluginConfig(this);
        }
    }
}
