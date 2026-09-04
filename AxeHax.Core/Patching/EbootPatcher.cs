using AxeHax.Core.Verification;
using ELFSharp.ELF;
using ELFSharp.ELF.Segments;
using System.Buffers.Binary;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Security.Cryptography;
using System.Text;

namespace AxeHax.Core.Patching;

public partial class EbootPatcher : IPatcher
{
    private const string _psarcDecryptMarker = "$#@!__DecryptPsarcs__!@#$";
    private readonly long? _patchTarget = null;
    private readonly Lazy<string?> _ppuHash = null;

    public bool GenerateRpcs3Patch = false;
    public string? Rpcs3PatchFolder = null;
    public string? GameVersion = null;
    public string? GameName;
    public string? TitleId;

    public EbootPatcher(Stream stream)
    {
        if (!stream.CanRead || !stream.CanSeek || !stream.CanWrite)
            throw new ArgumentException("Stream must be readable, seekable and writable", nameof(stream));

        Stream = stream;
        Stream.Position = 0;

        _patchTarget = FindPatchableElement(stream);
        _ppuHash = new Lazy<string?>(() => GeneratePpuHash(stream));
    }

    public Stream Stream { get; private set;  }

    // $#@!__DecryptPsarcs__!@#$

    private static long? FindPatchableElement(Stream file)
    {
        long? patchTarget = null;
        long start = Stopwatch.GetTimestamp();

        BufferedStream bufferedStream = new(file, 4 * 1024);
        BinaryReader reader = new(bufferedStream);
        Span<byte> buffer = new byte[4 * 1024];
        byte[] markerBytes = Encoding.UTF8.GetBytes(_psarcDecryptMarker);

        int ebootOffset = 0;
        int bytesRead = reader.Read(buffer);
        while (patchTarget == null && bytesRead > 0)
        {
            int index = buffer.IndexOf(markerBytes);
            if (index >= 0)
                patchTarget = ebootOffset + index;

            ebootOffset += bytesRead;
            bytesRead = reader.Read(buffer);
        }

        long end = Stopwatch.GetTimestamp();
        State.Logger.LogDebug(Patcher, $"Detecting patchables took {(end - start) / (double)Stopwatch.Frequency} seconds!");
        return patchTarget;
    }

    public static string GeneratePpuHash(Stream stream)
    {
        stream.Position = 0;
        ELF<ulong> elfFile = ELFReader.Load<ulong>(stream, false);

        using SHA1 hash = SHA1.Create();
        foreach (Segment<ulong>? segment in elfFile.Segments)
        {
            hash.TransformBlock(BitConverter.GetBytes(BinaryPrimitives.ReverseEndianness((uint)segment.Type)), 0, 4, null, 0);
            hash.TransformBlock(BitConverter.GetBytes(BinaryPrimitives.ReverseEndianness((uint)segment.Flags)), 0, 4, null, 0);

            if (segment.Type != SegmentType.Load || segment.Size == 0)
                continue;

            hash.TransformBlock(BitConverter.GetBytes(BinaryPrimitives.ReverseEndianness(segment.Address)), 0, 8, null, 0);
            hash.TransformBlock(BitConverter.GetBytes(BinaryPrimitives.ReverseEndianness(segment.Size)), 0, 8, null, 0);

            byte[]? segmentData = segment.GetFileContents();
            hash.TransformBlock(segmentData, 0, segmentData.Length, null, 0);
        }

        // trigger generation of hash after reading segments
        hash.TransformFinalBlock([], 0, 0);

        stream.Position = 0;

        string ppuHash = BitConverter.ToString(hash.Hash!).Replace("-", "").ToLower();

        State.Logger.LogDebug(PPU, $"PPU hash: PPU-{ppuHash}");
        return ppuHash;
    }

    /// <summary>
    ///     Checks the contents of the EBOOT to verify that it is patchable.
    /// </summary>
    /// <returns>A list of issues and notes about the EBOOT.</returns>
    [Pure]
    public List<Message> Verify()
    {
        List<Message> messages = new();

        Stream.Position = 0;
        State.Logger.LogInfo(LogType.Verify, "Verifying provided EBOOT file");

        try
        {
            Class output = ELFReader.CheckELFType(Stream);

            State.Logger.LogDebug(LogType.Verify, $"ELF class: {output}");
            if (output == Class.NotELF)
            {
                messages.Add(new Message(MessageLevel.Error, "EBOOT is not a valid ELF file."));
                return messages;
            }
        }
        catch (Exception e) when (e is IndexOutOfRangeException or EndOfStreamException)
        {
            messages.Add(new Message(MessageLevel.Error, "The EBOOT wasn't big enough to be valid."));
            return messages;
        }

        // Only requirement is that we needed to have found $#@!__DecryptPsarcs__!@#$ somewhere in our ELF
        if (_patchTarget == null)
            messages.Add(new Message(
                MessageLevel.Error,
                $"Could not find patchable element '{_psarcDecryptMarker}' in provided EBOOT. Nothing to patch."
            ));
        if (GenerateRpcs3Patch && string.IsNullOrWhiteSpace(_ppuHash.Value))
            messages.Add(new Message(
                MessageLevel.Error,
                """
                Couldn't determine the PPU hash. This is used by RPCS3 to know which game 
                the patch is used for. Without it, we cannot continue patching.
                """
            ));

        return messages;
    }

    public void Patch()
    {
        if (_patchTarget == null)
            throw new ArgumentException("Patchable element string not found in provided EBOOT stream!");

        if (GenerateRpcs3Patch)
        {
            Debug.Assert(Rpcs3PatchFolder != null);
            Debug.Assert(GameName != null);
            Debug.Assert(GameVersion != null);
            Debug.Assert(!string.IsNullOrEmpty(GameVersion));

            string patchesFile = Path.Combine(Rpcs3PatchFolder, "imported_patch.yml");

            if (!File.Exists(patchesFile))
                File.WriteAllText(patchesFile, "Version: 1.2\n\n");

            string template = $"""
                               PPU-{this._ppuHash}:
                                 "AxeHax PSARC Decryption Patch":
                                   Games:
                                     "{this.GameName.ReplaceLineEndings(string.Empty)}":
                                       {this.TitleId}: [ {this.GameVersion} ]
                                   Author: "Refresher (automated)"
                                   Notes: "This patches the retail version of the game to use standard unencrypted PSARCs"
                                   Patch Version:
                                   Patch:
                               """;
            string strPatchTemplate = """
                                            - [ be8, 0x{0}, "0x00" ]
                                      """;
            const long elfLoadOffset = 0x10000;
            long fileOffset = elfLoadOffset + _patchTarget.GetValueOrDefault() + _psarcDecryptMarker.Length;

            StringBuilder finalPatch = new();
            finalPatch.AppendLine("");
            finalPatch.AppendLine(template);
            finalPatch.AppendLine(string.Format(strPatchTemplate, fileOffset.ToString("x8")));
            File.AppendAllText(patchesFile, finalPatch.ToString());
        }
        else
        {
            using BinaryWriter writer = new(Stream);
            writer.BaseStream.Position = _patchTarget.GetValueOrDefault() + _psarcDecryptMarker.Length;
            writer.Write((byte)0);
        }
    }
}
