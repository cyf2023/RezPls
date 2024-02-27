using System;
using System.Collections.Generic;
using System.Numerics;
using ImGuiNET;
using RezPls.Managers;

namespace RezPls.GUI
{
    public class Interface : IDisposable
    {
        public const string PluginName = "RezPls";

        private readonly string _configHeader;
        private readonly RezPls _plugin;

        private          string          _statusFilter = string.Empty;
        private readonly HashSet<string> _seenNames;

        public bool Visible;

        public bool TestMode = false;

        private static void ChangeAndSave<T>(T value, T currentValue, Action<T> setter) where T : IEquatable<T>
        {
            if (value.Equals(currentValue))
                return;

            setter(value);
            RezPls.Config.Save();
        }

        public Interface(RezPls plugin)
        {
            _plugin       = plugin;
            _configHeader = RezPls.Version.Length > 0 ? $"{PluginName} v{RezPls.Version}###{PluginName}" : PluginName;
            _seenNames    = new HashSet<string>(_plugin.StatusSet.DisabledStatusSet.Count + _plugin.StatusSet.EnabledStatusSet.Count);

            Dalamud.PluginInterface.UiBuilder.Draw         += Draw;
            Dalamud.PluginInterface.UiBuilder.OpenConfigUi += Enable;
        }

        private static void DrawCheckbox(string name, string tooltip, bool value, Action<bool> setter)
        {
            var tmp = value;
            if (ImGui.Checkbox(name, ref tmp))
                ChangeAndSave(tmp, value, setter);

            if (ImGui.IsItemHovered())
                ImGui.SetTooltip(tooltip);
        }

        private void DrawEnabledCheckbox()
            => DrawCheckbox("启用", "启用或禁用这个插件。", RezPls.Config.Enabled, e =>
            {
                RezPls.Config.Enabled = e;
                if (e)
                    _plugin.Enable();
                else
                    _plugin.Disable();
            });

        private void DrawHideSymbolsOnSelfCheckbox()
            => DrawCheckbox("隐藏自己身上的图标", "隐藏游戏中玩家角色身上的图标以及文本。",
                RezPls.Config.HideSymbolsOnSelf,    e => RezPls.Config.HideSymbolsOnSelf = e);

        private void DrawEnabledRaiseCheckbox()
            => DrawCheckbox("启用复活高亮",
                "高亮正在被复活的玩家。", RezPls.Config.EnabledRaise, e => RezPls.Config.EnabledRaise = e);

        private void DrawShowGroupCheckbox()
            => DrawCheckbox("在小队界面中高亮",
                "根据你选择的颜色和状态在你的小队界面高亮玩家。",
                RezPls.Config.ShowGroupFrame,
                e => RezPls.Config.ShowGroupFrame = e);

        private void DrawShowAllianceCheckbox()
            => DrawCheckbox("在团队界面中高亮",
                "根据你选择的颜色和状态在你的团队界面高亮玩家",
                RezPls.Config.ShowAllianceFrame,
                e => RezPls.Config.ShowAllianceFrame = e);

        private void DrawShowCasterNamesCheckbox()
            => DrawCheckbox("写出施法者的名字",
                "在高亮玩家时，也在界面中写出正在对其使用复活或者康复的施法者的名字。",
                RezPls.Config.ShowCasterNames,
                e => RezPls.Config.ShowCasterNames = e);

        private void DrawShowIconCheckbox()
            => DrawCheckbox("在世界中绘制图标",
                "在正在被复活或已复活的尸体上绘制一个已复活图标", RezPls.Config.ShowIcon,
                e => RezPls.Config.ShowIcon = e);

        private void DrawShowIconDispelCheckbox()
            => DrawCheckbox("在世界中绘制图标##Dispel",
                "在有可康复的负面状态的玩家身上绘制一个负面状态图标。", RezPls.Config.ShowIconDispel,
                e => RezPls.Config.ShowIconDispel = e);

        private void DrawShowInWorldTextCheckbox()
            => DrawCheckbox("在世界中绘制文本",
                "在正在被复活或已复活的尸体下注明当前复活施法者。",
                RezPls.Config.ShowInWorldText,
                e => RezPls.Config.ShowInWorldText = e);

        private void DrawShowInWorldTextDispelCheckbox()
            => DrawCheckbox("在世界中绘制文本##Dispel",
                "在正在被康复的玩家下注明当前康复施法者。",
                RezPls.Config.ShowInWorldTextDispel,
                e => RezPls.Config.ShowInWorldTextDispel = e);

