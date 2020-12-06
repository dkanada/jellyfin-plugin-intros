using System;
using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.Intros.Configuration
{
    public class PluginConfiguration : BasePluginConfiguration
    {
        public int Intro { get; set; } = Plugin.DefaultIntro;

        public int Resolution { get; set; } = Plugin.DefaultResolution;

        public bool Random { get; set; } = false;

        public string OwnIntro {get; set; } = Plugin.DefaultOwnIntros;

        // used internally to track the current intro
        public Guid Id { get; set; }
    }
}
