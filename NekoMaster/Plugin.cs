using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Logging;
using Dalamud.Plugin;
using Dalamud.Interface.Windowing;
using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using NekoMaster.Windows;
using NekoMaster.Reflection;
using System.Threading.Tasks;

namespace NekoMaster
{
    public sealed class Plugin : IDalamudPlugin
    {
        public string Name => "NekoMaster";
        private DalamudPluginInterface pi { get; init; }
        private CommandManager cmd { get; init; }
        private Assembly da { get; init; }
        private object pm { get; init; }
        public Configuration Configuration { get; init; }
        public WindowSystem WindowSystem = new("NekoMaster");

        private MainWindow MainWindow { get; init; }

        
        public Plugin(
            [RequiredVersion("1.0")] DalamudPluginInterface pluginInterface,
            [RequiredVersion("1.0")] CommandManager commandManager)
        {
            this.pi = pluginInterface;
            this.cmd = commandManager;
            DalamudApi.Initialize(this,pi);
            pi.UiBuilder.OpenConfigUi += DrawConfigUI;

            this.Configuration = this.pi.GetPluginConfig() as Configuration ?? new Configuration();
            this.Configuration.Initialize(this.pi);

            MainWindow = new MainWindow(this);
            WindowSystem.AddWindow(MainWindow);
            this.pi.UiBuilder.Draw += DrawUI;

            this.da = pi.GetType().Assembly;
            this.pm = da.GetService("Dalamud.Service`1", "Dalamud.Plugin.Internal.PluginManager");

            
            this.cmd.AddHandler("/load", new CommandInfo(OnCommand) { HelpMessage = "load plugin" });
            this.cmd.AddHandler("/unload", new CommandInfo(OnCommand) { HelpMessage = "unload plugin" });
            this.cmd.AddHandler("/reload", new CommandInfo(OnCommand) { HelpMessage = "reload plugin" });
            this.cmd.AddHandler("/loadall", new CommandInfo(OnCommand) { HelpMessage =   "load all plugins" });
            this.cmd.AddHandler("/unloadall", new CommandInfo(OnCommand) { HelpMessage = "unload all plugins" });
            this.cmd.AddHandler("/reloadall", new CommandInfo(OnCommand) { HelpMessage = "reload all plugins" });
            this.cmd.AddHandler("/errorfuck", new CommandInfo(OnCommand) { HelpMessage = "reset error" });
            this.cmd.AddHandler("/unloadthd", new CommandInfo(OnCommand) { HelpMessage = "unload third plugins" });
        }

        //internal List<string> plugins = new List<string>();
        public object? PluginEnums()
        {
            List<object> plugins = new List<object>();
            dynamic installedPlugins = pm.GetBF("InstalledPlugins");
            return installedPlugins;
        }

        public object? PluginQuery(string name)
        {
            dynamic installedPlugins = pm.GetBF("InstalledPlugins");
            foreach (var plugin in installedPlugins)
            {
                if((string)plugin.GetType().GetProperty("Name", ReflectionHelper.PubInsFlags).GetValue(plugin)==name)return plugin;
            }
            return null;
        }

