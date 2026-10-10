using AxeHax.Core.Pipelines;

namespace AxeHax.Core.Accessors;

public static class PatchAccessorExtensions
{
    public static async Task UploadFileWithProgressAsync(this PatchAccessor accessor, string inPath, string outPath, Step step, CancellationToken cancellationToken = default)
    {
        if (accessor == null) throw new ArgumentNullException(nameof(accessor));
        if (inPath == null) throw new ArgumentNullException(nameof(inPath));
        if (step == null) throw new ArgumentNullException(nameof(step));

        using FileStream inStream = File.OpenRead(inPath);
        await accessor.UploadStreamWithProgressAsync(inStream, outPath, step, cancellationToken).ConfigureAwait(false);
    }

    public static async Task UploadStreamWithProgressAsync(this PatchAccessor accessor, Stream inputStream, string outPath, Step step, CancellationToken cancellationToken = default)
    {
        if (accessor == null) throw new ArgumentNullException(nameof(accessor));
        if (inputStream == null) throw new ArgumentNullException(nameof(inputStream));
        if (step == null) throw new ArgumentNullException(nameof(step));

        await using Stream writeStream = accessor.OpenWrite(outPath);

        // If input is not seekable, copy it to memory so we can compute length for progress.
        bool usedMemoryStream = false;
        MemoryStream? memorySource = null;
        Stream source = inputStream;

        if (!inputStream.CanSeek)
        {
            memorySource = new MemoryStream();
            await inputStream.CopyToAsync(memorySource, cancellationToken).ConfigureAwait(false);
            memorySource.Seek(0, SeekOrigin.Begin);
            source = memorySource;
            usedMemoryStream = true;
        }

        long totalBytes = source.CanSeek ? source.Length - source.Position : -1L;
        if (totalBytes <= 0L)
        {
            // Nothing or unknown length: finish quickly and set progress to done.
            step.Progress = 1.0f;
            if (usedMemoryStream && memorySource != null) memorySource.Dispose();
            return;
        }

        const int bufferSize = 81920;
        byte[] buffer = new byte[bufferSize];
        long transferred = 0L;
        int read;

        while ((read = await source.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false)) > 0)
        {
            await writeStream.WriteAsync(buffer, 0, read, cancellationToken).ConfigureAwait(false);
            transferred += read;

            // Scale progress from 0.5 -> 1.0 to match existing behavior.
            step.Progress = 0.5f + Math.Min(1.0f, (float)transferred / totalBytes) * 0.5f;
        }

        await writeStream.FlushAsync(cancellationToken).ConfigureAwait(false);
        step.Progress = 1.0f;

        if (usedMemoryStream && memorySource != null) memorySource.Dispose();
    }
}