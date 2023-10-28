﻿using System.Reflection;
using Dalamud.Game.Command;
using Dalamud.Plugin;
using RezPls.GUI;
using RezPls.Managers;

namespace RezPls
{
    // auto-format:off

    public partial class RezPls : IDalamudPlugin
    {
        public string Name
            => "RezPls";

        public static string Version = "";

        public static    RezPlsConfig Config { get; private set; } = null!;
        private readonly ActorWatcher _actorWatcher;
        private readonly Overlay      _overlay;
        private readonly Interface    _interface;

        public StatusSet StatusSet;

        public RezPls(DalamudPluginInterface pluginInterface)
        {
            Dalamud.Initialize(pluginInterface);
            Version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "";
            Config  = RezPlsConfig.Load();

            StatusSet     = new StatusSet();
            _actorWatcher = new ActorWatcher(StatusSet);
            _overlay      = new Overlay(_actorWatcher);
            _interface    = new Interface(this);

            if (Config.Enabled)
                Enable();
            else
                Disable();
            Dalamud.Commands.AddHandler("/rezpls", new CommandInfo(OnRezPls)
            {
                HelpMessage = "打开RezPls设置窗口。",
                ShowInHelp  = true,
            });
        }

        public void OnRezPls(string _, string arguments)
        {
            _interface!.Visible = !_interface.Visible;
        }

        public void Enable()
        {
            _actorWatcher!.Enable();
            _overlay!.Enable();
        }

        public void Disable()
        {
            _actorWatcher!.Disable();
            _overlay!.Disable();
        }

        public void Dispose()
        {
            Dalamud.Commands.RemoveHandler("/rezpls");
            _interface.Dispose();
            _overlay.Dispose();
            _actorWatcher.Dispose();
        }
    }
}
