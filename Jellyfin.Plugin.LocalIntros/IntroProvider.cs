
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Jellyfin.Plugin.LocalIntros.Configuration;
using Jellyfin.Data.Entities;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Entities.Movies;
using Microsoft.Extensions.Logging;


namespace Jellyfin.Plugin.LocalIntros;
public class IntroProvider : IIntroProvider
{
    private readonly ILogger<IntroProvider> logger;

    public IntroProvider(ILoggerFactory loggerFactory)
    {
        logger = loggerFactory.CreateLogger<IntroProvider>();
    }
    
    public string Name { get; } = "Intros";

    public Task<IEnumerable<IntroInfo>> GetIntros(BaseItem item, User user)
    {
        try
        {
            
            if (LocalIntrosPlugin.Instance.Configuration.Local != string.Empty)
            {
                logger.LogTrace("Local Config Detected, retrieving local intros.");
                return Task.FromResult(Local(item));
            }
            else 
            {
                logger.LogError("No Local Config Detected, retrieving library intros.");
                return Task.FromResult(Enumerable.Empty<IntroInfo>());
            }
        
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error retrieving intros");
            return Task.FromResult(Enumerable.Empty<IntroInfo>());
        }
    }
    
    private readonly CookieContainer _cookieContainer = new CookieContainer();

    private readonly Random _random = new Random();

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
        if (!File.Exists(introsPath) && !Directory.Exists(introsPath))
        {
            throw new Exception("No intros found in local path");
        }
        var location = File.GetAttributes(introsPath);

        var libraryResults = RetrieveIntroLibrary();

        if (!libraryResults.Any())
        {
            throw new Exception("No intros found in library");
        }

        var (tags, genres, studios, now) = GetCriteriaList(item);

        var validTagIntros = LocalIntrosPlugin.Instance.Configuration.TagIntros.Where(t => tags.Any(x => x.Equals(t.TagName, StringComparison.OrdinalIgnoreCase)));
        var validGenreIntros = LocalIntrosPlugin.Instance.Configuration.GenreIntros.Where(g => genres.Any(x => x.Equals(g.GenreName, StringComparison.OrdinalIgnoreCase)));
        var validStudioIntros = LocalIntrosPlugin.Instance.Configuration.StudioIntros.Where(s => studios.Any(x => x.Equals(s.StudioName, StringComparison.OrdinalIgnoreCase)));
        var validDateIntros = LocalIntrosPlugin.Instance.Configuration.CurrentDateIntros.Where(d => now.Date <= d.DateEnd && now.Date >= d.DateStart);
        
        FancyList<ISpecialIntro> selectableIntros = new FancyList<ISpecialIntro>();
        
        
        selectableIntros += validTagIntros;
        selectableIntros += validGenreIntros;
        selectableIntros += validStudioIntros;
        selectableIntros += validDateIntros;
        

        FancyList<Guid> randomIntros = new();

        if (selectableIntros.Any())
        {
            logger.LogInformation($"Selecting intros based on criteria, {selectableIntros.Count} intros found");
            
            var highestPrev = selectableIntros.Max(i => i.Precedence);

            var selectedIntros = selectableIntros.Where(i => i.Precedence == highestPrev);
            
            var maxNum = selectableIntros.Sum(i => i.Prevalence);

            var minNum = 0;

            var index = _random.Next(minNum, maxNum);
            
            logger.LogInformation($"Selecting intro from {minNum} to {maxNum}, selected index: {index}");
            
            foreach (var intro in selectedIntros)
            {
                if (index < intro.Prevalence)
                {
                    logger.LogInformation($"Selected intro: {intro.IntroId}");
                    randomIntros += intro.IntroId;
                    break;
                }
                else
                {
                    index -= intro.Prevalence;
                }
            }
            if (randomIntros.Count == 0)
            {
                var selItem = selectedIntros.Last();
                logger.LogInformation($"Selected intro: {selItem.IntroId}");
                randomIntros += selItem.IntroId;
            }
        }
        else 
        {
            randomIntros += LocalIntrosPlugin.Instance.Configuration.DefaultLocalVideos.Distinct();
            
            logger.LogInformation($"Selecting intros based on default, {randomIntros.Count} intros found");
        }
        if (randomIntros.Any())
        {
            var selectedId = randomIntros[_random.Next(randomIntros.Count)];

            logger.LogInformation($"Selected intro ID: {selectedId}");

            if (libraryResults.ContainsKey(selectedId))
            {
                var selectedItem = libraryResults[selectedId];
                
                logger.LogInformation($"Selected intro name: {selectedItem.Name}");
                
                logger.LogInformation($"Selected intro path: {selectedItem.Path}");

                return new []{new IntroInfo
                {
                    Path = selectedItem.Path,
                    ItemId = selectedItem.Id
                }};
            }
            else
            {
                throw new Exception($"Selected intro ID: {selectedId} not found in library");
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
        }).OrderBy(i => i.Name).ToList();
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
