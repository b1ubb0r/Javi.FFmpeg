using System;

namespace Javi.FFmpeg
{
	public class ConversionProgessData : ConversionInput
	{
		public TimeSpan TotalDuration { get; set; }
		public long? Frame { get; set; }
		public double? Fps { get; set; }
		public int? SizeKb { get; set; }
		public TimeSpan ProcessedDuration { get; set; }
		public double? Bitrate { get; set; }
		public double? Speed { get; set; }
	}
}
