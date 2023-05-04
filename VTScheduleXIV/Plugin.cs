using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using System.IO;
using Dalamud.Interface.Windowing;
using VTScheduleXIV.Windows;
using Dalamud.Game.Gui;

namespace VTScheduleXIV
{
    public sealed class Plugin : IDalamudPlugin
    {
        public string Name => "VTSchedule XIV";
        private const string CommandName = "/pvt";

        private DalamudPluginInterface PluginInterface { get; init; }
        private CommandManager CommandManager { get; init; }
        public Configuration Configuration { get; init; }
        public WindowSystem WindowSystem = new("VTSchedule XIV");
        // test build 2

        private ConfigWindow ConfigWindow { get; init; }
        private MainWindow MainWindow { get; init; }
        [PluginService] private ChatGui ChatGui { get; set; } = default;

        public Plugin(
            [RequiredVersion("1.0")] DalamudPluginInterface pluginInterface,
            [RequiredVersion("1.0")] CommandManager commandManager)
        {
            this.PluginInterface = pluginInterface;
            this.CommandManager = commandManager;

            this.Configuration = this.PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            this.Configuration.Initialize(this.PluginInterface);

            var imagePath = Path.Combine(PluginInterface.AssemblyLocation.Directory?.FullName!, "icon.png");
            var logoImage = this.PluginInterface.UiBuilder.LoadImage(imagePath);


            ConfigWindow = new ConfigWindow(this, logoImage);
            MainWindow = new MainWindow(this, ChatGui); 
            
            WindowSystem.AddWindow(ConfigWindow);
            WindowSystem.AddWindow(MainWindow);

            this.CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
            {
                HelpMessage = "Shows the main window for checking schedule"
            });

            this.PluginInterface.UiBuilder.Draw += DrawUI;
            this.PluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;
        }

        public void Dispose()
        {
            this.WindowSystem.RemoveAllWindows();
            ConfigWindow.Dispose();
            MainWindow.Dispose();
            
            this.CommandManager.RemoveHandler(CommandName);
        }

        private void OnCommand(string command, string args)
        {
            MainWindow.IsOpen = true;
        }

        private void DrawUI()
        {
            this.WindowSystem.Draw();
        }

        public void DrawConfigUI()
        {
            ConfigWindow.IsOpen = true;
        }
    }
}