        public async void PluginUnload(string name)
        {

            try {
                var plugin = PluginQuery(name);
                PluginLog.Log($"{plugin}");
                bool isLoaded = (bool)plugin.GetFoP("IsLoaded");
                string pst = (string)plugin.GetFoP("State").ToString();
                if (isLoaded &&!pst.Contains("Unloading"))
                {
                    await (Task)plugin.GetType().GetMethod("UnloadAsync")?.Invoke(plugin, new object[] { false, true });
                }
            }
            catch (Exception e)
            {
                PluginLog.Error($"{e.Message}\n{e.StackTrace}");
            }
        }
        public async void PluginLoad(string name)
        {
            try
            {
                var plugin = PluginQuery(name);
                PluginLog.Log($"{plugin}");
                bool isLoaded = (bool)plugin.GetFoP("IsLoaded");
                string pst = (string)plugin.GetFoP("State").ToString();
                object manifest = plugin.GetFoP("Manifest");
                if (pst.Contains("Error"))
                {
                    plugin.SetFoP("State", 0);
                    manifest.SetFoP("Disabled", false);
                }
                if (!isLoaded)
                {
                    await (Task)plugin.GetType().GetMethod("LoadAsync")?.Invoke(plugin, new object[] { 3, false });
                }
            }
            catch (Exception e)
            {
                PluginLog.Error($"{e.Message}\n{e.StackTrace}");
            }
        }
        public async void PluginReload(string name)
        {
            try
            {
                var plugin = PluginQuery(name);
                PluginLog.Log($"{plugin}");
                bool isLoaded = (bool)plugin.GetFoP("IsLoaded");
                if (isLoaded)
                {
                    await (Task)plugin.GetType().GetMethod("ReloadAsync")?.Invoke(plugin, new object[] { });
                }
            }
            catch (Exception e)
            {
                PluginLog.Error($"{e.Message}\n{e.StackTrace}");
            }
        }
        public async void ErrorFuck()
        {
            dynamic installedPlugins = pm.GetBF("InstalledPlugins");
            foreach (object plugin in installedPlugins)
            {
                string pst = (string)plugin.GetFoP("State").ToString();
                object manifest = plugin.GetFoP("Manifest");
                if (pst.Contains("Error")) {
                    plugin.SetFoP("State", 0);
                    manifest.SetFoP("Disabled",false);
                }

            }
        }
        public async void PluginUnloadAll()
        {
            dynamic plugins = PluginEnums();
            foreach (object p in plugins)
            {
                string pname = (string)p.GetFoP("Name");
                if (p != null && Name != pname)
                    PluginUnload(pname);
            }
        }
        public async void PluginUnloadThird()
        {
            dynamic plugins = PluginEnums();
            foreach (object p in plugins)
            {
                string pname = (string)p.GetFoP("Name");
                object manifest = p.GetFoP("Manifest");
                bool isThird = (bool)manifest.GetFoP("IsThirdParty");
                if (p != null && isThird && Name != pname )
                    PluginUnload(pname);
            }
        }
        public async void PluginLoadAll()
        {
            dynamic plugins = PluginEnums();
            foreach (object p in plugins)
            {
                string pname = (string)p.GetFoP("Name");
                if (p != null && Name != pname)
                    PluginLoad(pname);
            }
        }
        public async void PluginReloadAll()
        {
            dynamic plugins = PluginEnums();
            foreach (object p in plugins)
            {
                string pname = (string)p.GetFoP("Name");
                if (p != null && Name != pname)
                    PluginReload(pname);
            }
        }

        public void Dispose()
        {
            this.cmd.RemoveHandler("/load");
            this.cmd.RemoveHandler("/unload");
            this.cmd.RemoveHandler("/reload");
            this.cmd.RemoveHandler("/loadall");
            this.cmd.RemoveHandler("/unloadall");
            this.cmd.RemoveHandler("/reloadall");
            this.cmd.RemoveHandler("/errorfuck");
            this.cmd.RemoveHandler("/unloadthd");
            this.WindowSystem.RemoveAllWindows();
            
            MainWindow.Dispose();
            DalamudApi.Dispose();
        }

        private void OnCommand(string command, string args)
        {
            // in response to the slash command, just display our main ui
            MainWindow.IsOpen = true;
            PluginLog.Log($"{command} {args}");
            switch (command)
            {
                case "/load": PluginLoad(args);break;
                case "/unload": PluginUnload(args); break;
                case "/reload": PluginReload(args); break;
                case "/loadall": PluginLoadAll(); break;
                case "/unloadall": PluginUnloadAll(); break;
                case "/reloadall": PluginReloadAll(); break;
                case "/errorfuck": ErrorFuck(); break;
                case "/unloadthd": PluginUnloadThird(); break;
                default: ;break;
            }
        }

        private void DrawUI()
        {
            this.WindowSystem.Draw();
        }

        public void DrawConfigUI()
        {
            MainWindow.IsOpen = true;
        }
    }
}
