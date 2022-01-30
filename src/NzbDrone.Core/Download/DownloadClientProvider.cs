using System.Linq;
using System.Collections.Generic;
using NzbDrone.Core.Indexers;
using NzbDrone.Common.Cache;
using NLog;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.Download
{
    public interface IProvideDownloadClient
    {
        IDownloadClient GetDownloadClient(RemoteEpisode remoteEpisode);
        IEnumerable<IDownloadClient> GetDownloadClients();
        IDownloadClient Get(int id);
    }

    public class DownloadClientProvider : IProvideDownloadClient
    {
        private readonly Logger _logger;
        private readonly IDownloadClientFactory _downloadClientFactory;
        private readonly IDownloadClientStatusService _downloadClientStatusService;
        private readonly ICached<int> _lastUsedDownloadClient;

        public DownloadClientProvider(IDownloadClientStatusService downloadClientStatusService, IDownloadClientFactory downloadClientFactory, ICacheManager cacheManager, Logger logger)
        {
            _logger = logger;
            _downloadClientFactory = downloadClientFactory;
            _downloadClientStatusService = downloadClientStatusService;
            _lastUsedDownloadClient = cacheManager.GetCache<int>(GetType(), "lastDownloadClientId");
        }

        public IDownloadClient GetDownloadClient(RemoteEpisode remoteEpisode)
        {
            DownloadProtocol downloadProtocol = remoteEpisode.Release.DownloadProtocol;
            var availableProviders = _downloadClientFactory.GetAvailableProviders().Where(v => v.Protocol == downloadProtocol).ToList();

            if (!availableProviders.Any()) return null;

            var blockedProviders = new HashSet<int>(_downloadClientStatusService.GetBlockedProviders().Select(v => v.ProviderId));

            if (blockedProviders.Any())
            {
                var nonBlockedProviders = availableProviders.Where(v => !blockedProviders.Contains(v.Definition.Id)).ToList();

                if (nonBlockedProviders.Any())
                {
                    availableProviders = nonBlockedProviders;
                }
                else
                {
                    _logger.Trace("No non-blocked Download Client available, retrying blocked one.");
                }
            }
            // Divide clients to two pools: those that match the series types of the show, and those that have no series types defined.
            // Clients that have series types selected are preferred, but as a fallback we will also
            // use clients that have no series types.
            List<IDownloadClient> clientsWithSeriesTypes = new List<IDownloadClient>();
            List<IDownloadClient> fallbackClients = new List<IDownloadClient>();
            foreach (var provider in availableProviders)
            {
                var types = (provider.Definition as DownloadClientDefinition).SeriesTypes;
                if (types.Count == 0)
                {
                    fallbackClients.Add(provider);
                } else if (types.Contains(remoteEpisode.Series.SeriesType))
                {
                    clientsWithSeriesTypes.Add(provider);
                }
            }
            // Try selecting from series type pool first, then fallback to other clients.
            return SelectClient(clientsWithSeriesTypes, downloadProtocol) ?? SelectClient(fallbackClients, downloadProtocol);
        }

        public IDownloadClient SelectClient(List<IDownloadClient> candidates, DownloadProtocol downloadProtocol)
        {
            if (candidates.Count == 0)
            {
                return null;
            }
            // Use the first priority clients first
            List<IDownloadClient> availableProviders = candidates.GroupBy(v => (v.Definition as DownloadClientDefinition).Priority)
                .OrderBy(v => v.Key)
                .First()
                .OrderBy(v => v.Definition.Id)
                .ToList();

            var lastId = _lastUsedDownloadClient.Find(downloadProtocol.ToString());

            var provider = availableProviders.FirstOrDefault(v => v.Definition.Id > lastId) ?? availableProviders.First();

            _lastUsedDownloadClient.Set(downloadProtocol.ToString(), provider.Definition.Id);

            return provider;
        }

        public IEnumerable<IDownloadClient> GetDownloadClients()
        {
            return _downloadClientFactory.GetAvailableProviders();
        }

        public IDownloadClient Get(int id)
        {
            return _downloadClientFactory.GetAvailableProviders().Single(d => d.Definition.Id == id);
        }
    }
}
