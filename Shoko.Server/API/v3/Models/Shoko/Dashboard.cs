using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Shoko.Commons.Extensions;
using Shoko.Models.Enums;
using Shoko.Models.Server;
using Shoko.Server.API.v3.Models.Common;
using Shoko.Server.Models;

namespace Shoko.Server.API.v3.Models.Shoko
{
    public static class Dashboard
    {
        public class CollectionStats
        {
            /// <summary>
            /// Number of Files in the collection (visible to the current user)
            /// </summary>
            public int FileCount { get; set; }

            /// <summary>
            /// Number of Series in the Collection (visible to the current user)
            /// </summary>
            public int SeriesCount { get; set; }

            /// <summary>
            /// The number of Groups in the Collection (visible to the current user)
            /// </summary>
            public int GroupCount { get; set; }

            /// <summary>
            /// Total amount of space the collection takes (of what's visible to the current user)
            /// </summary>
            public long FileSize { get; set; }

            /// <summary>
            /// Number of Series Completely Watched
            /// </summary>
            public int FinishedSeries { get; set; }

            /// <summary>
            /// Number of Episodes Watched
            /// </summary>
            public int WatchedEpisodes { get; set; }

            /// <summary>
            /// Watched Hours, rounded to one place
            /// </summary>
            public decimal WatchedHours { get; set; }

            /// <summary>
            /// The percentage of files that are either duplicates or belong to the same episode
            /// </summary>
            public decimal PercentDuplicate { get; set; }

            /// <summary>
            /// The Number of missing episodes, regardless of where they are from or available
            /// </summary>
            public int MissingEpisodes { get; set; }

            /// <summary>
            /// The number of missing episodes from groups we are collecting. This should not be used as a rule, as it's not very reliable
            /// </summary>
            public int MissingEpisodesCollecting { get; set; }

            /// <summary>
            /// Number of Unrecognized Files
            /// </summary>
            public int UnrecognizedFiles { get; set; }

            /// <summary>
            /// The number of series missing both the TvDB and MovieDB Links
            /// </summary>
            public int SeriesWithMissingLinks { get; set; }

            /// <summary>
            /// The number of Episodes with more than one File (not marked as a variation)
            /// </summary>
            public int EpisodesWithMultipleFiles { get; set; }

            /// <summary>
            /// The number of files that exist in more than one location
            /// </summary>
            public int FilesWithDuplicateLocations { get; set; }
        }

        public class SeriesSummary
        {
            /// <summary>
            /// The number of normal Series
            /// </summary>
            public int Series { get; set; }

            /// <summary>
            /// The Number of OVAs
            /// </summary>
            public int OVA { get; set; }

            /// <summary>
            /// The Number of Movies
            /// </summary>
            public int Movie { get; set; }

            /// <summary>
            /// The Number of TV Specials
            /// </summary>
            public int Special { get; set; }

            /// <summary>
            /// ONAs and the like, it's more of a new concept
            /// </summary>
            public int Web { get; set; }

            /// <summary>
            /// Things marked on AniDB as Other, different from None
            /// </summary>
            public int Other { get; set; }

            /// <summary>
            /// Series that don't have AniDB Records. This is very bad, and usually means there was an error in the import process. It can also happen if the API is hit at just the right time.
            /// </summary>
            public int None { get; set; }
        }

        /// <summary>
        /// Episode details for displaying on the dashboard.
        /// </summary>
        public class EpisodeDetails
        {
            public EpisodeDetails(AniDB_Episode episode, SVR_AniDB_Anime anime, SVR_AnimeSeries series = null)
            {
                ID = episode.EpisodeID;
                Title = Episode.GetEpisodeTitle(episode.EpisodeID);
                Number = episode.EpisodeNumber;
                Type = Episode.MapAniDBEpisodeType(episode.GetEpisodeTypeEnum());
                AirDate = (DateTime)episode.GetAirDateAsDate();
                InCollection = series != null;
                SeriesID = anime.AnimeID;
                SeriesTitle = series?.GetSeriesName() ?? anime.PreferredTitle;
                SeriesPoster = Series.GetDefaultImage(anime.AnimeID, ImageSizeType.Poster) ?? Series.GetAniDBPoster(anime.AnimeID);
            }

            /// <summary>
            /// AniDB Episode ID.
            /// </summary>
            public int ID { get; set; }

            /// <summary>
            /// Episode title.
            /// </summary>
            public string Title { get; set; }

            /// <summary>
            /// Episode number.
            /// </summary>
            public int Number { get; set; }

            /// <summary>
            /// Episode type.
            /// </summary>
            [JsonConverter(typeof(StringEnumConverter))]
            public EpisodeType Type { get; set; }

            /// <summary>
            /// Air Date.
            /// </summary>
            /// <value></value>
            public DateTime AirDate { get; set; }

            /// <summary>
            /// True if the series is available in the user's collection.
            /// </summary>
            /// <value></value>
            public bool InCollection { get; set; }

            /// <summary>
            /// AniDB Series ID.
            /// </summary>
            /// <value></value>
            public int SeriesID { get; set; }

            /// <summary>
            /// Series title.
            /// </summary>
            public string SeriesTitle { get; set; }

            /// <summary>
            /// Series poster.
            /// </summary>
            public Image SeriesPoster { get; set; }
        }
    }
}