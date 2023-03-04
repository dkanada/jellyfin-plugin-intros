using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Jellyfin.Data.Entities;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;

namespace Jellyfin.Plugin.Intros
{
    public class IntroProvider : IIntroProvider
    {
        public string Name { get; } = "Intros";

        public Task<IEnumerable<IntroInfo>> GetIntros(BaseItem item, User user)
        {
            var showIntrosOnMovies = Plugin.Instance.Configuration.ShowIntrosOnMovies;
            var showIntrosOnEpisodes = Plugin.Instance.Configuration.ShowIntrosOnEpisodes;

            return Task.FromResult(item switch {
                Movie when showIntrosOnMovies is false => Enumerable.Empty<IntroInfo>(),
                Episode when showIntrosOnEpisodes is false => Enumerable.Empty<IntroInfo>(),
                _ => new IntroManager().Get()
            });
        }

        public IEnumerable<string> GetAllIntroFiles()
        {
            // not implemented on server
            return Enumerable.Empty<string>();
        }
    }
}
