using System;
using System.Data;
using System.Linq;
using FluentMigrator;
using NzbDrone.Common.Serializer;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(163)]
    public class add_series_types_to_downloadclient : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Alter.Table("DownloadClients")
                 .AddColumn("SeriesTypes").AsString().NotNullable().WithDefaultValue("[]");
        }
    }
}
