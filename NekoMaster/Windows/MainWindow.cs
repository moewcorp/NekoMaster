using System;
using System.Collections.Generic;
using System.Numerics;
using System.Xml.Linq;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using ImGuiScene;
using NekoMaster.Reflection;

namespace NekoMaster.Windows;

public class MainWindow : Window, IDisposable
{
    private Plugin Plugin;

    public MainWindow(Plugin plugin) : base(
        "MoewPluginManager", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
    {
        this.SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(475, 330),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };
        this.Plugin = plugin; 
    }

    public void Dispose()
    {
    }

    public override void Draw()
    {
        dynamic plugins = Plugin.PluginEnums();
        ImGui.SameLine();
        if (ImGui.Button($"ReloadAll"))
        {
            Plugin.PluginReloadAll();
        }
        ImGui.SameLine();
        if (ImGui.Button($"LoadAll"))
        {
            Plugin.PluginLoadAll();
        }
        ImGui.SameLine();
        if (ImGui.Button($"UnloadAll"))
        {
            Plugin.PluginUnloadAll();
        }
        ImGui.SameLine();
        if (ImGui.Button($"ErrorFuck"))
        {
            Plugin.ErrorFuck();
        }
        ImGui.BeginTable("PluginManager",6);
        ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.None, 4);
        ImGui.TableSetupColumn("isOn",ImGuiTableColumnFlags.None,2);
        ImGui.TableSetupColumn("isDev", ImGuiTableColumnFlags.None, 2);
        ImGui.TableSetupColumn("isThird", ImGuiTableColumnFlags.None, 2);
        ImGui.TableSetupColumn("State", ImGuiTableColumnFlags.None, 2);
        ImGui.TableSetupColumn("Method", ImGuiTableColumnFlags.None, 6);
        ImGui.TableHeadersRow();

        foreach (object p in plugins)
        {
            string pname = (string)p.GetFoP("Name");
            string pstat = p.GetFoP("State").ToString();
            object manifest = p.GetFoP("Manifest");
            bool isDisabled = (bool)p.GetFoP("IsDisabled");
            bool disabled = (bool)manifest.GetFoP("Disabled");
            bool isDev = (bool)p.GetFoP("IsDev");
            bool isThird = (bool)manifest.GetFoP("IsThirdParty");
            ImGui.TableNextColumn();
            ImGui.Text(pname);
            ImGui.TableNextColumn();
            ImGui.Text(isDisabled? "×" : "√");
            ImGui.SameLine();
            ImGui.Text(disabled ? "×" : "√");
            ImGui.TableNextColumn();
            ImGui.Text(isDev ? "√" : "×");
            ImGui.TableNextColumn();
            ImGui.Text(isThird ? "√" : "×");
            ImGui.TableNextColumn();
            ImGui.Text(pstat);
            ImGui.TableNextColumn();
            if (ImGui.Button($"Load##{pname}"))
            {
                Plugin.PluginLoad(pname);
            };
            ImGui.SameLine();
            if (ImGui.Button($"Unload##{pname}")) {
                Plugin.PluginUnload(pname);
            };
            ImGui.SameLine();
            if (ImGui.Button($"Reload##{pname}"))
            {
                Plugin.PluginReload(pname);
            };
        }
        ImGui.EndTable();
        ImGui.Spacing();

    }
}
