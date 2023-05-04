using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using Dalamud.Game.Gui;
using Dalamud.Game.Text;
using Dalamud.Interface.Windowing;
using Holodex.NET;
using Holodex.NET.DataTypes;
using Holodex.NET.Enums;
using ImGuiNET;
using System.Threading.Tasks;
using ImVec2 = System.Numerics.Vector2;
using System.Globalization;

namespace VTScheduleXIV.Windows;

public class MainWindow : Window, IDisposable
{
    private Plugin Plugin;
    private DateTime lastRefresh = DateTime.MinValue;
    private ChatGui chat;
    private List<Video> allVideos;
    private HolodexClient client;


    public MainWindow(Plugin plugin, ChatGui chat) : base(
        "VTScheduleXIV", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
    {
        this.SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(160, 120),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };
        client = new HolodexClient(plugin.Configuration.HolodexAPIKey);
        this.Plugin = plugin;
        this.chat = chat;
        allVideos = new List<Video>();


    }

    public void Dispose()
    {
    }

    private bool isPlaceholderStream(string title)
    {
        if (title.ToLower().Contains("free chat") || title.ToLower().Contains("schedule"))
        {
            return true;
        }
        return false;
    }

    public IReadOnlyCollection<Video> getVideosByChannelID(string channels)
    {
        if (channels == null || channels == "")
        {
            return new List<Video>();
        }
        IReadOnlyCollection<Video> videos = client.GetLiveVideosByChannelId(channels.Split(',')).Result;
        List<Video> result = new List<Video>();
        foreach (Video video in videos)
        {
            if ((Plugin.Configuration.RefreshLiveVideos && video.Status == VideoStatus.Live) 
                || (Plugin.Configuration.RefreshLiveVideos && 
                video.Status == VideoStatus.Upcoming && !isPlaceholderStream(video.Title)))
            {
                result.Add(video);
            }
        }
        return result;
    }

    public IReadOnlyCollection<Video> getVideosByOrganization(string org)
    {
        if(org == null || org == "")
        {
            return new List<Video>();
        }
        List<Video> result = new List<Video>(); 
        string[] organizations = org.Split(",");
        foreach (string o in organizations)
            result.AddRange(client.GetLiveVideos(organization: o).Result);
        List<Video> filteredResult = new List<Video>();
        foreach (Video video in result)
        {
            if ((Plugin.Configuration.RefreshLiveVideos && video.Status == VideoStatus.Live)
                || (Plugin.Configuration.RefreshLiveVideos && 
                video.Status == VideoStatus.Upcoming && !isPlaceholderStream(video.Title)))
            {
                filteredResult.Add(video);
            }
        }
        return filteredResult;
    }

    private async Task<IReadOnlyCollection<Video>> GetChannelVideosAsync()
    {
        return await Task.Run(() => getVideosByChannelID(Plugin.Configuration.Channels));
    }

    private async Task<IReadOnlyCollection<Video>> GetOrgVideosAsync()
    {
        return await Task.Run(() => getVideosByOrganization(Plugin.Configuration.Organizations));
    }


    public async Task ManualRefreshTable()
    {
        var channelVideosTask = GetChannelVideosAsync();
        var orgVideosTask = GetOrgVideosAsync();
        await Task.WhenAll(channelVideosTask, orgVideosTask);
        IReadOnlyCollection<Video> channelVideos = await channelVideosTask;
        IReadOnlyCollection<Video> orgVideos = await orgVideosTask;
        relayChanges(channelVideos.Concat(orgVideos).ToList());
        allVideos = channelVideos.Concat(orgVideos).ToList();
    }

