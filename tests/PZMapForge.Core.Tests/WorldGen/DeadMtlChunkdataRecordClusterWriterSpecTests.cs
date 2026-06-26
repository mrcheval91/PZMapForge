using PZMapForge.Core.WorldGen;
using Xunit;

namespace PZMapForge.Core.Tests.WorldGen;

public sealed class DeadMtlChunkdataRecordClusterWriterSpecTests
{
    // MAP37B_CORE_1: HeaderSize constant is 2 (MAP-37A confirmed 2-byte header)
    [Fact]
    public void HeaderSize_IsTwo()
    {
        Assert.Equal(2, DeadMtlChunkdataRecordClusterWriterSpec.HeaderSize);
    }

    // MAP37B_CORE_2: RecordWidth constant is 8 (MAP-37A ExactFitScore=3 for width=8)
    [Fact]
    public void RecordWidth_IsEight()
    {
        Assert.Equal(8, DeadMtlChunkdataRecordClusterWriterSpec.RecordWidth);
    }

    // MAP37B_CORE_3: MinimalFileSize == 2 + 128 * 8 == 1026 (exact-fit equation)
    [Fact]
    public void MinimalFileSize_MatchesExactFitEquation()
    {
        Assert.Equal(1026, DeadMtlChunkdataRecordClusterWriterSpec.MinimalFileSize);
        Assert.Equal(
            DeadMtlChunkdataRecordClusterWriterSpec.HeaderSize
            + DeadMtlChunkdataRecordClusterWriterSpec.DefaultRecordCount
            * DeadMtlChunkdataRecordClusterWriterSpec.RecordWidth,
            DeadMtlChunkdataRecordClusterWriterSpec.MinimalFileSize);
    }

    // MAP37B_CORE_4: BuildMinimalChunkdata returns 1026 bytes with header 0x00 0x01
    [Fact]
    public void BuildMinimalChunkdata_Returns1026BytesWithDefaultHeader()
    {
        var data = DeadMtlChunkdataRecordClusterWriterSpec.BuildMinimalChunkdata();
        Assert.Equal(1026, data.Length);
        Assert.Equal(0x00, data[0]);
        Assert.Equal(0x01, data[1]);
        for (var i = 2; i < data.Length; i++)
            Assert.Equal(0, data[i]);
    }

    // MAP37B_CORE_5: Validate rejects data shorter than header (truncated)
    [Fact]
    public void Validate_RejectsTruncatedData()
    {
        var (isValid, reason) = DeadMtlChunkdataRecordClusterWriterSpec.Validate(new byte[1]);
        Assert.False(isValid);
        Assert.Contains("MAP37B_REJECT_HEADER_INCOMPLETE", reason);
    }

    // MAP37B_CORE_6: Validate rejects data with body not divisible by RecordWidth
    [Fact]
    public void Validate_RejectsMisalignedBody()
    {
        var bad = new byte[2 + 5]; // 5 bytes body — not divisible by 8
        var (isValid, reason) = DeadMtlChunkdataRecordClusterWriterSpec.Validate(bad);
        Assert.False(isValid);
        Assert.Contains("MAP37B_REJECT_MISALIGNED_BODY", reason);
    }
}
