using System;

namespace Javi.FFmpeg
{
	public class ConversionCompletedData : ConversionInput
	{
		public TimeSpan TotalDuration { get; set; }
		public double MuxingOverhead { get; internal set; }
	}
}
