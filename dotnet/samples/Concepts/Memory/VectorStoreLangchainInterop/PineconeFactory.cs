﻿// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel.Connectors.Pinecone;
using Sdk = Pinecone;

namespace Memory.VectorStoreLangchainInterop;

/// <summary>
/// Contains a factory method that can be used to create a Pinecone vector store that is compatible with datasets ingested using Langchain.
/// </summary>
/// <remarks>
/// This class is used with the <see cref="VectorStore_Langchain_Interop"/> sample.
/// </remarks>
public static class PineconeFactory
{
    /// <summary>
    /// Record definition that matches the storage format used by Langchain for Pinecone.
    /// </summary>
    private static readonly VectorStoreRecordDefinition s_recordDefinition = new()
    {
        Properties = new List<VectorStoreRecordProperty>
        {
            new VectorStoreRecordKeyProperty("Key", typeof(string)),
            new VectorStoreRecordDataProperty("Content", typeof(string)) { StoragePropertyName = "text" },
            new VectorStoreRecordDataProperty("Source", typeof(string)) { StoragePropertyName = "source" },
            new VectorStoreRecordVectorProperty("Embedding", typeof(ReadOnlyMemory<float>)) { StoragePropertyName = "embedding", Dimensions = 1536 }
        }
    };

    /// <summary>
    /// Create a new Pinecone-backed <see cref="IVectorStore"/> that can be used to read data that was ingested using Langchain.
    /// </summary>
    /// <param name="pineconeClient">Pinecone client that can be used to manage the collections and points in a Pinecone store.</param>
    /// <returns>The <see cref="IVectorStore"/>.</returns>
    public static IVectorStore CreatePineconeLangchainInteropVectorStore(Sdk.PineconeClient pineconeClient)
        => new PineconeLangchainInteropVectorStore(pineconeClient);

    private sealed class PineconeLangchainInteropVectorStore(Sdk.PineconeClient pineconeClient)
        : PineconeVectorStore(pineconeClient)
    {
        private readonly Sdk.PineconeClient _pineconeClient = pineconeClient;

        public override IVectorStoreRecordCollection<TKey, TRecord> GetCollection<TKey, TRecord>(string name, VectorStoreRecordDefinition? vectorStoreRecordDefinition = null)
        {
            if (typeof(TKey) != typeof(string) || typeof(TRecord) != typeof(LangchainDocument<string>))
            {
                throw new NotSupportedException("This VectorStore is only usable with string keys and LangchainDocument<string> record types");
            }

            // Create a Pinecone collection and pass in our custom record definition that matches
            // the schema used by Langchain so that the default mapper can use the storage names
            // in it, to map to the storage scheme.
            return (new PineconeVectorStoreRecordCollection<TRecord>(
                _pineconeClient,
                name,
                new()
                {
                    VectorStoreRecordDefinition = s_recordDefinition
                }) as IVectorStoreRecordCollection<TKey, TRecord>)!;
        }
    }
}
