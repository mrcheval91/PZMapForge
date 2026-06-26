namespace PZMapForge.Core.WorldGen;

// MAP-37B: chunkdata format spec derived from MAP-37A ExactFitScore=3 confirmation.
// 2-byte header + 8-byte records. Minimal cell = 128 records = 1026 bytes.
// verified_chunkdata_format=false: structure matches hypothesis; not load-tested.
public static class DeadMtlChunkdataRecordClusterWriterSpec
{
    public const int HeaderSize         = 2;
    public const int RecordWidth        = 8;
    public const int DefaultRecordCount = 128;
    public const int MinimalFileSize    = HeaderSize + DefaultRecordCount * RecordWidth; // 1026

    public static byte[] BuildMinimalChunkdata(byte header0 = 0x00, byte header1 = 0x01)
    {
        var data = new byte[MinimalFileSize];
        data[0] = header0;
        data[1] = header1;
        return data;
    }

    public static (bool IsValid, string Reason) Validate(byte[] data)
    {
        if (data == null || data.Length < HeaderSize)
            return (false, $"MAP37B_REJECT_HEADER_INCOMPLETE: length {data?.Length ?? 0} < {HeaderSize}");
        var bodyLen = data.Length - HeaderSize;
        if (bodyLen % RecordWidth != 0)
            return (false, $"MAP37B_REJECT_MISALIGNED_BODY: body length {bodyLen} not divisible by record width {RecordWidth}");
        return (true, "MAP37B_OK");
    }
}
