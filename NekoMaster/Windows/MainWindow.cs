using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Numerics;
using System.Reflection;
using System.Text;
using System.Xml.Linq;
using Dalamud.Game.Command;
using Dalamud.Hooking.Internal;
using Dalamud.Interface.Windowing;
using Dalamud.Logging;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
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


    private string filterName = string.Empty;
    public override void Draw()
    {

        ImGui.BeginTabBar("NekoNeko");

        if (ImGui.BeginTabItem("Plugins")) { 
        
            dynamic plugins = Plugin.PluginEnums();

            if (ImGui.Button($"ReloadAll"))
            {
                Plugin.PluginReloadAll();
            }
            RightClickToCopyCmd($"/reloadall");
            ImGui.SameLine();
            if (ImGui.Button($"LoadAll"))
            {
                Plugin.PluginLoadAll();
            }
            RightClickToCopyCmd($"/loadall");
            ImGui.SameLine();
            if (ImGui.Button($"UnloadAll"))
            {
                Plugin.PluginUnloadAll();
            }
            RightClickToCopyCmd($"/unload");
            ImGui.SameLine();
            if (ImGui.Button($"ErrorFuck"))
            {
                Plugin.ErrorFuck();
            }
            RightClickToCopyCmd($"/errerfuck");
            ImGui.SameLine();
            if (ImGui.Button($"UnloadThird"))
            {
                Plugin.PluginUnloadThird();
            }
            RightClickToCopyCmd($"/unloadthd");
            ImGui.SameLine();
            ImGui.InputText("Search", ref filterName, 20, ImGuiInputTextFlags.None);
            if (ImGui.BeginTable("PluginManager", 6, ImGuiTableFlags.ScrollY | ImGuiTableFlags.Resizable))
            {
                ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.None, 4);
                ImGui.TableSetupColumn("isOn", ImGuiTableColumnFlags.None, 2);
                ImGui.TableSetupColumn("isDev", ImGuiTableColumnFlags.None, 2);
                ImGui.TableSetupColumn("isThird", ImGuiTableColumnFlags.None, 2);
                ImGui.TableSetupColumn("State", ImGuiTableColumnFlags.None, 2);
                ImGui.TableSetupColumn("Method", ImGuiTableColumnFlags.None, 6);
                ImGui.TableHeadersRow();


                foreach (object p in plugins)
                {
                    string pname = (string)p.GetFoP("Name");
                    if (pname == null || !pname.Contains(filterName,StringComparison.OrdinalIgnoreCase)) continue;
                    string pstat = p.GetFoP("State").ToString();
                    object manifest = p.GetFoP("Manifest");
                    bool isDisabled = (bool)p.GetFoP("IsDisabled");
                    bool disabled = (bool)manifest.GetFoP("Disabled");
                    bool isDev = (bool)p.GetFoP("IsDev");
                    bool isThird = (bool)manifest.GetFoP("IsThirdParty");
                    ImGui.TableNextColumn();
                    ImGui.Text(pname);
                    ImGui.TableNextColumn();
                    ImGui.Text(isDisabled ? "×" : "√");
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
                    RightClickToCopyCmd($"/load {pname}");
                    ImGui.SameLine();
                    if (ImGui.Button($"Unload##{pname}"))
                    {
                        Plugin.PluginUnload(pname);
                    };
                    RightClickToCopyCmd($"/unload {pname}");
                    ImGui.SameLine();
                    if (ImGui.Button($"Reload##{pname}"))
                    {
                        Plugin.PluginReload(pname);
                    };
                    RightClickToCopyCmd($"/reload {pname}");
                    if(!isDisabled&&!disabled&&(bool)p.GetFoP("DalamudInterface").GetFoP("UiBuilder").GetFoP("HasConfigUi"))
                    { 
                        ImGui.SameLine();
                        if (ImGui.Button($"UI##{pname}"))
                        {
                            Plugin.PluginOpenUI(pname);
                        }
                        RightClickToCopyCmd($"/openui {pname}");
                    }
                    else
                    {
                        ImGui.SameLine();
                        ImGui.Button($"X  ##{pname}");
                    }
                }
                ImGui.EndTable();
            }
            //ImGui.Spacing();
            ImGui.EndTabItem();
        }
        

        if (ImGui.BeginTabItem("Commands")) {
            
            ImGui.InputText($"##genic",ref CmdGicArg, 64);
            ImGui.SameLine();
            if (ImGui.Button("Dispatch"))
            {
                PluginLog.Log(CmdGicArg);
                DalamudApi.CommandManager.ProcessCommand(CmdGicArg);
            }
            ImGui.SameLine();
            ImGui.SetNextItemWidth(64 * 2);
            ImGui.InputText("Search", ref filterName, 10, ImGuiInputTextFlags.None);
            if (ImGui.BeginTable("CommandManager", 4, ImGuiTableFlags.ScrollY | ImGuiTableFlags.Resizable)) {
                ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.None, 0.5f);
                ImGui.TableSetupColumn("Args", ImGuiTableColumnFlags.None, 0.5f);
                ImGui.TableSetupColumn("Asseambly", ImGuiTableColumnFlags.None,0.5f);
                ImGui.TableSetupColumn("HelpMessage", ImGuiTableColumnFlags.None, 2);
                ImGui.TableHeadersRow();
                foreach (KeyValuePair<string, CommandInfo> cmd in DalamudApi.CommandManager.Commands)
                {
                    var ass = (string)cmd.Value.GetFoP("LoaderAssemblyName");
                    if (!ass.Contains(filterName,StringComparison.OrdinalIgnoreCase)) continue;
                    if (!CmdTempArg.ContainsKey(cmd.Key))
                    {
                        CmdTempArg.Add(cmd.Key, string.Empty);
                    }
                    ImGui.TableNextColumn();
                    if (ImGui.Button(cmd.Key))
                    {
                        DalamudApi.CommandManager.ProcessCommand($"{cmd.Key} {CmdTempArg[cmd.Key]}");
                    }
                    RightClickToCopyCmd(cmd.Key);
                    ImGui.TableNextColumn();
                    var arg = CmdTempArg[cmd.Key];
                    ImGui.InputText($"##{cmd.Key}", ref arg, 64);
                    CmdTempArg[cmd.Key] = arg;
                    ImGui.TableNextColumn();
                    ImGui.Text(ass);
                    ImGui.TableNextColumn();
                    ImGui.Text(cmd.Value.HelpMessage);
                }

                ImGui.EndTable();
            }
            ImGui.EndTabItem();
            
        }
        if (ImGui.BeginTabItem("Hooks"))
        {
            if (ImGui.BeginTable("CommandManager", 4, ImGuiTableFlags.ScrollY | ImGuiTableFlags.Resizable))
            {
                ImGui.TableSetupColumn("Guid", ImGuiTableColumnFlags.None, 0.5f);
                ImGui.TableSetupColumn("Addr", ImGuiTableColumnFlags.None, 0.5f);
                ImGui.TableSetupColumn("Assembly", ImGuiTableColumnFlags.None, 0.5f);
                ImGui.TableSetupColumn("Delegate", ImGuiTableColumnFlags.None, 0.5f);
                ImGui.TableHeadersRow();
                dynamic hookDict = Plugin.hm.GetFoP("TrackedHooks");

                foreach (object hook in hookDict)
                {
                    var addr = hook.GetFoP("Value").GetFoP("InProcessMemory");
                    if (addr == null || (ulong)addr == 0) continue;
                    ImGui.TableNextColumn();
                    ImGui.Text(hook.GetFoP("Key").ToString());
                    ImGui.TableNextColumn();
                    ImGui.Text($"ffxiv.exe+0x{addr:X}");
                    ImGui.TableNextColumn();
                    ImGui.Text(hook.GetFoP("Value").GetFoP("Assembly").ToString().Split(",")[0]);
                    ImGui.TableNextColumn();
                    ImGui.Text($"{hook.GetFoP("Value").GetFoP("Delegate")}");
                }
                ImGui.EndTable();
            }
            ImGui.EndTabItem();
        }
        

        ImGui.EndTabBar();
    }
    private string CmdGicArg = string.Empty;
    private Dictionary<string, string> CmdTempArg = new Dictionary<string, string>();


    public void RightClickToCopyCmd(string cmd)
    {
        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip($"Right-click to copy command:\n  {cmd}");
        }

        if (ImGui.IsItemHovered() && ImGui.IsItemClicked(ImGuiMouseButton.Right))
        {
            ImGui.SetClipboardText($"{cmd}");
        }
    }
}