        private void DrawRestrictJobsCheckbox()
            => DrawCheckbox("仅限有复活的职业",
                "仅在你是具有复活能力的职业时显示复活信息。\n"
              + "幻术，白魔，秘术，学者，召唤，占星，青魔，赤魔（64级以上）。\n"
              + "不包括失传技能和文理技能。\n", RezPls.Config.RestrictedJobs,
                e => RezPls.Config.RestrictedJobs = e);

        private void DrawDispelHighlightingCheckbox()
            => DrawCheckbox("启用可康复高亮",
                "高亮具有可康复负面状态的玩家。",
                RezPls.Config.EnabledDispel, e => RezPls.Config.EnabledDispel = e);

        private void DrawRestrictJobsDispelCheckbox()
            => DrawCheckbox("仅限有康复的职业",
                "仅在你是具有康复能力的职业时显示康复信息。\n"
              + "幻术，白魔，学者，占星，诗人（35级以上），青魔\n",
                RezPls.Config.RestrictedJobsDispel, e => RezPls.Config.RestrictedJobsDispel = e);

        private void DrawTestModeCheckBox1()
            => DrawCheckbox("玩家复活测试", "应在玩家角色和小队界面上显示激活的“已复活”效果。",
                ActorWatcher.TestMode == 1,       e => ActorWatcher.TestMode = e ? 1 : 0);

        private void DrawTestModeCheckBox2()
            => DrawCheckbox("玩家被目标复活测试",
                "应在玩家角色和小队界面上显示激活的“被复活中”效果，施法者为玩家当前目标。",
                ActorWatcher.TestMode == 2, e => ActorWatcher.TestMode = e ? 2 : 0);

        private void DrawTestModeCheckBox3()
            => DrawCheckbox("玩家不需要复活测试",
                "应在玩家角色上显示激活的“不需要复活”效果，就像玩家角色与当前目标复活的一样。",
                ActorWatcher.TestMode == 3, e => ActorWatcher.TestMode = e ? 3 : 0);

        private void DrawTestModeCheckBox4()
            => DrawCheckbox("玩家负面状态测试",
                "应在玩家角色上显示激活的“具有被监控的状态”效果，就像玩家角色有被监控的状态一样。",
                ActorWatcher.TestMode == 4, e => ActorWatcher.TestMode = e ? 4 : 0);

        private void DrawTestModeCheckBox5()
            => DrawCheckbox("玩家负面状态被康复测试",
                "应在玩家角色上显示激活的“被康复中”效果，就像正在被当前目标康复一样。",
                ActorWatcher.TestMode == 5, e => ActorWatcher.TestMode = e ? 5 : 0);

        private void DrawTestModeCheckBox6()
            => DrawCheckbox("玩家不需要康复测试",
                "应在玩家角色上显示激活的“不需要康复”效果，就像被两个人康复或者康复没有可驱散状态一样。",
                ActorWatcher.TestMode == 6, e => ActorWatcher.TestMode = e ? 6 : 0);


        private void DrawSingleStatusEffectList(string header, bool which, float width)
        {
            using var group = ImGuiRaii.NewGroup();
            var       list  = which ? _plugin.StatusSet.DisabledStatusSet : _plugin.StatusSet.EnabledStatusSet;
            _seenNames.Clear();
            if (ImGui.BeginListBox($"##{header}box", width / 2 * Vector2.UnitX))
            {
                for (var i = 0; i < list.Count; ++i)
                {
                    var (status, name) = list[i];
                    if (!name.Contains(_statusFilter) || _seenNames.Contains(name))
                        continue;

                    _seenNames.Add(name);
                    if (ImGui.Selectable($"{status.Name}##status{status.RowId}"))
                    {
                        _plugin.StatusSet.Swap((ushort) status.RowId);
                        --i;
                    }
                }

                ImGui.EndListBox();
            }

            if (which)
            {
                if (ImGui.Button("禁用所有状态", width / 2 * Vector2.UnitX))
                    _plugin.StatusSet.ClearEnabledList();
            }
            else if (ImGui.Button("启用所有状态", width / 2 * Vector2.UnitX))
            {
                _plugin.StatusSet.ClearDisabledList();
            }
        }

