using Mpv.NET;
using Mpv.WPF.YouTubeDl;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Windows.Controls;

namespace Mpv.WPF
{
	/// <summary>
	/// User control containing an mpv player.
	/// </summary>
	public partial class MpvPlayer : UserControl
	{
		/// <summary>
		/// An instance of the underlying mpv API. Do not touch unless you know what you're doing.
		/// </summary>
		public NET.Mpv API => mpv;

		/// <summary>
		/// The desired video quality to retrieve when loading streams from video sites.
		/// </summary>
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

		/// <summary>
		/// Number of entries in the playlist.
		/// </summary>
		public int PlaylistEntryCount
		{
			get
			{
				lock (mpvLock)
				{
					return (int)mpv.GetPropertyLong("playlist-count");
				}
			}
		}

		/// <summary>
		/// Index of the current entry in the playlist. (zero based)
		/// </summary>
		public int PlaylistIndex
		{
			get
			{
				lock (mpvLock)
				{
					return (int)mpv.GetPropertyLong("playlist-pos");
				}
			}
		}

		/// <summary>
		/// If true, when media is loaded it will automatically play.
		/// </summary>
		public bool AutoPlay { get; set; }

		/// <summary>
		/// True when media is loaded and ready for playback.
		/// </summary>
		public bool IsMediaLoaded { get; private set; }

		/// <summary>
		/// True if media is playing.
		/// </summary>
		public bool IsPlaying { get; private set; }

		/// <summary>
		/// Duration of the media file. (As indicated by metadata)
		/// </summary>
		public TimeSpan Duration
		{
			get
			{
				if (!IsMediaLoaded)
					return TimeSpan.Zero;

				long durationSeconds;
				lock (mpvLock)
				{
					durationSeconds = mpv.GetPropertyLong("duration");
				}

				return TimeSpan.FromSeconds(durationSeconds);
			}
		}

