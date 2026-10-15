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
    private const string _arcInfoMarker = "$#@!__ArcInfo__!@#$";
    private readonly long? _decryptPatchTarget = null;
    private readonly long? _arcInfoPatchTarget = null;
    private bool _isDisposed = false;

    public bool GenerateRpcs3Patch = false;
    public string? Rpcs3PatchFolder = null;
    public string? GameVersion = null;
    public string? GameName;
    public string? TitleId;

    public EbootPatcher(Stream stream, bool noEncryptPsarcs)
    {
        if (!stream.CanRead || !stream.CanSeek || !stream.CanWrite)
            throw new ArgumentException("Stream must be readable, seekable and writable", nameof(stream));

        this.NoEncryptPsarcs = noEncryptPsarcs;
        this._arcInfoPatchTarget = FindPatchableElement(stream, _arcInfoMarker);
        if (this.NoEncryptPsarcs)
            this._decryptPatchTarget = FindPatchableElement(stream, _psarcDecryptMarker);

        this.Stream = stream;
        this.Stream.Position = 0;
    }

    public Stream Stream { get; private set; }
    public bool NoEncryptPsarcs { get; private set; }

    private static long? FindPatchableElement(Stream file, string patchMarker)
    {
        long? patchTarget = null;
        long start = Stopwatch.GetTimestamp();

        file.Position = 0;
        BufferedStream bufferedStream = new(file, 4 * 1024);
        BinaryReader reader = new(bufferedStream);
        Span<byte> buffer = new byte[4 * 1024];
        byte[] markerBytes = Encoding.UTF8.GetBytes(patchMarker);

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

            if (segment.Type != SegmentType.Load || segment.Size == 0) continue;

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

        this.Stream.Position = 0;
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

        if (this._arcInfoPatchTarget == null)
            messages.Add(new Message(
                MessageLevel.Error,
                $"Could not find patchable element '{_arcInfoMarker}' in provided EBOOT. Nothing to patch."
            ));
        if (this.NoEncryptPsarcs && this._decryptPatchTarget == null)
            messages.Add(new Message(
                MessageLevel.Error,
                $"Could not find patchable element '{_psarcDecryptMarker}' in provided EBOOT. Nothing to patch."
            ));

        return messages;
    }

    public void Patch()
    {
        if (this._arcInfoPatchTarget == null)
            throw new ArgumentException("ArcInfo patchable element string not found in provided EBOOT stream!");
        if (this.NoEncryptPsarcs && this._decryptPatchTarget == null)
            throw new ArgumentException("DecryptPsarcs patchable element string not found in provided EBOOT stream!");

        using (BinaryWriter writer = new BinaryWriter(this.Stream, Encoding.UTF8, leaveOpen: true))
        {
            writer.BaseStream.Seek(
                this._arcInfoPatchTarget.GetValueOrDefault() + _arcInfoMarker.Length,
                SeekOrigin.Begin
            );
            byte[] patchData = System.Text.Encoding.UTF8.GetBytes("NOT_STAMPED");
            writer.Write(patchData);
            writer.Write((byte)0);

            if (this.NoEncryptPsarcs)
            {
                writer.BaseStream.Seek(
                    this._decryptPatchTarget.GetValueOrDefault() + _psarcDecryptMarker.Length,
                    SeekOrigin.Begin
                );
                writer.Write((byte)0);
            }

            writer.Flush();
        }

        if (this.GenerateRpcs3Patch)
        {
            Debug.Assert(this.Rpcs3PatchFolder != null);
            Debug.Assert(this.GameName != null);
            Debug.Assert(this.TitleId != null);
            Debug.Assert(!string.IsNullOrEmpty(this.GameVersion));

            this.Stream.Position = 0;
            string ppuHash = GeneratePpuHash(this.Stream);
            string patchesFile = Path.Combine(this.Rpcs3PatchFolder, "imported_patch.yml");
            if (!File.Exists(patchesFile))
                File.WriteAllText(patchesFile, "Version: 1.2\n\n");

            string patchTemplate = $"""
                                   PPU-{ppuHash}:
                                     "AxeHax Patch (LAN Lobby)":
                                       Games:
                                         "{this.GameName.ReplaceLineEndings(string.Empty)}":
                                           {this.TitleId}: [ {this.GameVersion} ]
                                      Author: AxeHax (automated)"
                                      Notes: "This patches the game to allow LAN lobbies to work with RPCS3."
                                      Patch Version:
                                      Patch:
                                       # HostLANLobby
                                       - [ calloc, 0x00313354, 5 ]
                                       - [ be32, 0x0, 0xf821fc41 ]   # stdu r1,-0x3c0(r1)
                                       - [ be32, 0x0, 0x3d600228 ]   # lis  r11,0x0228
                                       - [ be32, 0x0, 0x396b70c4 ]   # addi r11,r11,0x70c4
                                       - [ be32, 0x0, 0x39800001 ]   # li   r12,1
                                       - [ be32, 0x0, 0x998b0000 ]   # stb  r12,0(r11)
                                       # JoinLANLobby
                                       - [ calloc, 0x003136bc, 5 ]
                                       - [ be32, 0x0, 0xf821ff41 ]   # stdu r1,-0xc0(r1)
                                       - [ be32, 0x0, 0x3d600228 ]   # lis  r11,0x0228
                                       - [ be32, 0x0, 0x396b70c4 ]   # addi r11,r11,0x70c4
                                       - [ be32, 0x0, 0x39800000 ]   # li   r12,0
                                       - [ be32, 0x0, 0x998b0000 ]   # stb  r12,0(r11)
                                   """;

            StringBuilder finalPatch = new();
            finalPatch.AppendLine("");
            finalPatch.AppendLine(patchTemplate);
            File.AppendAllText(patchesFile, finalPatch.ToString());
        }
    }

    public void Dispose()
    {
        if (this._isDisposed)
            return;

        this.Stream.Dispose();
        this._isDisposed = true;
    }
}