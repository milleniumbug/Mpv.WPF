using System;
using System.Runtime.InteropServices;
using System.Windows.Interop;

namespace Mpv.WPF
{
	internal class MpvPlayerHwndHost : HwndHost
	{
		private NET.Mpv mpv;

		private const int WS_CHILD		= 0x40000000;
		private const int WS_VISIBLE	= 0x10000000;
		private const int HOST_ID		= 0x00000002;

		public MpvPlayerHwndHost(NET.Mpv mpv)
		{
			Guard.AgainstNull(mpv, nameof(mpv));

			this.mpv = mpv;
		}

		protected override HandleRef BuildWindowCore(HandleRef hwndParent)
		{
			// Create the child window that will host the
			// mpv player.
			var playerHostPtr = WinFunctions.CreateWindowEx(0,
															"static",
															"",
															WS_CHILD | WS_VISIBLE,
															0,
															0,
															100,
															100,
															hwndParent.Handle,
															(IntPtr)HOST_ID,
															IntPtr.Zero,
															0);

			// Set the mpv parent.
			var playerHostPtrLong = playerHostPtr.ToInt64();
			mpv.SetPropertyLong("wid", playerHostPtrLong);

			return new HandleRef(this, playerHostPtr);
		}

		protected override void DestroyWindowCore(HandleRef hwnd)
		{
			WinFunctions.DestroyWindow(hwnd.Handle);
		}
	}
}