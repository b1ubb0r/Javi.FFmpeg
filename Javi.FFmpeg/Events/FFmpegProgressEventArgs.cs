using System;

namespace Javi.FFmpeg
{
	public class FFmpegProgressEventArgs : AbstractFFmpegEventArgs
	{
		/// <summary>
		/// Raises notifications on the conversion process
		/// </summary>
		/// <param name="inputFile">The input file.</param>
		/// <param name="outputFile">The output file.</param>
		/// <param name="processed">Duration of the media which has been processed</param>
		/// <param name="totalDuration">The total duration of the original media</param>
		/// <param name="frame">The specific frame the conversion process is on</param>
		/// <param name="fps">The frames converted per second</param>
		/// <param name="sizeKb">The current size in Kb of the converted media</param>
		/// <param name="bitrate">The bit rate of the converted media</param>
		/// <param name="speed">The speed.</param>
		public FFmpegProgressEventArgs(string inputFile, string outputFile, TimeSpan processed, TimeSpan totalDuration, long? frame = null, double? fps = null, int? sizeKb = null, double? bitrate = null, double? speed = null)
		: base(inputFile, outputFile, totalDuration)
		{
			ProcessedDuration = processed;
			Frame = frame;
			Fps = fps;
			SizeKb = sizeKb;
			Bitrate = bitrate;
			Speed = speed;
		}

		public FFmpegProgressEventArgs(ConversionProgessData progessData) : base(progessData.InputFile, progessData.OutputFile, progessData.TotalDuration)
		{
			ProcessedDuration = progessData.ProcessedDuration;
			Frame = progessData.Frame;
			Fps = progessData.Fps;
			SizeKb = progessData.SizeKb;
			Bitrate = progessData.Bitrate;
			Speed = progessData.Speed;
		}

		public long? Frame { get; internal set; }
		public double? Fps { get; internal set; }
		public int? SizeKb { get; internal set; }
		public TimeSpan ProcessedDuration { get; internal set; }
		public double? Bitrate { get; internal set; }
		public double? Speed { get; internal set; }
	}
}