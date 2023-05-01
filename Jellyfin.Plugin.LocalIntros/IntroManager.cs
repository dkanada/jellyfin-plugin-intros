using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using Jellyfin.Plugin.LocalIntros.Configuration;
using Jellyfin.Data.Entities;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Entities.Movies;
// using Jellyfin.Data.Entities.Libraries;

namespace Jellyfin.Plugin.LocalIntros
{
    public class IntroManager
    {
        private readonly CookieContainer _cookieContainer = new CookieContainer();

        private readonly Random _random = new Random();

        public IEnumerable<IntroInfo> Get(BaseItem item, User _)
        {
            // only relevant on first installation
            // if (Plugin.Instance.Configuration.Id == Guid.Empty)
            // {
            //     Console.WriteLine("No intro ID found, generating one now.");
            //     Cache(Plugin.DefaultIntro);
            // }

            if (LocalIntrosPlugin.Instance.Configuration.Local != string.Empty)
            {
                Console.WriteLine("Local Config Detected, retrieving local intros.");
                return Local(item);
            }
            else 
            {
                return Enumerable.Empty<IntroInfo>();
            }
            
        }

        private static string introsPath => LocalIntrosPlugin.Instance.Configuration.Local;

        private (HashSet<string> tags, HashSet<string> genres, HashSet<string> studios, DateTime now) GetCriteriaList(BaseItem item)
        {
            switch (item.GetBaseItemKind())
            {
                case Data.Enums.BaseItemKind.Movie:
                    var movie = item as Movie;
                    return (movie.Tags.ToHashSet(),movie.Genres.ToHashSet(),movie.Studios.ToHashSet(), DateTime.Now);
                case Data.Enums.BaseItemKind.Episode:
                    var episode = item as Episode;
                    var season = episode.Season;
                    var series = episode.Series;
                    return (
                        episode.Tags.Concat(season.Tags).Concat(series.Tags).ToHashSet(),
                        episode.Genres.Concat(season.Genres).Concat(series.Genres).ToHashSet(),
                        episode.Studios.Concat(season.Studios).Concat(series.Studios).ToHashSet(),
                        DateTime.Now
                    );
            }
            var emp = new HashSet<string>();
            return (emp,emp,emp, DateTime.Now);
        }

        private IEnumerable<IntroInfo> Local(BaseItem item)
        {
            var location = File.GetAttributes(introsPath);

            var libraryResults = RetrieveIntroLibrary();

            if (!libraryResults.Any())
            {
                throw new Exception("No intros found in library");
            }

            var (tags, genres, studios, now) = GetCriteriaList(item);

            IEnumerable<ISpecialIntro> selectableIntros = Enumerable.Empty<ISpecialIntro>()
            .Concat(LocalIntrosPlugin.Instance.Configuration.TagIntros.Where(t => tags.Any(x => x.Equals(t.TagName, StringComparison.OrdinalIgnoreCase))))
            .Concat(LocalIntrosPlugin.Instance.Configuration.GenreIntros.Where(g => genres.Any(x => x.Equals(g.GenreName, StringComparison.OrdinalIgnoreCase))))
            .Concat(LocalIntrosPlugin.Instance.Configuration.StudioIntros.Where(s => studios.Any(x => x.Equals(s.StudioName, StringComparison.OrdinalIgnoreCase))))
            .Concat(LocalIntrosPlugin.Instance.Configuration.CurrentDateIntros.Where(d => now.Date <= d.DateEnd && now.Date >= d.DateStart));

            List<Guid> randomIntros;

            if (selectableIntros.Any())
            {
                var highestPrev = selectableIntros.Max(i => i.Precedence);

                var selectedIntros = selectableIntros.Where(i => i.Precedence == highestPrev);

                randomIntros = selectedIntros.SelectMany(i => Enumerable.Repeat(i.IntroId, i.Prevalence)).Distinct().ToList();
            }
            else 
            {
                randomIntros = LocalIntrosPlugin.Instance.Configuration.DefaultLocalVideos.Distinct().ToList();
            }
            if (randomIntros.Any())
            {
                var selectedId = randomIntros[_random.Next(randomIntros.Count)];

                Console.WriteLine($"Selected intro: {selectedId}");

                if (libraryResults.ContainsKey(selectedId))
                {
                    var selectedItem = libraryResults[selectedId];

                    return new []{new IntroInfo
                    {
                        Path = selectedItem.Path,
                        ItemId = selectedItem.Id
                    }};
                }
                else
                {
                    throw new Exception("No intros found in library");
                }
            }
            return Enumerable.Empty<IntroInfo>();
        }

        private void UpdateOptionsConfig(IEnumerable<BaseItem> libraryResults)
        {
            // Dictionary so we can use ContainsKey
            
            if (LocalIntrosPlugin.Instance.Configuration.DefaultLocalVideos.Count + LocalIntrosPlugin.Instance.Configuration.StudioIntros.Count + LocalIntrosPlugin.Instance.Configuration.TagIntros.Count + LocalIntrosPlugin.Instance.Configuration.GenreIntros.Count == 0)
            {
                LocalIntrosPlugin.Instance.Configuration.DefaultLocalVideos.Add(libraryResults.First().Id);
            }
            //And then to the List as we need for saving. (XML can't serialize Dictionaries..)
            LocalIntrosPlugin.Instance.Configuration.DetectedLocalVideos = libraryResults.Select(x => new IntroVideo{
                ItemId = x.Id,
                Name = x.Name
            }).ToList();
            LocalIntrosPlugin.Instance.SaveConfiguration();
        }


        private Dictionary<Guid, BaseItem> RetrieveIntroLibrary() => 
            LocalIntrosPlugin.LibraryManager.GetItemsResult(new InternalItemsQuery
            {
                HasAnyProviderId = new Dictionary<string, string>
                {
                    {"prerolls.video", ""}
                }
            }).Items.ToDictionary(x => x.Id, x => x);

   }
}
