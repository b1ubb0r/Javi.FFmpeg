using System;

namespace Javi.FFmpeg
{
	public class FFmpegCompletedEventArgs : AbstractFFmpegEventArgs
	{

		public FFmpegCompletedEventArgs(ConversionCompletedData data) : base(data.InputFile, data.OutputFile, data.TotalDuration)
		{
			MuxingOverhead = data.MuxingOverhead;
		}
		/// <summary>
		/// Raises notification once conversion is complete
		/// </summary>
		/// <param name="InputFile">The input file.</param>
		/// <param name="OutputFile">The output file.</param>
		/// <param name="totalDuration">The total duration of the original media</param>
		/// <param name="muxingOverhead">The muxing overhead.</param>
		public FFmpegCompletedEventArgs(string inputFile, string outputFile, TimeSpan totalDuration, double muxingOverhead) : base(inputFile, outputFile, totalDuration)
		{
			MuxingOverhead = muxingOverhead;
		}

		public double MuxingOverhead { get; internal set; }
	}
}