        private static void DrawStatusSelectorTitles(float width)
        {
            const string disabledHeader = "禁用的状态";
            const string enabledHeader  = "监控的状态";
            var          pos1           = width / 4 - ImGui.CalcTextSize(disabledHeader).X / 2;
            var          pos2           = 3 * width / 4 + ImGui.GetStyle().ItemSpacing.X - ImGui.CalcTextSize(enabledHeader).X / 2;
            ImGui.SetCursorPosX(pos1);
            ImGui.AlignTextToFramePadding();
            ImGui.Text(disabledHeader);
            ImGui.SameLine(pos2);
            ImGui.AlignTextToFramePadding();
            ImGui.Text(enabledHeader);
        }

        private void DrawStatusEffectList()
        {
            var width = ImGui.GetWindowContentRegionMax().X - ImGui.GetWindowContentRegionMin().X - ImGui.GetStyle().ItemSpacing.X;
            DrawStatusSelectorTitles(width);
            ImGui.SetNextItemWidth(width);
            ImGui.InputTextWithHint("##statusFilter", "Filter...", ref _statusFilter, 64);
            DrawSingleStatusEffectList("禁用的状态", true, width);
            ImGui.SameLine();
            DrawSingleStatusEffectList("监控的状态", false, width);
        }


        private void DrawColorPicker(string name, string tooltip, uint value, uint defaultValue, Action<uint> setter)
        {
            const ImGuiColorEditFlags flags = ImGuiColorEditFlags.AlphaPreviewHalf | ImGuiColorEditFlags.NoInputs;

            var tmp = ImGui.ColorConvertU32ToFloat4(value);
            if (ImGui.ColorEdit4($"##{name}", ref tmp, flags))
                ChangeAndSave(ImGui.ColorConvertFloat4ToU32(tmp), value, setter);
            ImGui.SameLine();
            if (ImGui.Button($"Default##{name}"))
                ChangeAndSave(defaultValue, value, setter);
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip(
                    $"Reset to default: #{defaultValue & 0xFF:X2}{(defaultValue >> 8) & 0xFF:X2}{(defaultValue >> 16) & 0xFF:X2}{defaultValue >> 24:X2}");
            ImGui.SameLine();
            ImGui.Text(name);
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip(tooltip);
        }

        private void DrawCurrentRaiseColorPicker()
            => DrawColorPicker("正在被复活",
                "正在被其他玩家或者你自己复活的玩家的高亮色。",
                RezPls.Config.CurrentlyRaisingColor, RezPlsConfig.DefaultCurrentlyRaisingColor, c => RezPls.Config.CurrentlyRaisingColor = c);


        private void DrawAlreadyRaisedColorPicker()
            => DrawColorPicker("已复活",
                "已复活且没有正在被你复活的玩家的高亮色。",
                RezPls.Config.RaisedColor, RezPlsConfig.DefaultRaisedColor, c => RezPls.Config.RaisedColor = c);

        private void DrawDoubleRaiseColorPicker()
            => DrawColorPicker("冗余咏唱",
                "你正在复活已复活的玩家或其他人与你一同复活的玩家的高亮色，\n"
              + "以及你与他人一起康复的玩家或康复没有被监控的负面状态的玩家的高亮色。",
                RezPls.Config.DoubleRaiseColor, RezPlsConfig.DefaultDoubleRaiseColor, c => RezPls.Config.DoubleRaiseColor = c);

        private void DrawInWorldBackgroundColorPicker()
            => DrawColorPicker("在世界中的背景色",
                "世界中显示在尸体上的文本的背景色。",
                RezPls.Config.InWorldBackgroundColor, RezPlsConfig.DefaultInWorldBackgroundColorRaise,
                c => RezPls.Config.InWorldBackgroundColor = c);

        private void DrawInWorldBackgroundColorPickerDispel()
            => DrawColorPicker("在世界中的背景色（康复）",
                "世界中显示在具有被监控的负面状态的玩家上的文本的背景色。",
                RezPls.Config.InWorldBackgroundColorDispel, RezPlsConfig.DefaultInWorldBackgroundColorDispel,
                c => RezPls.Config.InWorldBackgroundColorDispel = c);

        private void DrawDispellableColorPicker()
            => DrawColorPicker("具有被监控的状态",
                "具有任何被监控的负面状态的玩家的高亮色。",
                RezPls.Config.DispellableColor, RezPlsConfig.DefaultDispellableColor, c => RezPls.Config.DispellableColor = c);

        private void DrawCurrentlyDispelledColorPicker()
            => DrawColorPicker("正在被康复",
                "正在被其他玩家或者仅你自己康复的玩家的高亮色。",
                RezPls.Config.CurrentlyDispelColor, RezPlsConfig.DefaultCurrentlyDispelColor, c => RezPls.Config.CurrentlyDispelColor = c);

