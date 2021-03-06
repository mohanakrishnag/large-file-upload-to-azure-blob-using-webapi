﻿using Cache;
using Repository;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Operations
{
    public class Operations : IOperations
    {
        private IBlobRepository _blobRepository;
        private IAzureCache _azureCache;

        public Operations(IBlobRepository BlobRepository, IAzureCache AzureCache)
        {
            _blobRepository = BlobRepository;
            _azureCache = AzureCache;
        }

        public void UploadChunk(FileChunk chunk, Stream data)
        {
            _blobRepository.UploadBlock(chunk.FileId, chunk.ChunkId, data);
            _azureCache.PutItem(new CacheItem() { FileId = chunk.FileId, Item = chunk });
        }

        public void CommitChunks(FileChunk chunk)
        {
            if (chunk.IsCompleted)
            {
                List<CacheItem> cacheItems = _azureCache.GetItems(chunk.FileId);
                Dictionary<string, string> blockIds = cacheItems.Select(p => (FileChunk)p.Item)
                                                                .Select(p => new { p.OriginalChunkId, p.ChunkId })
                                                                .ToDictionary(d => d.OriginalChunkId, d => d.ChunkId);

                var comparer = new BlockIdComparer();
                blockIds = blockIds.OrderBy(p => p.Key).ToDictionary(p => p.Key, p => p.Value);
                _blobRepository.CommintBlocks(chunk.FileId, blockIds.Select(p => p.Value).ToList());
                _azureCache.RemoveItems(chunk.FileId);
            }
        }
    }
}
