using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Shoko.Commons.Extensions;
using Shoko.Models.Enums;
using Shoko.Server.API.v3.Models.Common;
using Shoko.Server.Models;
using Shoko.Server.Repositories;
using File = Shoko.Server.API.v3.Models.Shoko.File;
using FileSource = Shoko.Server.API.v3.Models.Shoko.FileSource;
using GroupSizes = Shoko.Server.API.v3.Models.Shoko.GroupSizes;
using Series = Shoko.Server.API.v3.Models.Shoko.Series;
using SeriesSizes = Shoko.Server.API.v3.Models.Shoko.SeriesSizes;
using SeriesType = Shoko.Server.API.v3.Models.Shoko.SeriesType;

namespace Shoko.Server.API.v3.Helpers;

public static class ModelHelper
{
    public static ListResult<T> ToListResult<T>(this IEnumerable<T> enumerable)
    {
        var list = enumerable is IReadOnlyList<T> l ? l : enumerable.ToList();
        return new ListResult<T>
        {
            Total = list.Count,
            List = list
        };
    }

    public static ListResult<T> ToListResult<T>(this IEnumerable<T> enumerable, int page, int pageSize)
    {
        var list = enumerable is IReadOnlyList<T> l ? l : enumerable.ToList();
        if (pageSize <= 0)
        {
            return new ListResult<T>
            {
                Total = list.Count,
                List = list
            };
        }

        return new ListResult<T>
        {
            Total = list.Count,
            List = list
                .Skip(pageSize * (page - 1))
                .Take(pageSize)
                .ToList()
        };
    }

    public static ListResult<U> ToListResult<T, U>(this IEnumerable<T> enumerable, Func<T, U> mapper, int page,
        int pageSize)
    {
        var list = enumerable is IReadOnlyList<T> l ? l : enumerable.ToList();
        if (pageSize <= 0)
        {
            return new ListResult<U>
            {
                Total = list.Count,
                List = list
                    .Select(mapper)
                    .ToList()
            };
        }

        return new ListResult<U>
        {
            Total = list.Count,
            List = list
                .Skip(pageSize * (page - 1))
                .Take(pageSize)
                .Select(mapper)
                .ToList()
        };
    }

    public static (int, EpisodeType?, string) GetEpisodeNumberAndTypeFromInput(string input)
    {
        EpisodeType? episodeType = null;
        if (!int.TryParse(input, out var episodeNumber))
        {
            var maybeType = input[0];
            var maybeRangeStart = input.Substring(1);
            if (!int.TryParse(maybeRangeStart, out episodeNumber))
            {
                return (0, null, "Unable to parse an int from `{VariableName}`");
            }

            episodeType = maybeType switch
            {
                'S' => EpisodeType.Special,
                'C' => EpisodeType.Credits,
                'T' => EpisodeType.Trailer,
                'P' => EpisodeType.Parody,
                'O' => EpisodeType.Other,
                'E' => EpisodeType.Episode,
                _ => null
            };
            if (!episodeType.HasValue)
            {
                return (0, null, $"Unknown episode type '{maybeType}' number in `{{VariableName}}`.");
            }
        }

        return (episodeNumber, episodeType, null);
    }

    public static int GetTotalEpisodesForType(List<SVR_AnimeEpisode> episodeList, EpisodeType episodeType)
    {
        return episodeList
            .Select(episode => episode.AniDB_Episode)
            .Where(anidbEpisode => anidbEpisode != null && (EpisodeType)anidbEpisode.EpisodeType == episodeType)
            .Count();
    }

    public static string ToDataURL(byte[] byteArray, string contentType, string fieldName = "ByteArrayToDataUrl", ModelStateDictionary modelState = null)
    {
        if (byteArray == null || string.IsNullOrEmpty(contentType))
        {
            modelState?.AddModelError(fieldName, $"Invalid byte array or content type for field '{fieldName}'.");
            return null;
        }

        try
        {
            string base64 = Convert.ToBase64String(byteArray);
            return $"data:{contentType};base64,{base64}";
        }
        catch (Exception)
        {
            modelState?.AddModelError(fieldName, $"Unexpected error when converting byte array to data URL for field '{fieldName}'.");
            return null;
        }
    }

    public static (byte[] byteArray, string contentType) FromDataURL(string dataUrl, string fieldName = "DataUrlToByteArray", ModelStateDictionary modelState = null)
    {
        var parts = dataUrl.Split(new[] { ":", ";", "," }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 4 || parts[0] != "data")
        {
            modelState?.AddModelError(fieldName, $"Invalid data URL format for field '{fieldName}'.");
            return (null, null);
        }

        try
        {
            var byteArray = Convert.FromBase64String(parts[3]);
            return (byteArray, parts[1]);
        }
        catch (FormatException)
        {
            modelState?.AddModelError(fieldName, $"Base64 data is not in a correct format for field '{fieldName}'.");
            return (null, null);
        }
        catch (Exception)
        {
            modelState?.AddModelError(fieldName, $"Unexpected error when converting data URL to byte array for field '{fieldName}'.");
            return (null, null);
        }
    }

