using SkiaSharp.Views.Desktop;
using SkiaSharp;
using System.IO;
using System.Windows;
using System.Windows.Forms.Integration;

namespace TrainingAppV3
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 
    public partial class MainWindow : Window
    {
        private SKGLControl _glControl;
        private SKBitmap? _bitmap;          // 原圖
        private SKPoint _translate = new SKPoint(0, 0);
        private bool _isDragging = false;
        private System.Drawing.Point _lastMousePos;

        private float _logicScale = 1.0f;
        private float _renderScale = 1.0f;
        private bool _useLowRes = false;

        private SKImage? _highResImage;
        private SKImage? _lowResImage;

        private const float _minScale = 0.02f;  // 最小縮小比例
        private const float _maxScale = 10.0f;  // 最大放大比例

        public MainWindow()
        {
            InitializeComponent();

            var host = new WindowsFormsHost();
            _glControl = new SKGLControl();
            host.Child = _glControl;
            MainGrid.Children.Add(host);

            // 事件
            _glControl.PaintSurface += Gl_PaintSurface;
            _glControl.MouseWheel += Gl_MouseWheel;
            _glControl.MouseDown += Gl_MouseDown;
            _glControl.MouseMove += Gl_MouseMove;
            _glControl.MouseUp += Gl_MouseUp;

            LoadImage("C:\\workspace\\test.jpg");
            CreateRenderTarget();
        }

        private void LoadImage(string path)
        {
            using var stream = File.OpenRead(path);
            var bmp = SKBitmap.Decode(stream);
            if (bmp != null)
            {
                _highResImage = SKImage.FromBitmap(bmp);
                // 建立低解析度版本
                int w = bmp.Width / 6;
                int h = bmp.Height / 6;
                var lowBmp = bmp.Resize(new SKImageInfo(w, h), SKFilterQuality.Low);
                if (lowBmp != null)
                    _lowResImage = SKImage.FromBitmap(lowBmp);
            }
        }

        // 將原圖快取到 RenderTarget / SKImage
        private void CreateRenderTarget()
        {
            if (_bitmap == null) return;

            using var surface = SKSurface.Create(new SKImageInfo(_bitmap.Width, _bitmap.Height));
            var canvas = surface.Canvas;
            canvas.Clear(SKColors.Transparent);
            canvas.DrawBitmap(_bitmap, 0, 0);
        }

        private void Gl_PaintSurface(object sender, SKPaintGLSurfaceEventArgs e)
        {
            var canvas = e.Surface.Canvas;
            canvas.Clear(SKColors.Black);

            if (_useLowRes && _lowResImage != null)
            {
                DrawImage(canvas, _lowResImage);
            }
            else if (_highResImage != null)
            {
                DrawImage(canvas, _highResImage);
            }
        }

        private void DrawImage(SKCanvas canvas, SKImage img)
        {
            canvas.Save();
            canvas.Translate(_glControl.Width / 2f + _translate.X,
                             _glControl.Height / 2f + _translate.Y);
            canvas.Scale(_renderScale);

            var paint = new SKPaint
            {
                FilterQuality = _useLowRes ? SKFilterQuality.Low : SKFilterQuality.High
            };

            canvas.DrawImage(
                img,
                new SKRect(-_highResImage!.Width / 2f, -_highResImage.Height / 2f,
                           _highResImage.Width / 2f, _highResImage.Height / 2f),
                paint
            );

            canvas.Restore();
        }

        private void Gl_MouseWheel(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (_highResImage == null) return;

            // 記錄滑鼠對圖片座標
            float mouseX = (e.X - (_glControl.Width / 2f + _translate.X)) / _renderScale + _highResImage.Width / 2f;
            float mouseY = (e.Y - (_glControl.Height / 2f + _translate.Y)) / _renderScale + _highResImage.Height / 2f;
            
            // 對數縮放
            float zoomDelta = e.Delta > 0 ? 0.1f : -0.1f;
            _logicScale *= MathF.Exp(zoomDelta);

            // 限制縮放範圍
            _logicScale = Math.Clamp(_logicScale, _minScale, _maxScale);

            // 高低解析度切換只影響畫質，不改變縮放
            float lowResThreshold = 0.3f;
            _useLowRes = _lowResImage != null && _logicScale < lowResThreshold;

            // renderScale 直接跟邏輯縮放一致
            _renderScale = _logicScale;

            // 保持滑鼠焦點
            _translate.X = e.X - _glControl.Width / 2f - (mouseX - _highResImage.Width / 2f) * _renderScale;
            _translate.Y = e.Y - _glControl.Height / 2f - (mouseY - _highResImage.Height / 2f) * _renderScale;

            _glControl.Invalidate();
        }

        private void Gl_MouseDown(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Left)
            {
                _isDragging = true;
                _lastMousePos = e.Location;
            }
        }

        private void Gl_MouseUp(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Left)
                _isDragging = false;
        }

        private void Gl_MouseMove(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (_isDragging)
            {
                _translate.X += e.X - _lastMousePos.X;
                _translate.Y += e.Y - _lastMousePos.Y;
                _lastMousePos = e.Location;

                _glControl.Invalidate();
            }
        }
    }
}