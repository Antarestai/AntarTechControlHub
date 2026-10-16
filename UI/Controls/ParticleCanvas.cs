using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace AntarTechControl.UI.Controls
{
    public class ParticleCanvas : FrameworkElement
    {
        private class Particle
        {
            public double X;
            public double Y;
            public double Radius;
            public double SpeedY;
            public double DriftSpeed;
            public double DriftOffset;
            public Brush Brush = Brushes.Transparent;
        }

        private readonly List<Particle> _particles = new();
        private readonly Random _rand = new();
        private DateTime _lastRenderTime = DateTime.UtcNow;
        private bool _isRunning = false;

        private static readonly Color[] ParticleColors = new[]
        {
            Color.FromArgb(90, 0, 195, 255),    // AntarTech Cyan
            Color.FromArgb(70, 56, 189, 248),   // Sky Blue
            Color.FromArgb(50, 99, 102, 241),   // Indigo Glow
            Color.FromArgb(40, 14, 165, 233),   // Deep Cyan
            Color.FromArgb(110, 255, 255, 255)  // Soft White Glint
        };

        public ParticleCanvas()
        {
            IsHitTestVisible = false;
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
            SizeChanged += OnSizeChanged;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (!_isRunning)
            {
                _isRunning = true;
                _lastRenderTime = DateTime.UtcNow;
                CompositionTarget.Rendering += OnRendering;
                InitializeParticles();
            }
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            if (_isRunning)
            {
                _isRunning = false;
                CompositionTarget.Rendering -= OnRendering;
            }
        }

        private void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (ActualWidth > 50 && ActualHeight > 50 && _particles.Count == 0)
            {
                InitializeParticles();
            }
        }

        private void InitializeParticles()
        {
            double w = ActualWidth > 50 ? ActualWidth : 1200;
            double h = ActualHeight > 50 ? ActualHeight : 800;

            _particles.Clear();
            int count = 35; // Lightweight, luxurious ambient feel

            for (int i = 0; i < count; i++)
            {
                var color = ParticleColors[_rand.Next(ParticleColors.Length)];
                var brush = new SolidColorBrush(color);
                brush.Freeze();

                _particles.Add(new Particle
                {
                    X = _rand.NextDouble() * w,
                    Y = _rand.NextDouble() * h,
                    Radius = 1.2 + _rand.NextDouble() * 3.0,
                    SpeedY = 12.0 + _rand.NextDouble() * 26.0,
                    DriftSpeed = 0.5 + _rand.NextDouble() * 1.2,
                    DriftOffset = _rand.NextDouble() * Math.PI * 2,
                    Brush = brush
                });
            }
        }

        private void OnRendering(object? sender, EventArgs e)
        {
            if (!_isRunning || ActualWidth <= 0 || ActualHeight <= 0) return;

            DateTime now = DateTime.UtcNow;
            double dt = (now - _lastRenderTime).TotalSeconds;
            _lastRenderTime = now;

            if (dt > 0.1) dt = 0.1; // Clamp large frame jumps

            double w = ActualWidth;
            double h = ActualHeight;

            foreach (var p in _particles)
            {
                p.Y -= p.SpeedY * dt;
                p.X += Math.Sin(now.TimeOfDay.TotalSeconds * p.DriftSpeed + p.DriftOffset) * (8.0 * dt);

                if (p.Y < -10)
                {
                    p.Y = h + 10;
                    p.X = _rand.NextDouble() * w;
                }
                if (p.X < -10) p.X = w + 10;
                else if (p.X > w + 10) p.X = -10;
            }

            InvalidateVisual();
        }

        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);

            // Draw each floating particle
            foreach (var p in _particles)
            {
                dc.DrawEllipse(p.Brush, null, new Point(p.X, p.Y), p.Radius, p.Radius);
            }
        }
    }
}