		/// <summary>
		/// Time since the beginning of the media file.
		/// </summary>
		public TimeSpan Position
		{
			get
			{
				if (!IsMediaLoaded)
					return TimeSpan.Zero;

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

		/// <summary>
		/// Time left of playback in the media file.
		/// </summary>
		public TimeSpan Remaining
		{
			get
			{
				if (!IsMediaLoaded)
					return TimeSpan.Zero;

				long remainingSeconds;
				lock (mpvLock)
				{
					remainingSeconds = mpv.GetPropertyLong("time-remaining");
				}

				return TimeSpan.FromSeconds(remainingSeconds);
			}
		}

		/// <summary>
		/// Volume of the current media file. Ranging from 0 to 100 inclusive.
		/// </summary>
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

		private MpvPlayerHwndHost playerHwndHost;

		private YouTubeDlVideoQuality ytdlVideoQuality;

		private bool isYouTubeDlEnabled = false;
		private bool isSeeking = false;

		private readonly object mpvLock = new object();

		/// <summary>
		/// Creates an instance of MpvPlayer using a specific libmpv DLL.
		/// </summary>
		/// <param name="dllPath">Relative or absolute path to the libmpv DLL.</param>
		public MpvPlayer(string dllPath)
		{
			Guard.AgainstNullOrEmptyOrWhiteSpaceString(dllPath, nameof(dllPath));

			InitializeComponent();

			Dispatcher.ShutdownStarted += DispatcherOnShutdownStarted;

			InitialiseMpv(dllPath);

			// Set defaults.
			Volume = 50;
			YouTubeDlVideoQuality = YouTubeDlVideoQuality.Highest;

			SetMpvHost();
		}

		/// <summary>
		/// Loads the file at the path into mpv.
		/// If youtube-dl is enabled, this method can be used to load videos from video sites.
		/// </summary>
		/// <param name="path">Path or URL to a media file.</param>
		/// <param name="loadMethod">The way in which the given media file should be loaded.</param>
		public void Load(string path, MpvPlayerLoadMethod loadMethod = MpvPlayerLoadMethod.AppendPlay)
		{
			Guard.AgainstNullOrEmptyOrWhiteSpaceString(path, nameof(path));

			lock (mpvLock)
			{
				mpv.SetPropertyString("pause", AutoPlay ? "no" : "yes");

				var loadMethodString = GetStringForLoadMethod(loadMethod);

				mpv.Command("loadfile", path, loadMethodString);
			}
		}

		/// <summary>
		/// Resume playback.
		/// </summary>
		public void Resume()
		{
			lock (mpvLock)
			{
				mpv.SetPropertyString("pause", "no");
			}

			IsPlaying = true;
		}

		/// <summary>
		/// Pause playback.
		/// </summary>
		public void Pause()
		{
			lock (mpvLock)
			{
				mpv.SetPropertyString("pause", "yes");
			}

			IsPlaying = false;
		}

		/// <summary>
		/// Stop playback and unload the media file.
		/// </summary>
		public void Stop()
		{
			lock (mpvLock)
			{
				mpv.Command("stop");
			}

			IsMediaLoaded = false;
			IsPlaying = false;
		}

		/// <summary>
		/// Goes to the start of the media file and resumes playback.
		/// </summary>
		public void Restart()
		{
			Position = TimeSpan.Zero;

			Resume();
		}

		/// <summary>
		/// Go to the next entry in the playlist.
		/// </summary>
		/// <returns>True if successful, false if not. False indicates that there are no entries after the current entry.</returns>
		public bool PlaylistNext()
		{
			try
			{
				lock (mpvLock)
				{
					mpv.Command("playlist-next");
				}

				return true;
			}
			catch (MpvException exception)
			{
				return HandleCommandMpvException(exception);
			}
		}

		/// <summary>
		/// Go to the previous entry in the playlist.
		/// </summary>
		/// <returns>True if successful, false if not. False indicates that there are no entries before the current entry.</returns>
		public bool PlaylistPrevious()
		{
			try
			{
				lock (mpvLock)
				{
					mpv.Command("playlist-prev");
				}

				return true;
			}
			catch (MpvException exception)
			{
				return HandleCommandMpvException(exception);
			}
		}

		/// <summary>
		/// Remove the current entry from the playlist.
		/// </summary>
		/// <returns>True if removed, false if not.</returns>
		public bool PlaylistRemove()
		{
			try
			{
				lock (mpvLock)
				{
					mpv.Command("playlist-remove", "current");
				}

				return true;
			}
			catch (MpvException exception)
			{
				return HandleCommandMpvException(exception);
			}
		}

		/// <summary>
		/// Removes a specific entry in the playlist, indicated by an index.
		/// </summary>
		/// <param name="index">Zero based index to an entry in the playlist.</param>
		/// <returns>True if removed, false if not.</returns>
		public bool PlaylistRemove(int index)
		{
			var indexString = index.ToString();

			try
			{
				lock (mpvLock)
				{
					mpv.Command("playlist-remove", indexString);
				}

				return true;
			}
			catch (MpvException exception)
			{
				return HandleCommandMpvException(exception);
			}
		}

		/// <summary>
		/// Moves the playlist entry at oldIndex to newIndex. This does not swap the entries.
		/// </summary>
		/// <param name="oldIndex">Index of the entry you want to move.</param>
		/// <param name="newIndex">Index of where you want to move the entry.</param>
		/// <returns>True if moved, false if not.</returns>
		public bool PlaylistMove(int oldIndex, int newIndex)
		{
			var oldIndexString = oldIndex.ToString();
			var newIndexString = newIndex.ToString();
			try
			{
				lock (mpvLock)
				{
					mpv.Command("playlist-move", oldIndexString, newIndexString);
				}

				return true;
			}
			catch (MpvException exception)
			{
				return HandleCommandMpvException(exception);
			}
		}

		/// <summary>
		/// Clear the playlist of all entries,
		/// </summary>
		public void PlaylistClear()
		{
			lock (mpvLock)
			{
				mpv.Command("playlist-clear");
			}
		}

		/// <summary>
		/// Enable youtube-dl functionality in mpv.
		/// </summary>
		/// <param name="ytdlHookScriptPath">Relative or absolute path to the "ytdl_hook.lua" script.</param>
		public void EnableYouTubeDl(string ytdlHookScriptPath)
		{
			if (isYouTubeDlEnabled)
				return;

			Guard.AgainstNullOrEmptyOrWhiteSpaceString(ytdlHookScriptPath, nameof(ytdlHookScriptPath));

			lock (mpvLock)
			{
				mpv.Command("load-script", ytdlHookScriptPath);
			}

			isYouTubeDlEnabled = true;
		}

		private void InitialiseMpv(string dllPath)
		{
			mpv = new NET.Mpv(dllPath);

			mpv.PlaybackRestart += MpvOnPlaybackRestart;
			mpv.Seek += MpvOnSeek;

			mpv.FileLoaded += MpvOnFileLoaded;
			mpv.EndFile += MpvOnEndFile;

#if DEBUG
			mpv.LogMessage += MpvOnLogMessage;

			mpv.RequestLogMessages(MpvLogLevel.Info);
#endif
		}

		private void SetMpvHost()
		{
			// Create the HwndHost and add it to the user control.
			playerHwndHost = new MpvPlayerHwndHost(mpv);
			AddChild(playerHwndHost);
		}

		private void MpvOnPlaybackRestart(object sender, EventArgs e)
		{
			if (isSeeking)
			{
				MediaEndedSeeking?.Invoke(this, EventArgs.Empty);
				isSeeking = false;
			}
		}

		private void MpvOnSeek(object sender, EventArgs e)
		{
			isSeeking = true;
			MediaStartedSeeking?.Invoke(this, EventArgs.Empty);
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

		private string GetStringForLoadMethod(MpvPlayerLoadMethod loadMethod)
		{
			switch (loadMethod)
			{
				case MpvPlayerLoadMethod.Replace:
					return "replace";
				case MpvPlayerLoadMethod.Append:
					return "append";
				case MpvPlayerLoadMethod.AppendPlay:
					return "append-play";
				default:
					throw new ArgumentException("Invalid load method.", nameof(loadMethod));
			}
		}

		private static bool HandleCommandMpvException(MpvException exception)
		{
			if (exception.Error == MpvError.Command)
				return false;
			else
				throw exception;
		}

		private void DispatcherOnShutdownStarted(object sender, EventArgs e)
		{
			mpv.Dispose();
			playerHwndHost.Dispose();
		}
	}
}