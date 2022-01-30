using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FizzWare.NBuilder;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.Download;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Core.Tv;

namespace NzbDrone.Core.Test.Download
{
    [TestFixture]
    public class DownloadClientProviderFixture : CoreTest<DownloadClientProvider>
    {
        private List<IDownloadClient> _downloadClients;
        private List<DownloadClientStatus> _blockedProviders;
        private int _nextId;

        [SetUp]
        public void SetUp()
        {
            _downloadClients = new List<IDownloadClient>();
            _blockedProviders = new List<DownloadClientStatus>();
            _nextId = 1;

            Mocker.GetMock<IDownloadClientFactory>()
                  .Setup(v => v.GetAvailableProviders())
                  .Returns(_downloadClients);

            Mocker.GetMock<IDownloadClientStatusService>()
                  .Setup(v => v.GetBlockedProviders())
                  .Returns(_blockedProviders);
        }

        private RemoteEpisode CreateRemoteEpisode(DownloadProtocol downloadProtocol, SeriesTypes seriesType = SeriesTypes.Standard)
        {
            return new RemoteEpisode()
            {
                Release = new ReleaseInfo()
                {
                    DownloadProtocol = downloadProtocol,
                },
                Series = new Series()
                {
                    SeriesType = seriesType,
                }
            };
        }

        private Mock<IDownloadClient> WithUsenetClient(int priority = 0, HashSet<SeriesTypes> seriesTypes = null)
        {
            var mock = new Mock<IDownloadClient>(MockBehavior.Default);
            mock.SetupGet(s => s.Definition)
                .Returns(Builder<DownloadClientDefinition>
                    .CreateNew()
                    .With(v => v.Id = _nextId++)
                    .With(v => v.Priority = priority)
                    .With(v => v.SeriesTypes = seriesTypes ?? new HashSet<SeriesTypes>())
                    .Build());

            _downloadClients.Add(mock.Object);

            mock.SetupGet(v => v.Protocol).Returns(DownloadProtocol.Usenet);

            return mock;
        }

        private Mock<IDownloadClient> WithTorrentClient(int priority = 0, HashSet<SeriesTypes> seriesTypes = null)
        {
            var mock = new Mock<IDownloadClient>(MockBehavior.Default);
            mock.SetupGet(s => s.Definition)
                .Returns(Builder<DownloadClientDefinition>
                    .CreateNew()
                    .With(v => v.Id = _nextId++)
                    .With(v => v.Priority = priority)
                    .With(v => v.SeriesTypes = seriesTypes ?? new HashSet<SeriesTypes>())
                    .Build());

            _downloadClients.Add(mock.Object);

            mock.SetupGet(v => v.Protocol).Returns(DownloadProtocol.Torrent);

            return mock;
        }

        private void GivenBlockedClient(int id)
        {
            _blockedProviders.Add(new DownloadClientStatus
            {
                ProviderId = id,
                DisabledTill = DateTime.UtcNow.AddHours(3)
            });
        }

        [Test]
        public void should_roundrobin_over_usenet_client()
        {
            WithUsenetClient();
            WithUsenetClient();
            WithUsenetClient();
            WithTorrentClient();

            var remoteEpisode = CreateRemoteEpisode(DownloadProtocol.Usenet);
            var client1 = Subject.GetDownloadClient(remoteEpisode);
            var client2 = Subject.GetDownloadClient(remoteEpisode);
            var client3 = Subject.GetDownloadClient(remoteEpisode);
            var client4 = Subject.GetDownloadClient(remoteEpisode);
            var client5 = Subject.GetDownloadClient(remoteEpisode);

            client1.Definition.Id.Should().Be(1);
            client2.Definition.Id.Should().Be(2);
            client3.Definition.Id.Should().Be(3);
            client4.Definition.Id.Should().Be(1);
            client5.Definition.Id.Should().Be(2);
        }

