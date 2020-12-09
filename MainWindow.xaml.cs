using GameOverlay.Drawing;
using GameOverlay.Windows;
using Newtonsoft.Json;
using NHotkey.Wpf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Color = GameOverlay.Drawing.Color;
using Geometry = GameOverlay.Drawing.Geometry;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using Line = GameOverlay.Drawing.Line;
using Rectangle = GameOverlay.Drawing.Rectangle;

namespace GameBlocker
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Dictionary<string, SolidBrush> _brushes;
        private Dictionary<string, Font> _fonts;

        private Geometry _gridGeometry;
        private Rectangle _gridBounds;

        private Random _random;
        private long _lastRandomSet;
        private List<Action<Graphics, float, float>> _randomFigures;

        GraphicsWindow _window;
        List<Key> keys = new List<Key>();
        List<Key> lockedKeys = new List<Key>();
        public MainWindow()
        {
            InitializeComponent();
            try
            {
                lockedKeys = JsonConvert.DeserializeObject<List<Key>>(Properties.Settings.Default.KeyGesture) ?? new List<Key>();
                ShowLocked();
            } catch (Exception e)
            {

            }
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            notifyIcon.Visibility = Visibility.Visible;
            Visibility = Visibility.Hidden;

            _brushes = new Dictionary<string, SolidBrush>();
            _fonts = new Dictionary<string, Font>();

            var gfx = new Graphics()
            {
                MeasureFPS = true,
                PerPrimitiveAntiAliasing = true,
                TextAntiAliasing = true
            };

            _window = new GraphicsWindow(0, 0, (int)System.Windows.SystemParameters.PrimaryScreenWidth, (int)System.Windows.SystemParameters.PrimaryScreenHeight, gfx)
            {
                FPS = 60,
                IsTopmost = true,
                IsVisible = false
            };

            _window.DestroyGraphics += _window_DestroyGraphics;
            _window.DrawGraphics += _window_DrawGraphics;
            _window.SetupGraphics += _window_SetupGraphics;

            _window.Create();
            //var kg = new KeyGesture((Key)(Keys.Control | Keys.Alt | Keys.Tab));
            var key = (Key)0;
            var modifiers = (ModifierKeys)0;
            if (lockedKeys.Contains(Key.LeftCtrl) || lockedKeys.Contains(Key.RightCtrl)) modifiers = modifiers | ModifierKeys.Control;
            if (lockedKeys.Contains(Key.LeftAlt) || lockedKeys.Contains(Key.RightAlt)) modifiers = modifiers | ModifierKeys.Alt;
            if (lockedKeys.Contains(Key.LeftShift) || lockedKeys.Contains(Key.RightShift)) modifiers = modifiers | ModifierKeys.Shift;
            foreach (var k in lockedKeys.Where(x => x != Key.LeftCtrl && x != Key.RightCtrl && x != Key.LeftAlt && x != Key.RightAlt && x != Key.LeftShift && x != Key.RightShift))
            {
                key = key | k;
            }

            var kg = new KeyGesture(key,modifiers);
            HotkeyManager.Current.AddOrReplace("Toggle", kg, (obj,e) =>
            {
                _window.IsVisible = !_window.IsVisible;
            });

        }

        private void _window_SetupGraphics(object sender, SetupGraphicsEventArgs e)
        {
            var gfx = e.Graphics;

            if (e.RecreateResources)
            {
                foreach (var pair in _brushes) pair.Value.Dispose();
            }

            _brushes["black"] = gfx.CreateSolidBrush(0, 0, 0);
            _brushes["background"] = gfx.CreateSolidBrush(0x33, 0x36, 0x3F);

            if (e.RecreateResources) return;
        }

        private void _window_DestroyGraphics(object sender, DestroyGraphicsEventArgs e)
        {
            foreach (var pair in _brushes) pair.Value.Dispose();
        }

        private void _window_DrawGraphics(object sender, DrawGraphicsEventArgs e)
        {
            var gfx = e.Graphics;
            gfx.ClearScene(_brushes["background"]);
            gfx.DrawBox2D(_brushes["black"], _brushes["black"], 0, 0, _window.Width, _window.Height, 0);
        }

        private void textBox_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {

        }

        private void textBox_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.LeftCtrl:
                case Key.RightCtrl:
                case Key.LeftAlt:
                case Key.RightAlt:
                case Key.LeftShift:
                case Key.RightShift:
                    UpdateKeys(keys);
                    break;
                default:
                    UpdateKeys(keys);
                    break;
            }
            keys.Add(e.Key);
            e.Handled = true;
        }

        private void textBox_KeyUp(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.LeftCtrl:
                case Key.RightCtrl:
                case Key.LeftAlt:
                case Key.RightAlt:
                case Key.LeftShift:
                case Key.RightShift:
                    keys.Remove(e.Key);
                    break;
                default:
                    LockIn(keys);
                    break;
            }
            keys.Remove(e.Key);
            if (keys.Count == 0) ShowLocked();
            e.Handled = true;
        }

        private void LockIn(List<Key> keys)
        {
            var text = new List<string>();
            if (keys.Contains(Key.LeftCtrl) || keys.Contains(Key.RightCtrl)) text.Add("CTRL");
            if (keys.Contains(Key.LeftAlt) || keys.Contains(Key.RightAlt)) text.Add("ALT");
            if (keys.Contains(Key.LeftShift) || keys.Contains(Key.RightShift)) text.Add("SHIFT");
            foreach (var key in keys.Where(x => x != Key.LeftCtrl && x != Key.RightCtrl && x != Key.LeftAlt && x != Key.RightAlt && x != Key.LeftShift && x != Key.RightShift))
            {
                text.Add(key.ToString());
            }
            textBox.Text = string.Join("+", text.ToArray());
            lockedKeys = new List<Key>(keys.ToArray());
            Properties.Settings.Default.KeyGesture = JsonConvert.SerializeObject(lockedKeys);
            Properties.Settings.Default.Save();
        }

        private void ShowLocked()
        {
            var text = new List<string>();
            if (lockedKeys.Contains(Key.LeftCtrl) || lockedKeys.Contains(Key.RightCtrl)) text.Add("CTRL");
            if (lockedKeys.Contains(Key.LeftAlt) || lockedKeys.Contains(Key.RightAlt)) text.Add("ALT");
            if (lockedKeys.Contains(Key.LeftShift) || lockedKeys.Contains(Key.RightShift)) text.Add("SHIFT");
            foreach (var key in lockedKeys.Where(x => x != Key.LeftCtrl && x != Key.RightCtrl && x != Key.LeftAlt && x != Key.RightAlt && x != Key.LeftShift && x != Key.RightShift))
            {
                text.Add(key.ToString());
            }
            textBox.Text = string.Join("+", text.ToArray());
        }

        private void UpdateKeys(List<Key> keys)
        {
            var text =  new List<string>();
            if (keys.Contains(Key.LeftCtrl) || keys.Contains(Key.RightCtrl)) text.Add("CTRL");
            if (keys.Contains(Key.LeftAlt) || keys.Contains(Key.RightAlt)) text.Add("ALT");
            if (keys.Contains(Key.LeftShift) || keys.Contains(Key.RightShift)) text.Add("SHIFT");
            foreach(var key in keys.Where(x=> x != Key.LeftCtrl && x != Key.RightCtrl && x != Key.LeftAlt && x != Key.RightAlt && x != Key.LeftShift && x != Key.RightShift)) {
                text.Add(key.ToString());
            }
            text.Add("...");
            textBox.Text = string.Join("+", text.ToArray());
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Application.Current.Shutdown();
        }

        private void MenuItem_Click_1(object sender, RoutedEventArgs e)
        {
            _window.IsVisible = !_window.IsVisible;
        }
    }
}
