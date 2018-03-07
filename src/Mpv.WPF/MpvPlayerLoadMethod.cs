namespace Mpv.WPF
{
	public enum MpvPlayerLoadMethod
	{
		/// <summary>
		/// Stop playback of current media and start new one.
		/// </summary>
		Replace,

		/// <summary>
		/// Append media to playlist.
		/// </summary>
		Append,

		/// <summary>
		/// Append media to playlist and play if nothing is playing start playback.
		/// </summary>
		AppendPlay
	}
}