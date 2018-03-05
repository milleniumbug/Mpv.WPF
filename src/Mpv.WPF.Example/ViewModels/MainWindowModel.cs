/*
	WPF Mpv user control example.
	Copyright(C) 2018 Aurel Hudec Jr

	This program is free software; you can redistribute it and/or modify
	it under the terms of the GNU General Public License as published by
	the Free Software Foundation; either version 2 of the License, or
	(at your option) any later version.

	This program is distributed in the hope that it will be useful,
	but WITHOUT ANY WARRANTY; without even the implied warranty of
	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
	GNU General Public License for more details.

	You should have received a copy of the GNU General Public License along
	with this program; if not, write to the Free Software Foundation, Inc.,
	51 Franklin Street, Fifth Floor, Boston, MA 02110-1301 USA. 
*/

using System;
using System.ComponentModel;

namespace Mpv.WPF.Example.ViewModels
{
	public class MainWindowModel : INotifyPropertyChanged
	{
		public TimeSpan Duration
		{
			get => duration;
			set
			{
				if (value != duration)
				{
					duration = value;
					NotifyPropertyChanged(nameof(Duration));
				}
			}
		}

		public TimeSpan Position
		{
			get => position;
			set
			{
				if (value != position)
				{
					position = value;
					NotifyPropertyChanged(nameof(Position));
				}
			}
		}

		public bool IsFileLoaded
		{
			get => isFileLoaded;
			set
			{
				if (value != isFileLoaded)
				{
					isFileLoaded = value;
					NotifyPropertyChanged(nameof(IsFileLoaded));
				}
			}
		}

		private TimeSpan duration;
		private TimeSpan position;

		private bool isFileLoaded;

		public event PropertyChangedEventHandler PropertyChanged;

		private void NotifyPropertyChanged(string propertyName)
		{
			var eventArgs = new PropertyChangedEventArgs(propertyName);
			PropertyChanged?.Invoke(this, eventArgs);
		}
	}
}