using Mpv.NET;
using Mpv.WPF.YouTubeDl;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Windows.Controls;

namespace Mpv.WPF
{
	public partial class MpvPlayer : UserControl
	{
		public NET.Mpv API => mpv;

		public YouTubeDlVideoQuality YouTubeDlVideoQuality
		{
			get => ytdlVideoQuality;
			set
			{
				var formatString = YouTubeDlHelper.GetFormatStringForVideoQuality(value);

				lock (mpvLock)
				{
					mpv.SetPropertyString("ytdl-format", formatString);
				}

				ytdlVideoQuality = value;
			}
		}

		// Media-related properties

		public bool AutoPlay { get; set; }

		public bool IsMediaLoaded { get; private set; }

		public bool IsPlaying { get; private set; }

		public bool IsFinished
		{
			get
			{
				GuardAgainstNotLoaded();

				return Position == Duration;
			}
		}

		public TimeSpan Duration
		{
			get
			{
				GuardAgainstNotLoaded();

				long durationSeconds;
				lock (mpvLock)
				{
					durationSeconds = mpv.GetPropertyLong("duration");
				}

				return TimeSpan.FromSeconds(durationSeconds);
			}
		}

		public TimeSpan Position
		{
			get
			{
				GuardAgainstNotLoaded();

				long positionSeconds;
				lock (mpvLock)
				{
					positionSeconds = mpv.GetPropertyLong("time-pos");
				}

				return TimeSpan.FromSeconds(positionSeconds);
			}
			set
			{
				GuardAgainstNotLoaded();

				var totalSeconds = value.TotalSeconds;

				var totalSecondsString = totalSeconds.ToString(CultureInfo.InvariantCulture);

				lock (mpvLock)
				{
					mpv.Command("seek", totalSecondsString, "absolute");
				}
			}
		}

		public TimeSpan Remaining
		{
			get
			{
				GuardAgainstNotLoaded();

				long remainingSeconds;
				lock (mpvLock)
				{
					remainingSeconds = mpv.GetPropertyLong("time-remaining");
				}

				return TimeSpan.FromSeconds(remainingSeconds);
			}
		}

		public int Volume
		{
			get
			{
				lock (mpvLock)
				{
					return (int)mpv.GetPropertyDouble("volume");
				}
			}
			set
			{
				if (value < 0 || value > 100)
					throw new ArgumentOutOfRangeException("Volume should be between 0 and 100.");

				lock (mpvLock)
				{
					mpv.SetPropertyDouble("volume", value);
				}
			}
		}

		public event EventHandler MediaLoaded;
		public event EventHandler MediaUnloaded;
		public event EventHandler MediaError;
		public event EventHandler MediaStartedSeeking;
		public event EventHandler MediaEndedSeeking;

		private NET.Mpv mpv;

		private YouTubeDlVideoQuality ytdlVideoQuality;

		private readonly MpvPlayerHwndHost playerHwndHost;

		private bool isYouTubeDlEnabled = false;
		private bool isSeeking = false;

		private readonly object mpvLock = new object();

		public MpvPlayer(string dllPath)
		{
			Guard.AgainstNullOrEmptyOrWhiteSpaceString(dllPath, nameof(dllPath));

			InitializeComponent();

			Dispatcher.ShutdownStarted += DispatcherOnShutdownStarted;

			InitialiseMpv(dllPath);

			// Set defaults.
			Volume = 50;
			YouTubeDlVideoQuality = YouTubeDlVideoQuality.Highest;

			// Create the HwndHost and add it to the user control.
			playerHwndHost = new MpvPlayerHwndHost(mpv);
			AddChild(playerHwndHost);
		}

		public void Load(string path)
		{
			Guard.AgainstNullOrEmptyOrWhiteSpaceString(path, nameof(path));

			lock (mpvLock)
			{
				mpv.SetPropertyString("pause", AutoPlay ? "no" : "yes");

				mpv.Command("loadfile", path);
			}
		}

		public void Resume()
		{
			lock (mpvLock)
			{
				mpv.SetPropertyString("pause", "no");
			}

			IsPlaying = false;
		}

		public void Pause()
		{
			lock (mpvLock)
			{
				mpv.SetPropertyString("pause", "yes");
			}

			IsPlaying = true;
		}

		public void Stop()
		{
			lock (mpvLock)
			{
				mpv.Command("stop");
			}

			IsMediaLoaded = false;
			IsPlaying = false;
		}

		public void Restart()
		{
			Position = TimeSpan.Zero;

			Resume();

			IsPlaying = true;
		}

		public void EnableYouTubeDl(string ytdlHookScriptPath)
		{
			if (isYouTubeDlEnabled)
				return;

			Guard.AgainstNullOrEmptyOrWhiteSpaceString(ytdlHookScriptPath, nameof(ytdlHookScriptPath));

			lock (mpvLock)
			{
				// Load youtube-dl hook script.
				mpv.Command("load-script", ytdlHookScriptPath);
			}

			isYouTubeDlEnabled = true;
		}

		private void InitialiseMpv(string dllPath)
		{
			mpv = new NET.Mpv(dllPath);
			mpv.FileLoaded += MpvOnFileLoaded;
			mpv.EndFile += MpvOnEndFile;
			mpv.PlaybackRestart += MpvOnPlayBackRestart;
			mpv.Seek += MpvOnSeek;

#if DEBUG
			mpv.LogMessage += MpvOnLogMessage;

			mpv.RequestLogMessages(MpvLogLevel.Info);
#endif

			// Keep the video player open.
			mpv.SetPropertyString("keep-open", "always");
		}

		private void MpvOnFileLoaded(object sender, EventArgs e)
		{
			IsMediaLoaded = true;

			IsPlaying = AutoPlay;

			MediaLoaded?.Invoke(this, EventArgs.Empty);
		}

		private void MpvOnEndFile(object sender, MpvEndFileEventArgs e)
		{
			IsMediaLoaded = false; 

			var eventEndFile = e.EventEndFile;

			switch (eventEndFile.Reason)
			{
				case MpvEndFileReason.EndOfFile:
				case MpvEndFileReason.Stop:
				case MpvEndFileReason.Quit:
				case MpvEndFileReason.Redirect:
					MediaUnloaded?.Invoke(this, EventArgs.Empty);
					break;
				case MpvEndFileReason.Error:
					MediaError?.Invoke(this, EventArgs.Empty);
					break;
			}
		}

		private void MpvOnPlayBackRestart(object sender, EventArgs e)
		{
			if (isSeeking)
				MediaEndedSeeking?.Invoke(this, EventArgs.Empty);
		}

		private void MpvOnSeek(object sender, EventArgs e)
		{
			isSeeking = true;
			MediaStartedSeeking?.Invoke(this, EventArgs.Empty);
		}

#if DEBUG
		private void MpvOnLogMessage(object sender, MpvLogMessageEventArgs e)
		{
			var message = e.Message;

			var prefix = message.Prefix;
			var text = message.Text;

			Debug.Write($"[{prefix}] {text}");
		}
#endif

		private void GuardAgainstNotLoaded()
		{
			if (!IsMediaLoaded)
				throw new InvalidOperationException("Operation could not be completed because no media file has been loaded.");
		}

		private void DispatcherOnShutdownStarted(object sender, EventArgs e)
		{
			mpv.Dispose();
			playerHwndHost.Dispose();
		}
	}
}