        private void DrawScaleButton()
        {
            const float min  = 0.1f;
            const float max  = 3.0f;
            const float step = 0.005f;

            var tmp = RezPls.Config.IconScale;
            if (ImGui.DragFloat("在世界中的图标尺寸", ref tmp, step, min, max))
                ChangeAndSave(tmp, RezPls.Config.IconScale, f => RezPls.Config.IconScale = Math.Max(min, Math.Min(f, max)));
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("设置在世界中的被复活的尸体上绘制的已复活图标的尺寸。");
        }

        private static readonly string[] RectTypeStrings = new[]
        {
            "填充",
            "仅描边",
            "仅不透明描边",
            "填充与不透明描边",
        };

        private void DrawRectTypeSelector()
        {
            var type = (int) RezPls.Config.RectType;
            if (!ImGui.Combo("矩形高亮框样式", ref type, RectTypeStrings, RectTypeStrings.Length))
                return;

            ChangeAndSave(type, (int) RezPls.Config.RectType, t => RezPls.Config.RectType = (RectType) t);
        }

        public void Draw()
        {
            if (!Visible)
                return;

            var buttonHeight      = ImGui.GetFontSize() + ImGui.GetStyle().FramePadding.Y * 2;
            var horizontalSpacing = new Vector2(0, ImGui.GetTextLineHeightWithSpacing());

            var height = 15 * buttonHeight
              + 6 * horizontalSpacing.Y
              + 27 * ImGui.GetStyle().ItemSpacing.Y;
            var width       = 450 * ImGui.GetIO().FontGlobalScale;
            var constraints = new Vector2(width, height);
            ImGui.SetNextWindowSizeConstraints(constraints, constraints);

            if (!ImGui.Begin(_configHeader, ref Visible, ImGuiWindowFlags.NoResize))
                return;

            try
            {
                DrawEnabledCheckbox();

                if (ImGui.CollapsingHeader("复活设置"))
                {
                    DrawEnabledRaiseCheckbox();
                    DrawRestrictJobsCheckbox();
                    DrawShowIconCheckbox();
                    DrawShowInWorldTextCheckbox();
                    ImGui.Dummy(horizontalSpacing);
                }

                if (ImGui.CollapsingHeader("康复设置"))
                {
                    DrawDispelHighlightingCheckbox();
                    DrawRestrictJobsDispelCheckbox();
                    DrawShowIconDispelCheckbox();
                    DrawShowInWorldTextDispelCheckbox();
                    ImGui.Dummy(horizontalSpacing);
                    DrawStatusEffectList();
                    ImGui.Dummy(horizontalSpacing);
                }

                if (ImGui.CollapsingHeader("通用设置"))
                {
                    DrawHideSymbolsOnSelfCheckbox();
                    DrawShowGroupCheckbox();
                    DrawShowAllianceCheckbox();
                    DrawShowCasterNamesCheckbox();
                    DrawRectTypeSelector();
                    DrawScaleButton();
                    ImGui.Dummy(horizontalSpacing);
                }

                if (ImGui.CollapsingHeader("颜色"))
                {
                    DrawCurrentRaiseColorPicker();
                    DrawAlreadyRaisedColorPicker();
                    ImGui.Dummy(horizontalSpacing);
                    DrawDispellableColorPicker();
                    DrawCurrentlyDispelledColorPicker();
                    ImGui.Dummy(horizontalSpacing);
                    DrawDoubleRaiseColorPicker();
                    ImGui.Dummy(horizontalSpacing);
                    DrawInWorldBackgroundColorPicker();
                    DrawInWorldBackgroundColorPickerDispel();
                    ImGui.Dummy(horizontalSpacing);
                }

                if (ImGui.CollapsingHeader("测试"))
                {
                    DrawTestModeCheckBox1();
                    DrawTestModeCheckBox2();
                    DrawTestModeCheckBox3();
                    DrawTestModeCheckBox4();
                    DrawTestModeCheckBox5();
                    DrawTestModeCheckBox6();
                }
            }
            finally
            {
                ImGui.End();
            }
        }

        public void Enable()
            => Visible = true;

        public void Dispose()
        {
            Dalamud.PluginInterface.UiBuilder.Draw         -= Draw;
            Dalamud.PluginInterface.UiBuilder.OpenConfigUi -= Enable;
        }
    }
}
