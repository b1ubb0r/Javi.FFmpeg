using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Javi.FFmpeg
{
	public class FFmpeg : IDisposable
	{
		/// <summary>
		/// The ffmpeg executable full path
		/// </summary>
		protected readonly string FFmpegPath;

		/// <summary>
		/// The ffmpeg process
		/// </summary>
		protected Process FFmpegProcess;

		/// <summary>
		/// The standard arguments to pass to ffmpeg:
		/// -nostdin                Disable interaction on standard input.
		/// -y                      Overwrite output files without asking.
		/// -loglevel info          Set logging level
		/// </summary>
		private readonly string _standardArguments = "-nostdin -y -loglevel info ";

		/// <summary>
		/// Event fired when ffmpeg is done processing.
		/// </summary>
		public event EventHandler<FFmpegCompletedEventArgs> OnCompleted;

		/// <summary>
		/// Event for every line of output from ffmpeg when processing.
		/// </summary>
		public event EventHandler<FFmpegRawDataEventArgs> OnData;

		/// <summary>
		/// Event fired for progress in ffmpeg process.
		/// </summary>
		public event EventHandler<FFmpegProgressEventArgs> OnProgress;

		/// <summary>
		/// Initializes a new instance of the <see cref="FFmpeg" /> class.
		/// </summary>
		/// <param name="ffmpegPath">Full path to ffmpeg executable.</param>

		public FFmpeg(string ffmpegPath)
		{
			FFmpegPath = ffmpegPath;

			if (!File.Exists(FFmpegPath))
			{
				throw new FFmpegNotFoundException(FFmpegPath);
			}
		}

		/// <summary>
		/// Call ffmpeg using a custom command.
		/// The ffmpegCommandLine must be a command line ffmpeg can process, including the input file, output file and parameters.
		/// </summary>
		/// <param name="inputFile">The input file.</param>
		/// <param name="ffmpegCommandLine">The ffmpeg commandline parameters.</param>
		/// <param name="cancellationToken">The cancellation token to cancel a running ffmpeg process.</param>
		/// <exception cref="ArgumentNullException">When ffmpegCommand is null or whitespace or empty.</exception>
		public async Task Run(string inputFile, string outputFile, string ffmpegCommandLine, CancellationToken cancellationToken = default)
		{
			if (string.IsNullOrWhiteSpace(ffmpegCommandLine))
			{
				throw new ArgumentNullException("ffmpegCommand");
			}

			await StartConversion(new ConversionInput { CommandLine = ffmpegCommandLine, InputFile = inputFile, OutputFile = outputFile }, cancellationToken);
		}

		/// <summary>
		/// Extracts the subtitle.
		/// </summary>
		/// <param name="inputFile">The input file.</param>
		/// <param name="outputFile">The output file.</param>
		/// <param name="subtitleTrack">The subtitle text stream to extract. This number is zero based. Omit to extract the first subtitle stream.</param>
		/// <param name="cancellationToken">The cancellation token to cancel a running ffmpeg process.</param>
		public async Task ExtractSubtitle(string inputFile, string outputFile, int subtitleTrack = 0, CancellationToken cancellationToken = default)
		{
			await Run(inputFile, outputFile, string.Format($"-i \"{inputFile}\" -vn -an -map 0:s:{subtitleTrack} -c:s:0 srt \"{outputFile}\""), cancellationToken);
		}

		/// <summary>
		/// Retrieve a thumbnail image from a video file.
		/// </summary>
		/// <param name="inputFile">Video file.</param>
		/// <param name="outputFile">Image file.</param>
		/// <param name="seekPosition">The seek position.</param>
		/// <param name="cancellationToken">The cancellation token to cancel a running ffmpeg process.</param>
		public async Task GetThumbnail(string inputFile, string outputFile, TimeSpan seekPosition, CancellationToken cancellationToken = default)
		{
			await Run(inputFile, outputFile,
				((FormattableString)$"-ss {seekPosition.TotalSeconds} -i \"{inputFile}\" -vframes 1  \"{outputFile}\"").ToString(CultureInfo.InvariantCulture),
				cancellationToken);
		}

		/// <summary>
		/// Cuts the media.
		/// </summary>
		/// <param name="inputFile">The input file.</param>
		/// <param name="outputFile">The output file.</param>
		/// <param name="start">The starttime in seconds.</param>
		/// <param name="end">The endtime in seconds.</param>
		/// <param name="cancellationToken">The cancellation token to cancel a running ffmpeg process.</param>
		public async Task CutMedia(string inputFile, string outputFile, TimeSpan start, TimeSpan end, CancellationToken cancellationToken = default)
		{
			await Run(inputFile, outputFile,
				((FormattableString)$"-ss {start} -to {end} -i \"{inputFile}\" -map 0:v? -c copy  -map 0:a? -c copy -map 0:s? -c copy \"{outputFile}\"").ToString(CultureInfo.InvariantCulture),
				cancellationToken);
		}

		/// <summary>
		/// Converts the audio to ac-3
		/// </summary>
		/// <param name="inputFile">The input file.</param>
		/// <param name="outputFile">The output file.</param>
		/// <param name="audioTrack">The audio track.</param>
		/// <param name="bitRate">The bit rate.</param>
		/// <param name="samplingRate">The sampling rate.</param>
		/// <param name="cancellationToken">The cancellation token to cancel a running ffmpeg process.</param>
		public async Task ConvertAudioToAC3(string inputFile, string outputFile, int audioTrack, int bitRate, int samplingRate, CancellationToken cancellationToken = default)
		{
			await Run(inputFile, outputFile,
				$" -hwaccel auto -i \"{inputFile}\" -map {audioTrack} -c:s copy -c:v copy -c:a ac3 -b:a {bitRate} -ar {samplingRate} \"{outputFile}\"",
				cancellationToken);
		}

		/// <summary>
		/// Converts the video to avc/h264
		/// </summary>
		/// <param name="inputFile">The input file.</param>
		/// <param name="outputFile">The output file.</param>
		/// <param name="videoTrack">The video track.</param>
		/// <param name="cancellationToken">The cancellation token to cancel a running ffmpeg process.</param>
		public async Task ConvertVideoToAVC(string inputFile, string outputFile, int videoTrack, CancellationToken cancellationToken = default)
		{
			await Run(inputFile, outputFile,
				$" -hwaccel auto -i \"{inputFile}\" -map {videoTrack} -c:a copy -c:s copy -c:v libx264 \"{outputFile}\"",
				cancellationToken);
		}

		private Task StartConversion(ConversionInput input, CancellationToken cancellationToken)
		{
			return Task.Run(() =>
			{
				var receivedMessagesLog = new List<string>();
				Exception caughtException = null;

				var processStartInfo = GenerateStartInfo(input.CommandLine);

				OnData?.Invoke(this, new FFmpegRawDataEventArgs(input.CommandLine));

				using (FFmpegProcess = Process.Start(processStartInfo))
				{
					FFmpegProcess.ErrorDataReceived += (sender, received) =>
					{
						if (received.Data == null)
						{
							return;
						}

						try
						{
							receivedMessagesLog.Insert(0, received.Data);

							OnData?.Invoke(this, new FFmpegRawDataEventArgs(received.Data));

							if (RegexEngine.HasProgressData(received.Data))
							{
								OnProgress?.Invoke(this, new FFmpegProgressEventArgs(RegexEngine.GetProgressData(received.Data, input)));
							}
							else if (RegexEngine.HasConversionCompleted(received.Data))
							{
								OnCompleted?.Invoke(this, new FFmpegCompletedEventArgs(RegexEngine.GetConversionCompletedData(received.Data, input)));
							}
						}
						catch (Exception ex)
						{
							// catch the exception and kill the process since we're in a faulted state
							caughtException = ex;

							try
							{
								FFmpegProcess.Kill();
							}
							catch (InvalidOperationException)
							{
								// Swallow exceptions that are thrown when killing the process, ie. the application is ending naturally before we get a chance to kill it.
							}
						}
					};

					FFmpegProcess.BeginErrorReadLine();
					if (cancellationToken != null)
					{
						while (!FFmpegProcess.WaitForExit(100))
						{
							if (!cancellationToken.IsCancellationRequested)
							{
								continue;
							}

							try
							{
								FFmpegProcess.Kill();
							}
							catch (Win32Exception)
							{
								// The associated process could not be terminated or the process is terminating.
							}
						}
					}
					else
					{
						FFmpegProcess.WaitForExit();
					}

					if (cancellationToken.IsCancellationRequested)
					{
						cancellationToken.ThrowIfCancellationRequested();
					}

					if (FFmpegProcess.ExitCode == 0 && caughtException == null)
					{
						return;
					}

					if (FFmpegProcess.ExitCode != 1 && receivedMessagesLog.Count >= 2)
					{
						throw new FFmpegException(FFmpegProcess.ExitCode + ": " + receivedMessagesLog[1] + receivedMessagesLog[0], caughtException);
					}
					else
					{
						throw new FFmpegException(string.Format($"ffmpeg exited with exitcode {FFmpegProcess.ExitCode}"), caughtException);
					}
				}
			});
		}

		private ProcessStartInfo GenerateStartInfo(string arguments)
		{
			return new ProcessStartInfo
			{
				Arguments = _standardArguments + arguments,
				FileName = FFmpegPath,
				CreateNoWindow = true,
				RedirectStandardInput = false,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				UseShellExecute = false,
				WindowStyle = ProcessWindowStyle.Hidden
			};
		}

		private bool _isdisposed = false;

		/// <summary>
		/// Finalizes an instance of the <see cref="FFmpeg"/> class.
		/// </summary>
		~FFmpeg()
		{
			Dispose(false);
		}

		/// <summary>
		/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
		/// </summary>
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Releases unmanaged and - optionally - managed resources.
		/// </summary>
		/// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
		protected virtual void Dispose(bool disposing)
		{
			if (!_isdisposed)
			{
				if (FFmpegProcess != null)
				{
					FFmpegProcess.Dispose();
				}
				FFmpegProcess = null;

				_isdisposed = true;
			}
		}
	}
}