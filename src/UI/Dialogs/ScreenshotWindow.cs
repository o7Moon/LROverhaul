using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using Gwen;
using Gwen.Controls;
using linerider.Tools;
using linerider.Utils;
using linerider.IO;
using linerider.Drawing;

namespace linerider.UI
{
    public class ScreenshotWindow : DialogBase
    {
        private RichLabel _descriptionlabel;
        private Label _error;
        private MainWindow _game;
        private const string howto = "You are about to export a capture of this current track.\n" +
            "It will be located in your line rider user directory (Documents/LRA/Renders).\n\n" +
            "This may take a few seconds for very high-resolution captures." +
            "The window will become unresponsive during this time.\n\n" +
            "If the image fails to record properly, try a smaller resolution.";

        private int lockW, lockH; //The width & height when fixed aspect ratio was enabled

        public ScreenshotWindow(GameCanvas parent, Editor editor, MainWindow window) : base(parent, editor)
        {
            _game = window;
            Title = "Export Screenshot";
            _descriptionlabel = new RichLabel(this)
            {
                AutoSizeToContents = true
            };
            if (!SafeFrameBuffer.CanRecord)
            {
                _descriptionlabel.AddText(
                    "Screenshot export is not supported on this machine.\n\nSorry.",
                    Skin.Colors.Text.Foreground);
            }
            else
            {
                _descriptionlabel.AddText(howto, Skin.Colors.Text.Foreground);
            }
            _descriptionlabel.Dock = Dock.Top;
            _error = new Label(this)
            {
                Dock = Dock.Top,
                TextColor = Color.Red,
                IsHidden = true,
                Margin = new Margin(0, 0, 0, 10)
            };
            AutoSizeToContents = true;
            MinimumSize = new Size(400, 300);
            MakeModal(true);
            Setup();
        }
        private void SetError(string error)
        {
            _error.IsHidden = false;
            _error.Text = "\n" + error;
        }
        private CheckProperty AddPropertyCheckbox(PropertyTable prop, string label, bool value)
        {
            var check = new CheckProperty(null);
            prop.Add(label, check);
            check.IsChecked = value;
            
            return check;
        }
        private void Setup()
        {
            Panel content = new Panel(this)
            {
                Dock = Dock.Fill,
                AutoSizeToContents = true,
                ShouldDrawBackground = false
            };
            Panel bottomrow = new Panel(content)
            {
                Dock = Dock.Bottom,
                AutoSizeToContents = true,
                ShouldDrawBackground = false,
            };
            PropertyTree proptree = new PropertyTree(content)
            {
                Dock = Dock.Top,
                AutoSizeToContents = true,
                Width = 200,
                Margin = new Margin(0, 0, 0, 10)
            };
            var table = proptree.Add("Output Settings", 150);

            var lockratiocheck = AddPropertyCheckbox(
               table,
               "Lock Aspect Ratio",
               Settings.ScreenshotLockRatio);

            lockratiocheck.ValueChanged += (o, e) =>
            {
                Settings.ScreenshotLockRatio = lockratiocheck.IsChecked;
                if (Settings.ScreenshotLockRatio)
                {
                    lockW = Settings.ScreenshotWidth;
                    lockH = Settings.ScreenshotHeight;
                }
            };

            var width = new NumberProperty(null)
            {
                Min = 1,
                Max = 50000,
                NumberValue = Settings.ScreenshotWidth,
                OnlyWholeNumbers = true
            };
            var height = new NumberProperty(null)
            {
                Min = 1,
                Max = 50000,
                NumberValue = Settings.ScreenshotHeight,
                OnlyWholeNumbers = true
            };

            table.Add("Width", width);
            table.Add("Height", height);

            width.ValueChanged += (o, e) =>
            {
                Settings.ScreenshotWidth = (int)width.NumberValue;
                if (Settings.ScreenshotLockRatio)
                {
                    Settings.ScreenshotLockRatio = false; //Setting this to false prevents the height value trying to update the width value again
                    height.NumberValue = Settings.ScreenshotWidth * lockH / lockW;
                    Settings.ScreenshotLockRatio = true;
                }
            };
            height.ValueChanged += (o, e) =>
            {
                Settings.ScreenshotHeight = (int)height.NumberValue;
                if (Settings.ScreenshotLockRatio)
                {
                    Settings.ScreenshotLockRatio = false;
                    width.NumberValue = Settings.ScreenshotHeight * lockW / lockH;
                    Settings.ScreenshotLockRatio = true;
                }
            };

            table = proptree.Add("Overlay settings", 150);
            var ppf = AddPropertyCheckbox(
                table,
                "Show P/f",
                Settings.ScreenshotShowPpf);
            ppf.ValueChanged += (o, e) => { Settings.ScreenshotShowPpf = ppf.IsChecked; Settings.Save(); };
            var fps = AddPropertyCheckbox(
                table,
                "Show FPS",
                Settings.ScreenshotShowFps);
            fps.ValueChanged += (o, e) => { Settings.ScreenshotShowFps = fps.IsChecked; Settings.Save(); };
            var tools = AddPropertyCheckbox(
                table,
                "Show Tools",
                Settings.ScreenshotShowTools);
            tools.ValueChanged += (o, e) => { Settings.ScreenshotShowTools = tools.IsChecked; Settings.Save(); };
            var hitTest = AddPropertyCheckbox(
               table,
               "Show Hit Test",
               Settings.ScreenshotShowHitTest);
            hitTest.ValueChanged += (o, e) => { Settings.ScreenshotShowHitTest = hitTest.IsChecked; Settings.Save(); };
            var resIndZoom = AddPropertyCheckbox(
                table,
                "Res-Independent Zoom",
                Settings.ScreenshotResIndependentZoom);
            resIndZoom.ValueChanged += (o, e) => { Settings.ScreenshotResIndependentZoom = resIndZoom.IsChecked; Settings.Save(); };
            proptree.ExpandAll();
            Button Cancel = new Button(bottomrow)
            {
                Dock = Dock.Right,
                Text = "Cancel",
                Margin = new Margin(10, 0, 0, 0),
            };
            Cancel.Clicked += (o, e) =>
            {
                Close();
            };
            Button ok = new Button(bottomrow)
            {
                Dock = Dock.Right,
                Text = "Export"
            };
            ok.Clicked += (o, e) =>
                {
                    Close();
                    Settings.Recording.ShowPpf = ppf.IsChecked;
                    Settings.Recording.ShowFps = fps.IsChecked;
                    Settings.Recording.ShowTools = tools.IsChecked;
                    Settings.Recording.ResIndZoom = resIndZoom.IsChecked;
                    Settings.Recording.ShowHitTest = hitTest.IsChecked;

                    Settings.Save();
                    Record();
                };
        }
        private void Record()
        {
            IO.TrackRecorder.RecordScreenshot(_game);
        }
    }
}
