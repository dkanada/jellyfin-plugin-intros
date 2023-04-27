using System;
using System.Collections.Generic;
using Jellyfin.Plugin.LocalIntros.Configuration;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;

namespace Jellyfin.Plugin.LocalIntros
{
    public class LocalIntrosPlugin : BasePlugin<IntroPluginConfiguration>, IHasWebPages
    {
        public override string Name => "Local Intros";

        public override Guid Id => Guid.Parse("07d86795-01f2-4d22-b174-cdc6056c3e7c");

        public const int DefaultResolution = 1080;

        public static LocalIntrosPlugin Instance { get; private set; }

        public static ILibraryManager LibraryManager { get; private set; }

        public LocalIntrosPlugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer, ILibraryManager libraryManager)
            : base(applicationPaths, xmlSerializer)
        {
            Instance = this;
            LibraryManager = libraryManager;
        }

        public IEnumerable<PluginPageInfo> GetPages()
        {
            yield return new PluginPageInfo
            {
                Name = Name,
                EmbeddedResourcePath = GetType().Namespace + ".Configuration.config.html"
            };
        }
    }
}
