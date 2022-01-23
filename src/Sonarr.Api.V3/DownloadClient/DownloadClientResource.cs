using NzbDrone.Core.Download;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Tv;
using System.Collections.Generic;

namespace Sonarr.Api.V3.DownloadClient
{
    public class DownloadClientResource : ProviderResource
    {
        public bool Enable { get; set; }
        public DownloadProtocol Protocol { get; set; }
        public int Priority { get; set; }
        public bool RemoveCompletedDownloads { get; set; }
        public bool RemoveFailedDownloads { get; set; }
        public HashSet<SeriesTypes> SeriesTypes { get; set; }
    }

    public class DownloadClientResourceMapper : ProviderResourceMapper<DownloadClientResource, DownloadClientDefinition>
    {
        public override DownloadClientResource ToResource(DownloadClientDefinition definition)
        {
            if (definition == null) return null;

            var resource = base.ToResource(definition);

            resource.Enable = definition.Enable;
            resource.Protocol = definition.Protocol;
            resource.Priority = definition.Priority;
            resource.RemoveCompletedDownloads = definition.RemoveCompletedDownloads;
            resource.RemoveFailedDownloads = definition.RemoveFailedDownloads;
            resource.SeriesTypes = definition.SeriesTypes;

            return resource;
        }

        public override DownloadClientDefinition ToModel(DownloadClientResource resource)
        {
            if (resource == null) return null;

            var definition = base.ToModel(resource);

            definition.Enable = resource.Enable;
            definition.Protocol = resource.Protocol;
            definition.Priority = resource.Priority;
            definition.RemoveCompletedDownloads = resource.RemoveCompletedDownloads;
            definition.RemoveFailedDownloads = resource.RemoveFailedDownloads;
            definition.SeriesTypes = resource.SeriesTypes;

            return definition;
        }
    }
}