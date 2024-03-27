# Raileasy.DataProtection.Gcs

![NuGet Version](https://img.shields.io/nuget/v/Raileasy.DataProtection.Gcs)

This library provides a way to store and retrieve ASP.NET data protection keys in an Google Cloud Storage bucket. The
keys can either be stored in the root of the bucket, or the objects can use a configurable prefix.

All GCS operations are attempted 5 times with exponential backup. If all 5 attempts fail the final exception is thrown.

## Installation

A `net8.0` Nuget package is available [here](https://www.nuget.org/packages/Raileasy.DataProtection.Gcs/).

Or you can install with the dotnet cli:

```
dotnet add package Raileasy.DataProtection.Gcs
```

## Sample Code

### Setting the persistence provider

The `PersistKeysToGcs` method can be used to set the storage up easily.

```csharp
using Raileasy.DataProtection.Gcs;

var gcsDataProtectionConfiguration = builder.Configuration
    .GetSection("GcsDataProtection")
    .Get<GcsDataProtectionConfiguration>();
builder.Services.AddDataProtection().PersistKeysToGcs(gcsDataProtectionConfiguration);
```

### Configuration options

`GcsDataProtectionConfiguration` has two properties:
- `StorageBucket`: Required - the name of the GCS bucket to store the keys in
- `ObjectPrefix`: Optional - the prefix to use for the key objects (for example, `staging/`). If this is not provided 
  then the keys are stored in the root of the bucket.
