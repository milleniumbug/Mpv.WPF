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

using Mpv.WPF.Example.ViewModels;
using Mpv.WPF.YouTubeDl;
using System;
using System.Windows;
using System.Windows.Threading;

namespace Mpv.WPF.Example
{
	public partial class MainWindow : Window
	{
		private readonly MainWindowModel model = new MainWindowModel();

		private DispatcherTimer positionUpdateTimer;

		private MpvPlayer player;

		private bool isMovingPositionSlider = false;

		public MainWindow()
		{
			InitializeComponent();
		}

		private void WindowOnLoaded(object sender, RoutedEventArgs e)
		{
			DataContext = model;

			SetupMpvPlayer();

			SetupPositionUpdateTimer();
		}

		private void SetupMpvPlayer()
		{
			player = new MpvPlayer(@"lib\mpv-1.dll");
			player.MediaLoaded += PlayerOnFileLoaded;
			player.MediaUnloaded += PlayerOnFileUnloaded;

			playerHost.Children.Add(player);

			player.EnableYouTubeDl(@"scripts\ytdl_hook.lua");
			player.YouTubeDlVideoQuality = YouTubeDlVideoQuality.MediumHigh;

			player.AutoPlay = true;
			player.Load(@"https://www.youtube.com/watch?v=jrVbawRPO7I");
			player.Load(@"https://www.youtube.com/watch?v=vggNhNiFo5Q");
		}

		private void SetupPositionUpdateTimer()
		{
			positionUpdateTimer = new DispatcherTimer
			{
				Interval = TimeSpan.FromMilliseconds(500)
			};
			positionUpdateTimer.Tick += PositionUpdateTimerOnTick;
			positionUpdateTimer.Start();
		}

		private void PlayerOnFileUnloaded(object sender, EventArgs e)
		{
			model.IsMediaLoaded = false;
		}

		private void PlayerOnFileLoaded(object sender, EventArgs e)
		{
			model.IsMediaLoaded = true;

			model.Duration = player.Duration;
		}

		private void ButtonPlayOnClick(object sender, RoutedEventArgs e)
		{
			if (player.IsFinished)
				player.Restart();

			player.Resume();
		}

		private void ButtonPauseOnClick(object sender, RoutedEventArgs e)
		{
			player.Pause();
		}

		private void ButtonStopOnClick(object sender, RoutedEventArgs e)
		{
			player.Stop();
		}

		private void SliderOnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			if (IsLoaded)
			{
				var newVolume = (int)e.NewValue;

				player.Volume = newVolume;
			}
		}

		private void PositionUpdateTimerOnTick(object sender, EventArgs e)
		{
			if (!isMovingPositionSlider && model.IsMediaLoaded)
				positionSlider.Value = player.Position.TotalSeconds;
		}

		private void PositionSliderOnMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
		{
			if (isMovingPositionSlider)
				model.Position = TimeSpan.FromSeconds(positionSlider.Value);
		}

		private void PositionSliderOnPreviewMouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
		{
			positionSlider.Value = model.Position.TotalSeconds;
			player.Position = model.Position;

			isMovingPositionSlider = false;
		}

		private void PositionSliderOnPreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
		{
			isMovingPositionSlider = true;
		}
	}
}