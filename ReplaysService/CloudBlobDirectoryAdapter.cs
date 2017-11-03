﻿using System;
using Microsoft.WindowsAzure.Storage.Blob;

namespace toofz.NecroDancer.Leaderboards.ReplaysService
{
    internal sealed class CloudBlobDirectoryAdapter : ICloudBlobDirectory
    {
        public CloudBlobDirectoryAdapter(CloudBlobDirectory directory)
        {
            this.directory = directory ?? throw new ArgumentNullException(nameof(directory));
        }

        private readonly CloudBlobDirectory directory;

        public ICloudBlockBlob GetBlockBlobReference(string blobName)
        {
            return new CloudBlockBlobAdapter(directory.GetBlockBlobReference(blobName));
        }
    }
}
