using Google.Cloud.Storage.V1;

using Microsoft.AspNetCore.DataProtection.Repositories;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace Raileasy.DataProtection.Gcs;

public class GcsXmlRepository : IXmlRepository
{
    private readonly ILogger _logger;
    private readonly StorageClient _storageClient;
    private readonly string _bucketName;
    private readonly string? _objectPrefix;
    private const int MaxRetries = 5;

    public GcsXmlRepository(ILogger<GcsXmlRepository> logger, StorageClient storageClient,
        IOptions<GcsDataProtectionConfiguration> gcsDataProtectionConfiguration)
    {
        _logger = logger;
        _storageClient = storageClient;
        _bucketName = gcsDataProtectionConfiguration.Value.StorageBucket;
        _objectPrefix = gcsDataProtectionConfiguration.Value.ObjectPrefix;
    }
    
    private T DoWithRetry<T>(Func<T> func)
    {
        for (var i = 1; i < MaxRetries; i++)
        {
            try
            {
                return func();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Retry {retryCount} of {maxRetries} failed with {exception}. Retrying in 1 second.",
                    i, MaxRetries, ex);
                // Exponential backoff: 1, 2, 4, 8, 16 seconds
                System.Threading.Thread.Sleep(1000 * (1 << i-1));
            }
        }
        
        return func();
    }

    public IReadOnlyCollection<XElement> GetAllElements()
    {
        var gcsObjects = DoWithRetry(() =>
            _storageClient.ListObjects(_bucketName, _objectPrefix).Where(o => o.Name.EndsWith(".xml")).ToList()
        );

        var elements = new List<XElement>();

        foreach (var o in gcsObjects)
        {
            _logger.LogDebug("Reading data from object {objectName}", o.Name);
            using var stream = new MemoryStream();
            DoWithRetry(() => _storageClient.DownloadObject(o, stream));
            stream.Position = 0;
            elements.Add(XElement.Load(stream));
        }

        return elements.AsReadOnly();
    }

    public void StoreElement(XElement element, string friendlyName)
    {
        // Make sure the name is suitable for use in a GCS object name
        // See: https://cloud.google.com/storage/docs/naming-objects
        if (string.IsNullOrWhiteSpace(friendlyName) || friendlyName.FirstOrDefault() == '.' ||
            friendlyName.Any(char.IsControl) || friendlyName.Length > 256)
        {
            var newName = Guid.NewGuid().ToString();
            _logger.LogInformation(
                "The name {friendlyName} is potentially unsuitable for use in a GCS object name. Using {newName} instead.",
                friendlyName, newName);
            friendlyName = newName;
        }

        var objectName = $"{_objectPrefix}{friendlyName}.xml";

        _logger.LogInformation("Writing data to object {objectName}", objectName);

        using var stream = new MemoryStream();

        element.Save(stream);
        stream.Position = 0;

        // Useful note: GCS uploads are atomic
        DoWithRetry(() => _storageClient.UploadObject(_bucketName, objectName, "application/xml", stream));
    }
}
