using System;

namespace Javi.FFmpeg
{
    /// <summary>
    /// Raw data from the running ffmpeg process as output by ffmpeg.
    /// </summary>
    public class FFmpegRawDataEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FFmpegRawDataEventArgs"/> class.
        /// </summary>
        /// <param name="data">The data.</param>
        public FFmpegRawDataEventArgs(string data)
        {
            this.Data = data;
        }

        public string Data { get; set; }
    }
}
