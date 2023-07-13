﻿//  Author:
//       Noah Ablaseau <nablaseauhotmail.com>
//
//  Copyright (c) 2017 
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.

using Gwen;
using Gwen.Controls;
using Gwen.Skin;
using linerider.Tools;
using linerider.UI;
using linerider.UI.Components;
using linerider.Utils;
using OpenTK;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;

namespace linerider
{
    public class GameCanvas : Canvas
    {
        public static readonly Queue<Action> QueuedActions = new Queue<Action>();
        public ZoomSlider ZoomSlider;
        public Gwen.Renderer.OpenTK Renderer;
        private InfoBarCoords _infobarcoords;
        private TimelineWidget _timeline;
        private SwatchBar _swatchbar;
        private Toolbar _toolbar;
        private LoadingSprite _loadingsprite;
        private readonly MainWindow game;
        public PlatformImpl Platform;
        public bool Loading { get; set; }
        public Color TextForeground => Settings.NightMode ? Skin.Colors.Text.Highlight : Skin.Colors.Text.Foreground;
        public bool IsModalOpen => Children.FirstOrDefault(x => x is Gwen.ControlInternal.Modal) != null;
        private Tooltip _usertooltip;
        public bool Scrubbing => _timeline.Playhead.Held;
        public readonly Fonts Fonts;