        [Test]
        public void should_roundrobin_over_torrent_client()
        {
            WithUsenetClient();
            WithTorrentClient();
            WithTorrentClient();
            WithTorrentClient();

            var remoteEpisode = CreateRemoteEpisode(DownloadProtocol.Torrent);
            var client1 = Subject.GetDownloadClient(remoteEpisode);
            var client2 = Subject.GetDownloadClient(remoteEpisode);
            var client3 = Subject.GetDownloadClient(remoteEpisode);
            var client4 = Subject.GetDownloadClient(remoteEpisode);
            var client5 = Subject.GetDownloadClient(remoteEpisode);

            client1.Definition.Id.Should().Be(2);
            client2.Definition.Id.Should().Be(3);
            client3.Definition.Id.Should().Be(4);
            client4.Definition.Id.Should().Be(2);
            client5.Definition.Id.Should().Be(3);
        }

        [Test]
        public void should_roundrobin_over_protocol_separately()
        {
            WithUsenetClient();
            WithTorrentClient();
            WithTorrentClient();

            var remoteEpisodeUsenet = CreateRemoteEpisode(DownloadProtocol.Usenet);
            var remoteEpisodeTorrent = CreateRemoteEpisode(DownloadProtocol.Torrent);
            var client1 = Subject.GetDownloadClient(remoteEpisodeUsenet);
            var client2 = Subject.GetDownloadClient(remoteEpisodeTorrent);
            var client3 = Subject.GetDownloadClient(remoteEpisodeTorrent);
            var client4 = Subject.GetDownloadClient(remoteEpisodeTorrent);

            client1.Definition.Id.Should().Be(1);
            client2.Definition.Id.Should().Be(2);
            client3.Definition.Id.Should().Be(3);
            client4.Definition.Id.Should().Be(2);
        }

        [Test]
        public void should_skip_blocked_torrent_client()
        {
            WithUsenetClient();
            WithTorrentClient();
            WithTorrentClient();
            WithTorrentClient();

            GivenBlockedClient(3);

            var remoteEpisode = CreateRemoteEpisode(DownloadProtocol.Torrent);
            var client1 = Subject.GetDownloadClient(remoteEpisode);
            var client2 = Subject.GetDownloadClient(remoteEpisode);
            var client3 = Subject.GetDownloadClient(remoteEpisode);
            var client4 = Subject.GetDownloadClient(remoteEpisode);

            client1.Definition.Id.Should().Be(2);
            client2.Definition.Id.Should().Be(4);
            client3.Definition.Id.Should().Be(2);
            client4.Definition.Id.Should().Be(4);
        }

        [Test]
        public void should_not_skip_blocked_torrent_client_if_all_blocked()
        {
            WithUsenetClient();
            WithTorrentClient();
            WithTorrentClient();
            WithTorrentClient();

            GivenBlockedClient(2);
            GivenBlockedClient(3);
            GivenBlockedClient(4);

            var remoteEpisode = CreateRemoteEpisode(DownloadProtocol.Torrent);
            var client1 = Subject.GetDownloadClient(remoteEpisode);
            var client2 = Subject.GetDownloadClient(remoteEpisode);
            var client3 = Subject.GetDownloadClient(remoteEpisode);
            var client4 = Subject.GetDownloadClient(remoteEpisode);

            client1.Definition.Id.Should().Be(2);
            client2.Definition.Id.Should().Be(3);
            client3.Definition.Id.Should().Be(4);
            client4.Definition.Id.Should().Be(2);
        }

