using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Javi.FFmpeg
{
	/// <summary>
	///     Contains all Regex tasks
	/// </summary>
	internal static class RegexEngine
	{
		/// <summary>
		///     Dictionary containing every Regex test.
		/// </summary>
		private static Dictionary<Find, Regex> _expressions = new Dictionary<Find, Regex>
		{
			{ Find.Duration, new Regex(@"Duration: ([^,]*), ") },
			{ Find.ConvertProgressFrame, new Regex(@"frame=\s*([0-9]*)") },
			{ Find.ConvertProgressFps, new Regex(@"fps=\s*([0-9]*\.?[0-9]*?)") },
			{ Find.ConvertProgressSize, new Regex(@"size=\s*([0-9]*)kB") },
			{ Find.ConvertProgressFinished, new Regex(@"(muxing overhead: )([0-9]*\.?[0-9]*)%*") },
			{ Find.ConvertProgressTime, new Regex(@"time=\s*([^ ]*)") },
			{ Find.ConvertProgressBitrate, new Regex(@"bitrate=\s*([0-9]*\.?[0-9]*?)kbits/s") },
			{ Find.ConvertProgressSpeed, new Regex(@"speed=\s*([0-9]*\.?[0-9]*[e]*[+]*[0-9]*?)x") }
		};

		private enum Find
		{
			Duration,
			ConvertProgressSpeed,
			ConvertProgressBitrate,
			ConvertProgressFps,
			ConvertProgressFrame,
			ConvertProgressSize,
			ConvertProgressFinished,
			ConvertProgressTime,
		}

		internal static bool HasProgressData(string data)
		{
			return data.Contains("size") && data.Contains("time") && data.Contains("bitrate");
		}

		/// <summary>
		/// Establishes whether the data contains progress information.
		/// </summary>
		/// <param name="rawData">Event data from ffmpeg.</param>
		/// <param name="progressEventArgs">If successful, outputs a <see cref="FFmpegProgressEventArgs"/> which is generated from the data.</param>
		internal static ConversionProgessData GetProgressData(string rawData, ConversionInput inputData)
		{
			if (!HasProgressData(rawData))
			{
				return null;
			}

			var matchSize = _expressions[Find.ConvertProgressSize].Match(rawData);
			var matchTime = _expressions[Find.ConvertProgressTime].Match(rawData);
			var matchBitrate = _expressions[Find.ConvertProgressBitrate].Match(rawData);
			var matchFrame = _expressions[Find.ConvertProgressFrame].Match(rawData);
			var matchFps = _expressions[Find.ConvertProgressFps].Match(rawData);
			var matchSpeed = _expressions[Find.ConvertProgressSpeed].Match(rawData);
			TimeSpan.TryParse(matchTime.Groups[1].Value, out TimeSpan processedDuration);
			long.TryParse(matchFrame.Groups[1].Value, out long frame);
			double.TryParse(matchFps.Groups[1].Value, out double fps);
			int.TryParse(matchSize.Groups[1].Value, out int sizeKb);
			double.TryParse(matchBitrate.Groups[1].Value, out double bitrate);
			double.TryParse(matchSpeed.Groups[1].Value, out double speed);

			return new ConversionProgessData
			{
				OutputFile = inputData.OutputFile,
				InputFile = inputData.InputFile,
				CommandLine = inputData.CommandLine,
				ProcessedDuration = processedDuration,
				TotalDuration = TimeSpan.Zero,
				Frame = frame,
				Fps = fps,
				SizeKb = sizeKb,
				Bitrate = bitrate,
				Speed = speed
			};
		}

		internal static bool HasConversionCompleted(string data)
		{
			return data.Contains("muxing overhead");
		}

		/// <summary>
		/// Establishes whether the data indicates the conversion is complete.
		/// </summary>
		/// <param name="rawData">Event data from ffmpeg.</param>
		/// <param name="conversionCompleteEvent">If successful, outputs a <see cref="FFmpegCompletedEventArgs"/> which is generated from the data.</param>
		/// <returns></returns>
		internal static ConversionCompletedData GetConversionCompletedData(string rawData, ConversionInput inputData)
		{
			if (!HasConversionCompleted(rawData))
			{
				return null;
			}

			var matchFinished = _expressions[Find.ConvertProgressFinished].Match(rawData);
			double.TryParse(matchFinished.Groups[2].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out double muxingOverhead);

			return new ConversionCompletedData
			{
				CommandLine = inputData.CommandLine,
				InputFile = inputData.InputFile,
				OutputFile = inputData.OutputFile,
				MuxingOverhead = muxingOverhead,
				TotalDuration = GetMediaDuration(rawData)
			};
		}


		/// <summary>
		/// Check if data contains the media duration. If so return this duration.
		/// </summary>
		/// <param name="data">Event data from ffmpeg.</param>
		/// <param name="mediaDuration">Duration of the media.</param>
		/// <returns></returns>
		private static TimeSpan GetMediaDuration(string data)
		{
			var mediaDuration = TimeSpan.Zero;

			var match = _expressions[Find.Duration].Match(data);
			if (match.Success)
			{
				TimeSpan.TryParse(match.Groups[1].Value, out mediaDuration);
			}

			return mediaDuration;
		}
	}
}
