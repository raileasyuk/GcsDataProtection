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

    public GcsXmlRepository(ILogger<GcsXmlRepository> logger, StorageClient storageClient,
        IOptions<GcsDataProtectionConfiguration> gcsDataProtectionConfiguration)
    {
        _logger = logger;
        _storageClient = storageClient;
        _bucketName = gcsDataProtectionConfiguration.Value.StorageBucket;
        _objectPrefix = gcsDataProtectionConfiguration.Value.ObjectPrefix;
    }

    public IReadOnlyCollection<XElement> GetAllElements()
    {
        var gcsObjects = _storageClient.ListObjects(_bucketName, _objectPrefix)
            .Where(o => o.Name.EndsWith(".xml"));

        var elements = new List<XElement>();

        foreach (var o in gcsObjects)
        {
            _logger.LogDebug("Reading data from object {objectName}", o.Name);
            using var stream = new MemoryStream();
            _storageClient.DownloadObject(o, stream);
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
        _storageClient.UploadObject(_bucketName, objectName, "application/xml", stream);
    }
}
