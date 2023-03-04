using System;
using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.Intros.Configuration
{
    public class PluginConfiguration : BasePluginConfiguration
    {
        public string Local { get; set; } = string.Empty;

        public string Vimeo { get; set; } = string.Empty;

        public int Intro { get; set; } = Plugin.DefaultIntro;

        public int Resolution { get; set; } = Plugin.DefaultResolution;

        public bool Random { get; set; } = false;

        public bool ShowIntrosOnMovies { get; set; } = true;

        public bool ShowIntrosOnEpisodes { get; set; } = false;

        // used internally to track the current intro
        public Guid Id { get; set; }
    }
}
