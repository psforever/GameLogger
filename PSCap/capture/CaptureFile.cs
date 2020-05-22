using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PSCap
{
    public class InvalidCaptureFileException : Exception
    {
        public InvalidCaptureFileException() { }
        public InvalidCaptureFileException(string message)
            : base(message)
        {
        }

        public InvalidCaptureFileException(string message, Exception ex)
            : base(message, ex)
        {
        }
    }

    class CaptureFile
    {
        // bump these when releasing capture file changes
        public const byte VERSION_MAJOR = 1;
        public const byte VERSION_MINOR = 0;

        /* GCAP File Format
            MAGIC = "GCAP" [4 bytes]
            CAPTURE_VERSION = "1.0" [2 bytes, one per version point, independent of tool version]
            CAPTURE_GUID = "ab78c938c18aff2873acc" [GUID]
            CAPTURE_TIME = 140378339 [QWORD time_t] (time since the UNIX epoch)
            CAPTURE_DESC = "Neat capture description" [Octet stream with length prepended]
            RECORD_HASH = a hash of all of the record hashes (for global consitiency)
            RECORD_COUNT = N [DWORD]
            RECORD1 = GameRecord
            ...
            RECORDN = GameRecord
            <EOF>
        */

        const string MAGIC = "GCAP";

        struct GameCapHeader
        {
            public string Magic;
            public byte VersionMajor;
            public byte VersionMinor;

            // monotonic counter that is incremented with each save
            public UInt64 CaptureRevision;
            public Guid GUID;
            public UInt64 CaptureStart;
            public UInt64 CaptureEnd;
            public UInt64 RecordCount;
            public byte[] HeaderChecksum; // SHA256 - digest of 32 bytes

            public const int HEADER_LEN = 4 + 1 + 1 + 8 + 16 + 8 + 8 + 8 + 32;
        }

        // capture file properties
        Guid GUID = new Guid();
        string captureFilename = "";
        string captureName = "";
        string captureDescription = "";
        ulong captureRevision = 0;

        List<Record> records = new List<Record>();
        ulong unixTimeStart = 0;
        ulong unixTimeEnd = 0;

        // capture file state
        bool frozen = false;
        bool modified = false;
        bool firstSave = true;

        public static class Factory
        {
            public static CaptureFile New()
            {
                CaptureFile file =  new CaptureFile();
                // pick some sane default capture name
                file.captureFilename = string.Format("PSCap-{0:yyyy-MM-dd_hh-mm-ss-tt}", DateTime.Now);
                file.captureName = string.Format("PSCap {0:yyyy-MM-dd_hh-mm-ss-tt}", DateTime.Now);
                file.unixTimeStart = Util.GetUNIXTime();

                return file;
            }

            public static CaptureFile FromFile(string filename, Form parent, ProgressDialog progress)
            {
                CaptureFile cap = new CaptureFile();

                // read the header and verify the checksum and magic
                using (FileStream file = File.Open(filename, FileMode.Open, FileAccess.Read))
                {
                    GameCapHeader header = ReadAndVerifyHeader(file);

                    cap.unixTimeStart = header.CaptureStart;
                    cap.unixTimeEnd = header.CaptureEnd;
                    cap.captureRevision = header.CaptureRevision;
                    cap.GUID = header.GUID;

                    Log.Debug("CaptureFile.Load: time {{{0} -> {1}}}, rev {2}, GUID {3}, records {4}",
                        cap.unixTimeStart, cap.unixTimeEnd, cap.captureRevision, cap.GUID, header.RecordCount);

                    // print metadata and read records
                    byte[] buffer = new byte[1024*1024*10];
                    BitStream stream = new BitStream();
                    bool metaFound = false;

                    // start deserializing records
                    int stepStride = (int)header.RecordCount / 20;
                    int nextStepPoint = stepStride;

                    parent.SafeInvoke((a) => progress.ProgressParams((int)header.RecordCount, stepStride));

                    for (UInt64 i = 0; i < header.RecordCount; i++)
                    {
                        try
                        {
                            Record rec = ReadOneRecord(file, buffer, stream);

                            switch (rec.type)
                            {
                                case RecordType.GAME:
                                    //if (i % 1000 == 0)
                                    //    Log.Debug("Game record {0}", i);
                                    cap.addRecord(rec);
                                    break;
                                case RecordType.METADATA:
                                    if (metaFound)
                                        throw new InvalidCaptureFileException("Multiple metadata records");

                                    RecordMetadata meta = rec as RecordMetadata;
                                    cap.captureDescription = meta.description;
                                    cap.captureName = meta.captureName;
                                    metaFound = true;
                                    break;
                            }

                            if((int)i >= nextStepPoint)
                            {
                                nextStepPoint += stepStride;
                                parent.SafeInvoke((a) => progress.Step());
                            }
                        }
                        catch(IOException e)
                        {
                            throw new InvalidCaptureFileException("IOException: " + e.Message, e);
                        }
                        catch(InvalidCaptureFileException e)
                        {
                            throw new InvalidCaptureFileException(e.Message + string.Format(" at record {0}", i), e);
                        }
                    }

                    if (!metaFound)
                        Log.Warning("Capture file is missing a metadata record. Please resave to fix this.");
                }

                cap.existingCapture(filename);

                return cap;
            }

            public static void ToFile(CaptureFile cap, string filename)
            {
                GameCapHeader header = new GameCapHeader();

                header.Magic = MAGIC;
                header.VersionMajor = VERSION_MAJOR;
                header.VersionMinor = VERSION_MINOR;

                // only bump the revision if changes were made
                if(cap.isModified())
                    header.CaptureRevision = cap.captureRevision + 1;
                else
                    header.CaptureRevision = cap.captureRevision;

                header.GUID = cap.GUID;
                header.CaptureStart = cap.unixTimeStart;
                header.CaptureEnd = cap.unixTimeEnd;
                header.RecordCount = (ulong)cap.records.Count;

                List<byte> headerBytes = new List<byte>(GameCapHeader.HEADER_LEN);

                headerBytes.AddRange(Encoding.ASCII.GetBytes(header.Magic));
                BitOps.WriteByte(headerBytes, header.VersionMajor);
                BitOps.WriteByte(headerBytes, header.VersionMinor);
                BitOps.WriteUInt64(headerBytes, header.CaptureRevision);
                headerBytes.AddRange(header.GUID.ToByteArray());
                BitOps.WriteUInt64(headerBytes, header.CaptureStart);
                BitOps.WriteUInt64(headerBytes, header.CaptureEnd);
                BitOps.WriteUInt64(headerBytes, header.RecordCount);

                // minus
                byte[] calcChecksum = SHA256.Create().ComputeHash(headerBytes.ToArray());

                Trace.Assert(calcChecksum.Length == 32, "Invalid SHA-256 checksum length");

                headerBytes.AddRange(calcChecksum);

                // write the header and records
                using (FileStream file = File.Open(filename, FileMode.Create, FileAccess.Write))
                {
                    file.Write(headerBytes.ToArray(), 0, headerBytes.Count);

                    // write a metadata record
                    RecordMetadata meta = Record.Factory.Create(RecordType.METADATA) as RecordMetadata;
                    meta.description = cap.captureDescription;
                    meta.captureName = cap.captureName;

                    try
                    {
                        BitStream stream = Record.Factory.Encode(meta);
                        //Log.Debug(Util.HexDump(stream.data));
                        file.Write(stream.data, 0, stream.data.Length);
                    }
                    catch (InvalidOperationException e)
                    {
                        throw new IOException("Failed to encode metadata record: " + e.Message, e);
                    }

                    // serialize records
                    for (UInt64 i = 0; i < header.RecordCount; i++)
                    {
                        Record rec = cap.records[(int)i];

                        try
                        {
                            BitStream stream = Record.Factory.Encode(rec);
                            file.Write(stream.data, 0, stream.data.Length);
                        }
                        catch (InvalidOperationException e)
                        {
                            throw new IOException(string.Format("Failed to encode Record[{0}]: {1}", i, e.Message), e);
                        }
                    }
                }

                // commit the file in memory (finish it)
                cap.captureRevision = header.CaptureRevision;
                cap.existingCapture(filename);
            }

            private static Record ReadOneRecord(FileStream file, byte[] buffer, BitStream streamBuffer)
            {
                // be greedy and use the cache
                Record r = TryDecodeRecord(streamBuffer);

                if (r != null)
                    return r;

                bool eof = false;
                int dataReady = 0;

                // else, read some data in first
                while (true)
                {
                    int amtRead = file.Read(buffer, 0, buffer.Length);

                    if (amtRead == 0 || amtRead < 0)
                    {
                        eof = true;
                    }
                    else
                    {
                        dataReady += amtRead;
                        // add the data we just got to the stream
                        streamBuffer.append(buffer, 0, amtRead);
                        //Log.Debug("CaptureFile.Read {0} bytes, buffered {1}", amtRead, streamBuffer.sizeLeft());
                    }
                    
                    Record rec = TryDecodeRecord(streamBuffer);

                    if (rec != null)
                        return rec;
                    
                    if (eof)
                        throw new InvalidCaptureFileException("Unexpected end of capture file");
                }
            }

            private static Record TryDecodeRecord(BitStream streamBuffer)
            {
                if (streamBuffer.size() <= 0)
                    return null;

                int startPos = streamBuffer.position();

                try
                {
                    Record rec = Record.Factory.Decode(streamBuffer);
                    return rec;
                }
                catch (NeedMoreDataException)
                {
                    // undo any failed processing
                    streamBuffer.seek(startPos);
                    return null;
                }
                catch (InvalidOperationException e)
                {
                    throw new InvalidCaptureFileException(string.Format("General record decoding error"), e);
                }
                catch (ArgumentException e)
                {
                    throw new InvalidCaptureFileException(string.Format("Invalid record type ({0})", e.Message), e);
                }
            }

            private static GameCapHeader ReadAndVerifyHeader(FileStream file)
            {
                GameCapHeader header = new GameCapHeader();
                byte[] headerBytes = new byte[GameCapHeader.HEADER_LEN];

                int readAmt = file.Read(headerBytes, 0, GameCapHeader.HEADER_LEN);

                if (readAmt != GameCapHeader.HEADER_LEN)
                {
                    throw new InvalidCaptureFileException("File header is not the correct length");
                }

                BitStream headerStream = new BitStream(headerBytes);
                header.Magic = headerStream.extractString((uint)MAGIC.Length);

                if (header.Magic != MAGIC)
                {
                    throw new InvalidCaptureFileException("Invalid GCAP Magic");
                }

                header.VersionMajor = BitOps.ReadByte(headerStream);
                header.VersionMinor = BitOps.ReadByte(headerStream);
                header.CaptureRevision = BitOps.ReadUInt64(headerStream);
                header.GUID = new Guid(headerStream.extractOctetStream(16).ToArray());
                header.CaptureStart = BitOps.ReadUInt64(headerStream);
                header.CaptureEnd = BitOps.ReadUInt64(headerStream);
                header.RecordCount = BitOps.ReadUInt64(headerStream);
                header.HeaderChecksum = headerStream.extractOctetStream(32).ToArray();

                byte[] calcChecksum = SHA256.Create().ComputeHash(headerBytes, 0, GameCapHeader.HEADER_LEN - 32);

                if (!calcChecksum.SequenceEqual(header.HeaderChecksum))
                {
                    throw new InvalidCaptureFileException("Invalid header checksum");
                }

                if (VERSION_MAJOR != header.VersionMajor || VERSION_MINOR != header.VersionMinor)
                {
                    throw new InvalidCaptureFileException(
                        string.Format("Incompatible GCAP file version.\nExpected {0}.{1}, got {2}.{3}\nPlease upgrade your GameCap version to read newer files.",
                            VERSION_MAJOR, VERSION_MINOR, header.VersionMajor, header.VersionMinor)
                        );
                }

                // this shouldnt be fatal as time might change while capturing (i.e. user adjusts clock)
                if (header.CaptureEnd < header.CaptureStart)
                {
                    Log.Warning("Capture end time ({0}) comes BEFORE the start time ({1})",
                        header.CaptureEnd, header.CaptureStart);
                }

                return header;
            }
            
        }

        public CaptureFile()
        {
            modified = true;
            frozen = false;
            firstSave = true;
            GUID = Guid.NewGuid();
        }

        private void existingCapture(string filename)
        {
            captureFilename = filename;
            modified = false;
            frozen = true; // capture records are immutable
            firstSave = false;
        }

        public void finalize()
        {
            Trace.Assert(!frozen, "Attempted to finalize frozen capture file");
            unixTimeEnd = Util.GetUNIXTime();
            frozen = true;
        }

        public string getCaptureName()
        {
            return captureName;
        }

        public string getCaptureDescription()
        {
            return captureDescription;
        }

        public string getCaptureFilename()
        {
            return captureFilename;
        }

        public void setCaptureName(string name)
        {
            if (name != captureName)
            {
                captureName = name;
                modified = true;
            }
        }

        public void setCaptureDescription(string desc)
        {
            if(desc != captureDescription)
            {
                captureDescription = desc;
                modified = true;
            }
        }

        public bool isFirstSave()
        {
            return firstSave;
        }

        public Record getRecord(int which)
        {
            return records[which];
        }

        public IEnumerable<Record> getRecords()
        {
            return records;
        }

        public void addRecord(Record record)
        {
            Trace.Assert(!frozen, "Tried to add a record to a frozen capture file");

            records.Add(record);
            modified = true;
        }

        public int getNumRecords()
        {
            return records.Count;
        }

        public ulong getStartTime()
        {
            return unixTimeStart;
        }

        public bool isModified()
        {
            return modified;
        }

        public override string ToString()
        {
            return string.Format("Capture {0}, modified {1}", getCaptureName(), isModified());
        }
    }
}