        [Test]
        public void should_skip_secondary_prio_torrent_client()
        {
            WithUsenetClient();
            WithTorrentClient(2);
            WithTorrentClient();
            WithTorrentClient();

            var remoteEpisode = CreateRemoteEpisode(DownloadProtocol.Torrent);
            var client1 = Subject.GetDownloadClient(remoteEpisode);
            var client2 = Subject.GetDownloadClient(remoteEpisode);
            var client3 = Subject.GetDownloadClient(remoteEpisode);
            var client4 = Subject.GetDownloadClient(remoteEpisode);

            client1.Definition.Id.Should().Be(3);
            client2.Definition.Id.Should().Be(4);
            client3.Definition.Id.Should().Be(3);
            client4.Definition.Id.Should().Be(4);
        }

        [Test]
        public void should_not_skip_secondary_prio_torrent_client_if_primary_blocked()
        {
            WithUsenetClient();
            WithTorrentClient(2);
            WithTorrentClient(2);
            WithTorrentClient();

            GivenBlockedClient(4);

            var remoteEpisode = CreateRemoteEpisode(DownloadProtocol.Torrent);
            var client1 = Subject.GetDownloadClient(remoteEpisode);
            var client2 = Subject.GetDownloadClient(remoteEpisode);
            var client3 = Subject.GetDownloadClient(remoteEpisode);
            var client4 = Subject.GetDownloadClient(remoteEpisode);

            client1.Definition.Id.Should().Be(2);
            client2.Definition.Id.Should().Be(3);
            client3.Definition.Id.Should().Be(2);
            client4.Definition.Id.Should().Be(3);
        }

        [Test]
        public void should_filter_clients_by_series_types()
        {
            WithTorrentClient(0, new HashSet<SeriesTypes>() { SeriesTypes.Standard });
            WithTorrentClient(0, new HashSet<SeriesTypes>() { SeriesTypes.Anime });
            WithTorrentClient(0, new HashSet<SeriesTypes>() { SeriesTypes.Daily });

            GivenBlockedClient(4);

            var standardClient = Subject.GetDownloadClient(CreateRemoteEpisode(DownloadProtocol.Torrent, SeriesTypes.Standard));
            var animeClient = Subject.GetDownloadClient(CreateRemoteEpisode(DownloadProtocol.Torrent, SeriesTypes.Anime));
            var dailyClient = Subject.GetDownloadClient(CreateRemoteEpisode(DownloadProtocol.Torrent, SeriesTypes.Daily));

            standardClient.Definition.Id.Should().Be(1);
            animeClient.Definition.Id.Should().Be(2);
            dailyClient.Definition.Id.Should().Be(3);
        }

        [Test]
        public void should_skip_download_clients_without_series_types_if_better_available()
        {
            WithTorrentClient(0);
            WithTorrentClient(0, new HashSet<SeriesTypes>() { SeriesTypes.Anime });
            WithTorrentClient(0, new HashSet<SeriesTypes>() { SeriesTypes.Anime, SeriesTypes.Daily });

            GivenBlockedClient(4);

            var client1 = Subject.GetDownloadClient(CreateRemoteEpisode(DownloadProtocol.Torrent, SeriesTypes.Anime));
            var client2 = Subject.GetDownloadClient(CreateRemoteEpisode(DownloadProtocol.Torrent, SeriesTypes.Daily));
            var client3 = Subject.GetDownloadClient(CreateRemoteEpisode(DownloadProtocol.Torrent, SeriesTypes.Daily));
            var client4 = Subject.GetDownloadClient(CreateRemoteEpisode(DownloadProtocol.Torrent, SeriesTypes.Anime));
            var client5 = Subject.GetDownloadClient(CreateRemoteEpisode(DownloadProtocol.Torrent, SeriesTypes.Anime));
            var client6 = Subject.GetDownloadClient(CreateRemoteEpisode(DownloadProtocol.Torrent, SeriesTypes.Standard));

            client1.Definition.Id.Should().Be(2);
            client2.Definition.Id.Should().Be(3);
            client3.Definition.Id.Should().Be(3);
            client4.Definition.Id.Should().Be(2);
            client5.Definition.Id.Should().Be(3);
            client6.Definition.Id.Should().Be(1);
        }
    }
}
