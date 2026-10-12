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

        using (BinaryWriter writer = new BinaryWriter(Stream))
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