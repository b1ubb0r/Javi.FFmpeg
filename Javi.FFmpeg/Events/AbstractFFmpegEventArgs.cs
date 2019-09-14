using System;

namespace Javi.FFmpeg
{
	public abstract class AbstractFFmpegEventArgs : EventArgs
	{
		public AbstractFFmpegEventArgs(string inputFile, string outputFile, TimeSpan totalDuration)
		{
			InputFile = inputFile;
			OutputFile = outputFile;
			TotalDuration = totalDuration;
		}

		public string InputFile { get; internal set; }
		public string OutputFile { get; internal set; }
		public TimeSpan TotalDuration { get; internal set; }
	}
}
