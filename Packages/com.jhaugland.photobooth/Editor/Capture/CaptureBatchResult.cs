using System.Collections.Generic;

namespace Photobooth.Editor.Capture
{
    internal sealed class CaptureBatchResult
    {
        readonly IReadOnlyList<CaptureFileResult> files;

        internal IReadOnlyList<CaptureFileResult> Files => files;
        internal int CapturedCount { get; }
        internal int SkippedCount { get; }
        internal int FailedCount { get; }

        internal CaptureBatchResult(IReadOnlyList<CaptureFileResult> captureFiles)
        {
            files = captureFiles;
            foreach (CaptureFileResult file in files)
            {
                switch (file.Status)
                {
                    case CaptureFileStatus.Captured:
                        CapturedCount++;
                        break;
                    case CaptureFileStatus.SkippedExisting:
                        SkippedCount++;
                        break;
                    case CaptureFileStatus.Failed:
                        FailedCount++;
                        break;
                }
            }
        }
    }
}