    private void DrawTable(List<Video> videos)
    {
        if (videos.Count == 0)
        {
            return;
        }
        ImGui.BeginChild("VideoTableScroll", new ImVec2(0, 400), true, ImGuiWindowFlags.HorizontalScrollbar);
        ImGui.BeginTable("VideoTable", 4, ImGuiTableFlags.RowBg | ImGuiTableFlags.BordersInnerV);
        ImGui.TableNextRow(ImGuiTableRowFlags.Headers);
        ImGui.TableSetColumnIndex(0);
        ImGui.Text("Time");
        ImGui.TableSetColumnIndex(1);
        ImGui.Text("Channel Name");
        ImGui.TableSetColumnIndex(2);
        ImGui.Text("Title");
        ImGui.TableSetColumnIndex(3);
        ImGui.Text("URL");
        var sortedVideos = videos.OrderBy(v =>
        {
            if (DateTime.TryParseExact(v.ScheduledStart.ToString()!, "M/d/yyyy h:mm:ss tt", null, DateTimeStyles.None, out DateTime parsedDate))
            {
                return parsedDate;
            }
            else
            {
                return DateTime.MinValue;
            }
        }).ToList();

        foreach (var data in sortedVideos)
        {
            if ((data.Status == VideoStatus.Live && !Plugin.Configuration.RefreshLiveVideos)
                || (data.Status == VideoStatus.Upcoming && !Plugin.Configuration.RefreshUpcomingVideos))
            {
                continue;
            }
            ImGui.TableNextRow();
            ImGui.TableSetColumnIndex(0);
            if (data.Status == VideoStatus.Live)
            {
                ImGui.Text("Currently Live");
            }
            else
            {
                DateTime dateTimeUtc = DateTime.ParseExact(data.ScheduledStart.ToString()!, "M/d/yyyy h:mm:ss tt",
                CultureInfo.InvariantCulture);
                DateTime localTime = TimeZoneInfo.ConvertTimeFromUtc(dateTimeUtc, TimeZoneInfo.Local);
                ImGui.Text(localTime.ToString("M/d/yyyy h:mm:ss tt")); 
            }
            ImGui.TableSetColumnIndex(1);
            ImGui.Text(data.Channel.Name);
            ImGui.TableSetColumnIndex(2);
            ImGui.Text(data.Title);
            if (ImGui.IsItemHovered())
            {
                ImGui.BeginTooltip();
                ImGui.Text(data.Title);
                ImGui.EndTooltip();
            }
            
            ImGui.TableSetColumnIndex(3);
            string videoUrl = "https://www.youtube.com/watch?v=" + data.VideoId;
            if (ImGui.Button(videoUrl))
            {
                Process.Start("explorer", $"\"{videoUrl}\"");
            }
        }
        ImGui.EndTable();
        ImGui.EndChild();
    }

    private void relayChanges(List<Video> newVideoList) 
    {
        if (allVideos.Count() == 0)
        {
            return;
        }

        List<Video> diff = newVideoList
        .Where(newVideo => allVideos
            .Any(oldVideo => oldVideo.VideoId == newVideo.VideoId
                && oldVideo.Status == VideoStatus.Upcoming
                && newVideo.Status == VideoStatus.Live))
        .ToList();

        foreach (Video video in diff)
        {
            if (video.Status == VideoStatus.Live)
            {
                chat.PrintChat(new XivChatEntry() { Type = XivChatType.ErrorMessage, Name = "VTAlert", Message = 
                    video.Channel.Name 
                    + " is live: " + 
                    video.Title + " Link: " + "https://www.youtube.com/watch?v=" + video.VideoId});
            }
        }

    }

    public override void Draw()
    {
        ImGui.Text("Time Since Last Refresh: " + (DateTime.Now - lastRefresh).TotalSeconds.ToString());
        if (ImGui.Button("Show Settings"))
        {
            this.Plugin.DrawConfigUI();
        }
        ImGui.SameLine();
        if (ImGui.Button("Manual Refresh"))
        {
            chat.PrintChat(new XivChatEntry() { Type = XivChatType.ErrorMessage, Name = "VTAlert", Message = "Manually Refreshed" });
            ManualRefreshTable();
         
        }
        DrawTable(allVideos);


    }

    public override void Update()
    {
        if ((DateTime.Now - lastRefresh).TotalSeconds >= 600)
        {
            ManualRefreshTable();
            lastRefresh = DateTime.Now;
        }
    }

    

}
