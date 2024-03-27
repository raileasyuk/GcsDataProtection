using Google.Cloud.Storage.V1;

using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using System;

namespace Raileasy.DataProtection.Gcs;

public static class DataProtectionBuilderExtensions
{
    public static IDataProtectionBuilder PersistKeysToGcs(this IDataProtectionBuilder builder,
        GcsDataProtectionConfiguration gcsDataProtectionConfiguration)
    {
        builder.Services.AddSingleton(Options.Create(gcsDataProtectionConfiguration));
        builder.Services.AddSingleton(StorageClient.Create());
        builder.Services.AddSingleton<GcsXmlRepository>();
        builder.Services.AddSingleton<IConfigureOptions<KeyManagementOptions>>(services =>
        {
            var repository = services.GetService<GcsXmlRepository>() ?? throw new InvalidOperationException("GcsXmlRepository is not registered.");
            return new ConfigureOptions<KeyManagementOptions>(options => options.XmlRepository = repository);
        });
        return builder;
    }
}