        public GameCanvas(
            SkinBase skin,
            MainWindow Game,
            Gwen.Renderer.OpenTK renderer,
            Fonts fonts) : base(skin)
        {
            game = Game;
            Fonts = fonts;
            Renderer = renderer;
            Platform = new PlatformImpl(Game);
            Gwen.Platform.Neutral.Implementation = Platform;
            CreateUI();
            OnThink += Think;
        }
        private void Think(object sender, EventArgs e)
        {
            // Process recording junk
            bool rec = Settings.Local.RecordingMode;
            ZoomSlider.IsHidden = rec || !Settings.UIShowZoom;
            _toolbar.IsHidden = rec && !Settings.Recording.ShowTools;
            _swatchbar.IsHidden = rec || !CurrentTools.CurrentTool.ShowSwatch;
            _infobarcoords.IsHidden = rec || !Settings.Editor.ShowCoordinateMenu;
            _timeline.IsHidden = rec;

            _loadingsprite.IsHidden = rec || !Loading;
            Tool selectedtool = CurrentTools.CurrentTool;
            _usertooltip.IsHidden = !(selectedtool.Active && selectedtool.Tooltip != "");
            if (!_usertooltip.IsHidden)
            {
                if (_usertooltip.Text != selectedtool.Tooltip)
                {
                    _usertooltip.Text = selectedtool.Tooltip;
                    _usertooltip.Layout();
                }
                Point mousePos = Gwen.Input.InputHandler.MousePosition;
                Rectangle bounds = _usertooltip.Bounds;
                Rectangle offset = Util.FloatRect(
                    mousePos.X - bounds.Width * 0.5f,
                    mousePos.Y - bounds.Height - 10,
                    bounds.Width,
                    bounds.Height);
                offset = Util.ClampRectToRect(offset, Bounds);
                _usertooltip.SetPosition(offset.X, offset.Y);
            }
        }
        private void CreateUI()
        {

            _usertooltip = new Tooltip(this) { IsHidden = true };
            ZoomSlider = new ZoomSlider(this, game.Track);
            _timeline = new TimelineWidget(this, game.Track);

            ControlBase leftPanel = new Panel(this)
            {
                Margin = new Margin(WidgetContainer.WidgetMargin, WidgetContainer.WidgetMargin, 0, 0),
                ShouldDrawBackground = false,
                MouseInputEnabled = false,
                AutoSizeToContents = true,
                Dock = Dock.Left,
            };
            _ = new InfoBarLeft(leftPanel, game.Track)
            {
                Dock = Dock.Top,
            };
            _infobarcoords = new InfoBarCoords(leftPanel)
            {
                Dock = Dock.Top,
                Margin = new Margin(0, WidgetContainer.WidgetMargin, 0, 0),
            };

            WidgetContainer middlePanel = new WidgetContainer(this)
            {
                AutoSizeToContents = true,
                Positioner = (o) => new Point(Width / 2 - o.Width / 2, WidgetContainer.WidgetMargin),
            };
            _toolbar = new Toolbar(middlePanel, game)
            {
                Dock = Dock.Top,
            };
            _swatchbar = new SwatchBar(middlePanel, game.Track)
            {
                AutoSizeToContents = true,
                Dock = Dock.Left,
            };

            ControlBase rightPanel = new Panel(this)
            {
                Margin = new Margin(0, WidgetContainer.WidgetMargin, WidgetContainer.WidgetMargin, 0),
                ShouldDrawBackground = false,
                MouseInputEnabled = false,
                AutoSizeToContents = true,
                Dock = Dock.Right,
            };
            _ = new InfoBarRight(rightPanel, game.Track)
            {
                Dock = Dock.Top,
            };

            _loadingsprite = new LoadingSprite(this)
            {
                Positioner = (o) => new Point(
                    middlePanel.X + middlePanel.Width,
                    middlePanel.Y + WidgetContainer.WidgetPadding
                ),
            };
        }
        protected override void OnChildAdded(ControlBase child)
        {
            if (child is Gwen.ControlInternal.Modal || child is WindowControl)
            {
                CurrentTools.CurrentTool.Stop();
            }
        }
        public override void Think()
        {
            while (QueuedActions.Count != 0)
            {
                QueuedActions.Dequeue().Invoke();
            }
            base.Think();
        }
        public static void OpenUrl(string url)
        {
            try
            {
                _ = Process.Start(url);
            }
            catch
            {
                // Hack because of this: https://github.com/dotnet/corefx/issues/10361
                if (Configuration.RunningOnWindows)
                {
                    url = url.Replace("&", "^&");
                    _ = Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true });
                }
                else if (Configuration.RunningOnMacOS)
                {
                    _ = Process.Start("open", url);
                }
                else if (Configuration.RunningOnLinux)
                {
                    _ = Process.Start("xdg-open", url);
                }
                else
                {
                    throw;
                }
            }
        }
        public void RefreshCursors()
        {
            game.Cursors.Reload();
            game.Cursors.Refresh(this);
        }
        public void ShowChangelog()
        {
            if (Settings.showChangelog != true)
            {
                return;
            }
            ShowDialog(new ChangelogWindow(this, game.Track));
        }

        public static bool ShowLoadCrashBackup(string name)
        {
            bool ret = false;
            string text = "" +
                "Hey, it looks like you are trying to load a Crash Backup.\n" +
                "(" + name + ")\n" +
                "Some issues with the save may cause the file to always crash this program.\n" +
                "Are you sure you want to load it?";
            string title = "So about that crash backup...";

            if (System.Windows.Forms.MessageBox.Show(text, title,
                System.Windows.Forms.MessageBoxButtons.YesNo)
                 == System.Windows.Forms.DialogResult.Yes)
            {
                ret = true;
                Settings.LastSelectedTrack = "";
                Settings.Save();
            }
            return ret;
        }

        public void ShowOutOfDate()
        {
            if (Program.NewVersion == null)
                return;
            MessageBox window = MessageBox.Show(this, "Would you like to download the latest version?", "Update Available! v" + Program.NewVersion, MessageBox.ButtonType.OkCancel);
            window.RenameButtons("Go to Download");
            window.Dismissed += (o, e) =>
            {
                if (window.Result == DialogResult.OK)
                {
                    try
                    {
                        OpenUrl($"{Constants.GithubPageHeader}/releases/latest");
                    }
                    catch
                    {
                        ShowError("Unable to open your browser.");
                    }
                }
            };
            Program.NewVersion = null;
        }
        public void ShowError(string message) => MessageBox.Show(this, message, "Error!");
        public List<ControlBase> GetOpenWindows()
        {
            List<ControlBase> ret = new List<ControlBase>();
            foreach (ControlBase child in Children)
            {
                if (child is WindowControl)
                {
                    ret.Add(child);
                }
                else if (child is Gwen.ControlInternal.Modal)
                {
                    foreach (ControlBase modalchild in child.Children)
                    {
                        if (modalchild is WindowControl w)
                        {
                            ret.Add(w);
                        }
                    }
                }
            }
            return ret;
        }
        public override void Dispose()
        {
            Fonts.Dispose();
            base.Dispose();
        }
        private void ShowDialog(WindowControl window)
        {
            if (game.Track.Playing)
                game.Track.TogglePause();
            game.StopTools();
            window.ShowCentered();
        }
        public void ShowSaveDialog() => ShowDialog(new SaveWindow(this, game.Track));
        public void ShowLoadDialog() => ShowDialog(new LoadWindow(this, game.Track));
        public void ShowPreferencesDialog() => ShowDialog(new PreferencesWindow(this, game.Track));
        public void ShowTrackPropertiesDialog() => ShowDialog(new TrackInfoWindow(this, game.Track));
        public void ShowTriggerWindow() => ShowDialog(new TriggerWindow(this, game.Track));
        public void ShowExportVideoWindow()
        {
            if (File.Exists(IO.ffmpeg.FFMPEG.ffmpeg_path))
            {
                ShowDialog(new ExportWindow(this, game.Track, game));
            }
            else
            {
                ShowffmpegMissing();
            }
        }
        public void ShowScreenCaptureWindow() => ShowDialog(new ScreenshotWindow(this, game.Track, game));
        public void ShowGameMenuWindow() => ShowDialog(new GameMenuWindow(this, game.Track));
        public void ShowffmpegMissing()
        {
            MessageBox mbox = MessageBox.Show(
                this,
                "This feature requires ffmpeg for encoding.\n" +
                "Automatically download it?",
                "ffmpeg not found",
                MessageBox.ButtonType.YesNo,
                true,
                true);
            mbox.Dismissed += (o, e) =>
              {
                  if (e == DialogResult.Yes)
                  {
                      ShowDialog(new FFmpegDownloadWindow(this, game.Track));
                  }
              };
        }
        public void ShowLineWindow(Game.GameLine line, int x, int y)
        {
            LineWindow wnd = new LineWindow(this, game.Track, line);
            ShowDialog(wnd);
            wnd.SetPosition(x - wnd.Width / 2, y - wnd.Height / 2);
        }
        public void ShowGeneratorWindow(Vector2d pos) => ShowDialog(new GeneratorWindow(this, pos));
    }
}