    public static SeriesSizes GenerateSeriesSizes(List<SVR_AnimeEpisode> episodeList, int userID)
    {
        var now = DateTime.Now;
        var sizes = new SeriesSizes();
        var fileSet = new HashSet<int>();
        foreach (var episode in episodeList)
        {
            var anidbEpisode = episode.AniDB_Episode;
            var fileList = episode.GetVideoLocals();
            var isLocal = fileList.Count > 0;
            var isWatched = (episode.GetUserRecord(userID)?.WatchedCount ?? 0) > 0;
            foreach (var file in fileList)
            {
                // Only iterate the same file once.
                if (!fileSet.Add(file.VideoLocalID))
                    continue;

                var anidbFile = file.GetAniDBFile();
                if (anidbFile == null)
                {
                    sizes.FileSources.Unknown++;
                    continue;
                }

                switch (File.ParseFileSource(anidbFile.File_Source))
                {
                    case FileSource.Unknown:
                        sizes.FileSources.Unknown++;
                        break;
                    case FileSource.Other:
                        sizes.FileSources.Other++;
                        break;
                    case FileSource.TV:
                        sizes.FileSources.TV++;
                        break;
                    case FileSource.DVD:
                        sizes.FileSources.DVD++;
                        break;
                    case FileSource.BluRay:
                        sizes.FileSources.BluRay++;
                        break;
                    case FileSource.Web:
                        sizes.FileSources.Web++;
                        break;
                    case FileSource.VHS:
                        sizes.FileSources.VHS++;
                        break;
                    case FileSource.VCD:
                        sizes.FileSources.VCD++;
                        break;
                    case FileSource.LaserDisc:
                        sizes.FileSources.LaserDisc++;
                        break;
                    case FileSource.Camera:
                        sizes.FileSources.Camera++;
                        break;
                }
            }

            if (episode.IsHidden)
            {
                sizes.Hidden++;
                continue;
            }

            var airDate = anidbEpisode.GetAirDateAsDate();
            if (anidbEpisode == null)
            {
                sizes.Total.Unknown++;
                if (isLocal)
                {
                    sizes.Local.Unknown++;
                }

                if (isWatched)
                {
                    sizes.Watched.Unknown++;
                }

                continue;
            }

            switch ((EpisodeType)anidbEpisode.EpisodeType)
            {
                case EpisodeType.Episode:
                    sizes.Total.Episodes++;
                    if (isLocal)
                    {
                        sizes.Local.Episodes++;
                    }
                    else if (airDate.HasValue && airDate.Value < now)
                    {
                        sizes.Missing.Episodes++;
                    }

                    if (isWatched)
                    {
                        sizes.Watched.Episodes++;
                    }

                    break;
                case EpisodeType.Credits:
                    sizes.Total.Credits++;
                    if (isLocal)
                    {
                        sizes.Local.Credits++;
                    }

                    if (isWatched)
                    {
                        sizes.Watched.Credits++;
                    }

                    break;
                case EpisodeType.Special:
                    sizes.Total.Specials++;
                    if (isLocal)
                    {
                        sizes.Local.Specials++;
                    }
                    else if (airDate.HasValue && airDate.Value < now)
                    {
                        sizes.Missing.Specials++;
                    }

                    if (isWatched)
                    {
                        sizes.Watched.Specials++;
                    }

                    break;
                case EpisodeType.Trailer:
                    sizes.Total.Trailers++;
                    if (isLocal)
                    {
                        sizes.Local.Trailers++;
                    }

                    if (isWatched)
                    {
                        sizes.Watched.Trailers++;
                    }

                    break;
                case EpisodeType.Parody:
                    sizes.Total.Parodies++;
                    if (isLocal)
                    {
                        sizes.Local.Parodies++;
                    }

                    if (isWatched)
                    {
                        sizes.Watched.Parodies++;
                    }

                    break;
                case EpisodeType.Other:
                    sizes.Total.Others++;
                    if (isLocal)
                    {
                        sizes.Local.Others++;
                    }

                    if (isWatched)
                    {
                        sizes.Watched.Others++;
                    }

                    break;
            }
        }

        return sizes;
    }

    public static GroupSizes GenerateGroupSizes(List<SVR_AnimeSeries> seriesList, List<SVR_AnimeEpisode> episodeList,
        int subGroups, int userID)
    {
        var sizes = new GroupSizes(GenerateSeriesSizes(episodeList, userID));
        foreach (var series in seriesList)
        {
            var anime = series.GetAnime();
            switch (SeriesFactory.GetAniDBSeriesType(anime?.AnimeType))
            {
                case SeriesType.Unknown:
                    sizes.SeriesTypes.Unknown++;
                    break;
                case SeriesType.Other:
                    sizes.SeriesTypes.Other++;
                    break;
                case SeriesType.TV:
                    sizes.SeriesTypes.TV++;
                    break;
                case SeriesType.TVSpecial:
                    sizes.SeriesTypes.TVSpecial++;
                    break;
                case SeriesType.Web:
                    sizes.SeriesTypes.Web++;
                    break;
                case SeriesType.Movie:
                    sizes.SeriesTypes.Movie++;
                    break;
                case SeriesType.OVA:
                    sizes.SeriesTypes.OVA++;
                    break;
            }
        }

        sizes.SubGroups = subGroups;
        return sizes;
    }

