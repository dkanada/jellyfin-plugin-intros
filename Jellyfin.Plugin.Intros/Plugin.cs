using System;
using System.Collections.Generic;
using Jellyfin.Plugin.Intros.Configuration;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;

namespace Jellyfin.Plugin.Intros
{
    public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
    {
        public override string Name => "Intros";

        public override Guid Id => Guid.Parse("9482dc3b-48aa-4d3b-8224-9128d1e8e0cd");

        public const int DefaultIntro = 443404335;

        public const string DefaultOwnIntros = "459725398,440978154,440793415,440978850,462141918";

        public const int DefaultResolution = 1080;

        public static Plugin Instance { get; private set; }

        public static IApplicationPaths ApplicationPaths { get; private set; }

        public static ILibraryManager LibraryManager { get; private set; }

        public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer, ILibraryManager libraryManager)
            : base(applicationPaths, xmlSerializer)
        {
            Instance = this;

            ApplicationPaths = applicationPaths;
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
