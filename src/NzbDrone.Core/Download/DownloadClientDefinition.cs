using NzbDrone.Core.Indexers;
using NzbDrone.Core.ThingiProvider;
using NzbDrone.Core.Tv;
using System.Collections.Generic;

namespace NzbDrone.Core.Download
{
    public class DownloadClientDefinition : ProviderDefinition
    {
        public DownloadProtocol Protocol { get; set; }
        public int Priority { get; set; } = 1;

        public bool RemoveCompletedDownloads { get; set; } = true;
        public bool RemoveFailedDownloads { get; set; } = true;
        public HashSet<SeriesTypes> SeriesTypes { get; set; } = new HashSet<SeriesTypes>();

    }
}
