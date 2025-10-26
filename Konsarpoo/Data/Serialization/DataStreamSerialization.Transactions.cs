using System;
using System.Collections.Generic;
using System.IO;

namespace Konsarpoo.Collections.Data.Serialization;

public partial class DataStreamSerialization
{
    private sealed class StreamContext : IDisposable
    {
        public Stream Stream { get; }
        public BinaryWriter Writer { get; }
        public BinaryReader Reader { get; }
        public bool OwnsStream { get; }
        // Snapshot of in-memory state
        public int ArrayCount { get; }
        public int DataCount { get; }
        public int VersionValue { get; }
        public int MaxSizeOfArrayValue { get; }
        public byte[] ExtraMetaDataSnapshot { get; }
        public long[] OffsetTableSnapshot { get; }
        public int EditCount { get; }

        public StreamContext(Stream stream, BinaryWriter writer, BinaryReader reader, bool ownsStream,
            int arrayCount, int dataCount, int versionValue, int maxSizeOfArrayValue,
            byte[] extraMetaDataSnapshot, long[] offsetTableSnapshot, int editCount)
        {
            Stream = stream;
            Writer = writer;
            Reader = reader;
            OwnsStream = ownsStream;
            ArrayCount = arrayCount;
            DataCount = dataCount;
            VersionValue = versionValue;
            MaxSizeOfArrayValue = maxSizeOfArrayValue;
            ExtraMetaDataSnapshot = extraMetaDataSnapshot;
            OffsetTableSnapshot = offsetTableSnapshot;
            EditCount = editCount;
        }

        public void Dispose()
        {
            if (OwnsStream)
            {
                Writer?.Dispose();
                Reader?.Dispose();
                Stream?.Dispose();
            }
        }
    }

    private readonly Stack<StreamContext> m_contextStack = new Stack<StreamContext>();

    public void BeginTransaction(string path)
    {
        PushStagingStream(CreateStagingStream(path));
    }

    public bool EndTransaction()
    {
        if (m_contextStack.Count > 0)
        {
            CommitTopStage();

            return true;
        }

        return false;
    }

    public bool CancelTransaction()
    {
        if (m_contextStack.Count > 0)
        {
            CancelTopStage();

            return true;
        }

        return false;
    }

    protected virtual Stream CreateStagingStream(string path)
    {
        // Use a temp file on disk; DeleteOnClose cleans it up on dispose.
        string tempPath = Path.Combine(path, Guid.NewGuid().ToString("D") + ".bak");

        var fs = new FileStream(
            tempPath,
            FileMode.Create,
            FileAccess.ReadWrite,
            FileShare.None,
            bufferSize: 64 * 1024,
            options: FileOptions.DeleteOnClose | FileOptions.SequentialScan);

        m_fileStream.Seek(0, SeekOrigin.Begin);
        m_fileStream.CopyTo(fs);

        fs.Seek(0, SeekOrigin.Begin);

        var staging = fs;

        return staging;
    }

    private void PushStagingStream(Stream stagingStream)
    {
        // Save current context with snapshot (do not own root file resources)
        var parent = new StreamContext(
            m_fileStream,
            m_writer,
            m_reader,
            ownsStream: false,
            arrayCount: m_arrayCount,
            dataCount: m_dataCount,
            versionValue: m_version,
            maxSizeOfArrayValue: m_maxSizeOfArray,
            extraMetaDataSnapshot: m_extraMetaData != null ? (byte[])m_extraMetaData.Clone() : Array.Empty<byte>(),
            offsetTableSnapshot: m_offsetTable != null ? (long[])m_offsetTable.Clone() : Array.Empty<long>(),
            editCount: m_edit);
        m_contextStack.Push(parent);

        // Switch to staging context (we own it)
        m_fileStream = stagingStream;
        m_writer = new BinaryWriter(m_fileStream);
        m_reader = new BinaryReader(m_fileStream);
    }

    private void CommitTopStage()
    {
        if (m_contextStack.Count == 0)
        {
            return;
        }

        // Current top is the staging context we own
        var stage = new StreamContext(m_fileStream, m_writer, m_reader, ownsStream: true,
            arrayCount: 0, dataCount: 0, versionValue: 0, maxSizeOfArrayValue: 0,
            extraMetaDataSnapshot: Array.Empty<byte>(), offsetTableSnapshot: Array.Empty<long>(), editCount: 0);

        // Restore parent context (do not restore memory on commit)
        var parent = m_contextStack.Pop();

        // Copy staged content into parent stream
        stage.Writer.Flush();
        stage.Stream.Seek(0, SeekOrigin.Begin);

        parent.Stream.Seek(0, SeekOrigin.Begin);
        parent.Stream.SetLength(0);
        stage.Stream.CopyTo(parent.Stream);
        parent.Writer.Flush();

        // Dispose the staging resources
        stage.Dispose();

        // Switch back to parent stream and IO resources
        m_fileStream = parent.Stream;
        m_writer = parent.Writer;
        m_reader = parent.Reader;
        // Keep current in-memory fields as they represent committed state
    }

    private void CancelTopStage()
    {
        if (m_contextStack.Count == 0)
        {
            return;
        }

        // Current top is the staging context we own; just dispose it
        var stage = new StreamContext(m_fileStream, m_writer, m_reader, ownsStream: true,
            arrayCount: 0, dataCount: 0, versionValue: 0, maxSizeOfArrayValue: 0,
            extraMetaDataSnapshot: Array.Empty<byte>(), offsetTableSnapshot: Array.Empty<long>(), editCount: 0);

        // Restore parent context and snapshot
        var parent = m_contextStack.Pop();

        // Dispose staging without copying back
        stage.Dispose();

        // Switch back to parent stream and IO resources
        m_fileStream = parent.Stream;
        m_writer = parent.Writer;
        m_reader = parent.Reader;

        // Restore in-memory snapshot
        RestoreFromSnapshot(parent);
    }

    private void DiscardAllStages()
    {
        while (m_contextStack.Count > 0)
        {
            // Dispose current staging context
            var stage = new StreamContext(m_fileStream, m_writer, m_reader, ownsStream: true,
                arrayCount: 0, dataCount: 0, versionValue: 0, maxSizeOfArrayValue: 0,
                extraMetaDataSnapshot: Array.Empty<byte>(), offsetTableSnapshot: Array.Empty<long>(), editCount: 0);
            var parent = m_contextStack.Pop();

            stage.Dispose();

            // Switch back to parent to continue unwinding
            m_fileStream = parent.Stream;
            m_writer = parent.Writer;
            m_reader = parent.Reader;

            // Restore in-memory snapshot at each unwind step
            RestoreFromSnapshot(parent);
        }
    }

    private void RestoreFromSnapshot(StreamContext snapshot)
    {
        m_arrayCount = snapshot.ArrayCount;
        m_dataCount = snapshot.DataCount;
        m_version = snapshot.VersionValue;
        m_maxSizeOfArray = snapshot.MaxSizeOfArrayValue;
        m_extraMetaData = snapshot.ExtraMetaDataSnapshot != null ? (byte[])snapshot.ExtraMetaDataSnapshot.Clone() : Array.Empty<byte>();
        m_offsetTable = snapshot.OffsetTableSnapshot != null ? (long[])snapshot.OffsetTableSnapshot.Clone() : Array.Empty<long>();
        m_edit = snapshot.EditCount;
    }

    // Call this at the beginning of Dispose()
    private void DisposeTransactionalContexts()
    {
        DiscardAllStages();
    }
}