using System;
using System.Numerics;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using ImGuiScene;

namespace VTScheduleXIV.Windows;

public class ConfigWindow : Window, IDisposable
{
    private TextureWrap logo;
    private Configuration Configuration;

    public ConfigWindow(Plugin plugin, TextureWrap logo) : base(
        "Configuration",
        ImGuiWindowFlags.NoScrollbar |
        ImGuiWindowFlags.NoScrollWithMouse)
    {
        this.SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(375, 330),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };
        this.Configuration = plugin.Configuration;
        this.logo = logo;
    }


    public void Dispose() { }

    public override void Draw()
    {
        var holodexApiKey = this.Configuration.HolodexAPIKey;
        if (ImGui.InputText("Holodex API Key", ref holodexApiKey, 10000))
        {
            this.Configuration.HolodexAPIKey = holodexApiKey;
            this.Configuration.Save();
        }

        var refreshLive = this.Configuration.RefreshLiveVideos;
        if (ImGui.Checkbox("Refresh Live Videos", ref refreshLive))
        {
            this.Configuration.RefreshLiveVideos = refreshLive;
            this.Configuration.Save();
        }
        var refreshUpcoming = this.Configuration.RefreshUpcomingVideos;
        if (ImGui.Checkbox("Refresh Upcoming Videos", ref refreshUpcoming))
        {
            this.Configuration.RefreshUpcomingVideos = refreshUpcoming;
            this.Configuration.Save();
        }
        
        var channels = this.Configuration.Channels;
        if (ImGui.InputText("Channels", ref channels, 10000))
        {
            this.Configuration.Channels = channels;
            this.Configuration.Save();
        }


        var organizations = this.Configuration.Organizations;
        if (ImGui.InputText("Organizations", ref organizations, 10000))
        {
            this.Configuration.Organizations = organizations;
            this.Configuration.Save();
        }

        ImGui.Text("Organizations and Channels are separated by commas and are case sensitive as listed on Holodex");
        ImGui.Image(this.logo.ImGuiHandle, new Vector2(200, 173));
        


    }
}