    public static ListResult<File> FilterFiles(IEnumerable<SVR_VideoLocal> input, SVR_JMMUser user, int pageSize, int page, FileNonDefaultIncludeType[] include,
        FileExcludeTypes[] exclude, FileIncludeOnlyType[] include_only, List<string> sortOrder, HashSet<DataSource> includeDataFrom, bool skipSort = false)
    {
        include ??= Array.Empty<FileNonDefaultIncludeType>();
        exclude ??= Array.Empty<FileExcludeTypes>();
        include_only ??= Array.Empty<FileIncludeOnlyType>();

        var includeLocations = exclude.Contains(FileExcludeTypes.Duplicates) ||
                               (sortOrder?.Any(criteria => criteria.Contains(File.FileSortCriteria.DuplicateCount.ToString())) ?? false);
        var includeUserRecord = exclude.Contains(FileExcludeTypes.Watched) || (sortOrder?.Any(criteria =>
            criteria.Contains(File.FileSortCriteria.ViewedAt.ToString()) || criteria.Contains(File.FileSortCriteria.WatchedAt.ToString())) ?? false);
        var enumerable = input
            .Select(video => (
                Video: video,
                BestLocation: video.GetBestVideoLocalPlace(),
                Locations: includeLocations ? video.Places : null,
                UserRecord: includeUserRecord ? video.GetUserRecord(user.JMMUserID) : null
            ))
            .Where(tuple =>
            {
                var (video, _, locations, userRecord) = tuple;
                var xrefs = video.EpisodeCrossRefs;
                var isAnimeAllowed = xrefs
                    .Select(xref => xref.AnimeID)
                    .Distinct()
                    .Select(anidbID => RepoFactory.AniDB_Anime.GetByAnimeID(anidbID))
                    .Where(anime => anime != null)
                    .All(user.AllowedAnime);
                if (!isAnimeAllowed)
                    return false;

                if (!include.Contains(FileNonDefaultIncludeType.Ignored) && video.IsIgnored) return false;
                if (include_only.Contains(FileIncludeOnlyType.Ignored) && !video.IsIgnored) return false;

                if (exclude.Contains(FileExcludeTypes.Duplicates) && locations.Count > 1) return false;
                if (include_only.Contains(FileIncludeOnlyType.Duplicates) && locations.Count <= 1) return false;

                if (exclude.Contains(FileExcludeTypes.Unrecognized) && xrefs.Count == 0) return false;
                if (include_only.Contains(FileIncludeOnlyType.Unrecognized) && xrefs.Count > 0 && xrefs.Any(x =>
                        RepoFactory.AnimeSeries.GetByAnimeID(x.AnimeID) != null &&
                        RepoFactory.AnimeEpisode.GetByAniDBEpisodeID(x.EpisodeID) != null)) return false;

                if (exclude.Contains(FileExcludeTypes.ManualLinks) && xrefs.Count > 0 &&
                    xrefs.Any(xref => xref.CrossRefSource != (int)CrossRefSource.AniDB)) return false;
                if (include_only.Contains(FileIncludeOnlyType.ManualLinks) &&
                    (xrefs.Count == 0 || xrefs.Any(xref => xref.CrossRefSource == (int)CrossRefSource.AniDB))) return false;

                if (exclude.Contains(FileExcludeTypes.Watched) && userRecord?.WatchedDate != null) return false;
                if (include_only.Contains(FileIncludeOnlyType.Watched) && userRecord?.WatchedDate == null) return false;

                return true;
            });

        // Sorting.
        if (sortOrder != null && sortOrder.Count > 0)
            enumerable = File.OrderBy(enumerable, sortOrder);
        else if (skipSort)
            enumerable = File.OrderBy(enumerable, new()
            {
                // First sort by import folder from A-Z.
                File.FileSortCriteria.ImportFolderName.ToString(),
                // Then by the relative path inside the import folder, from A-Z.
                File.FileSortCriteria.RelativePath.ToString(),
            });

        // Skip and limit.
        return enumerable.ToListResult(
            tuple => new File(tuple.UserRecord, tuple.Video, include.Contains(FileNonDefaultIncludeType.XRefs), includeDataFrom,
                include.Contains(FileNonDefaultIncludeType.MediaInfo), include.Contains(FileNonDefaultIncludeType.AbsolutePaths)), page, pageSize);
    }
}
