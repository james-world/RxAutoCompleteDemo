using System;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace RxAutoCompleteDemo
{
    public static class ScaleToWindowSizeBehavior
    {
        #region ParentWindow

        public static readonly DependencyProperty ParentWindowProperty =
            DependencyProperty.RegisterAttached("ParentWindow",
                                                 typeof(Window),
                                                 typeof(ScaleToWindowSizeBehavior),
                                                 new FrameworkPropertyMetadata(null, OnParentWindowChanged));

        public static void SetParentWindow(FrameworkElement element, Window value)
        {
            element.SetValue(ParentWindowProperty, value);
        }

        public static Window GetParentWindow(FrameworkElement element)
        {
            return (Window)element.GetValue(ParentWindowProperty);
        }

        private static void OnParentWindowChanged(DependencyObject target,
                                                  DependencyPropertyChangedEventArgs e)
        {
            FrameworkElement mainElement = (FrameworkElement)target;
            Window window = e.NewValue as Window;

            ScaleTransform scaleTransform = new ScaleTransform();
            scaleTransform.CenterX = 0;
            scaleTransform.CenterY = 0;
            Binding scaleValueBinding = new Binding
            {
                Source = window,
                Path = new PropertyPath(ScaleValueProperty)
            };
            BindingOperations.SetBinding(scaleTransform, ScaleTransform.ScaleXProperty, scaleValueBinding);
            BindingOperations.SetBinding(scaleTransform, ScaleTransform.ScaleYProperty, scaleValueBinding);
            mainElement.LayoutTransform = scaleTransform;
            mainElement.SizeChanged += mainElement_SizeChanged;
            CalculateScale(window, mainElement);
        }

        #endregion // ParentWindow

        #region ScaleValue

        public static readonly DependencyProperty ScaleValueProperty =
            DependencyProperty.RegisterAttached("ScaleValue",
                                                typeof(double),
                                                typeof(ScaleToWindowSizeBehavior),
                                                new UIPropertyMetadata(1.0, OnScaleValueChanged, OnCoerceScaleValue));

        public static double GetScaleValue(DependencyObject target)
        {
            return (double)(target.GetValue(ScaleValueProperty) ?? 1.0);
        }

        public static void SetScaleValue(DependencyObject target, double value)
        {
            target.SetValue(ScaleValueProperty, value);
        }

        private static void OnScaleValueChanged(DependencyObject target, DependencyPropertyChangedEventArgs e)
        {
        }

        private static object OnCoerceScaleValue(DependencyObject d, object baseValue)
        {
            if (!(baseValue is double value)) return 1.0f;

            if (double.IsNaN(value))
            {
                return 1.0f;
            }
            value = Math.Max(0.1, value);
            return value;
        }

        private static void mainElement_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            FrameworkElement mainElement = sender as FrameworkElement;
            Window window = GetParentWindow(mainElement);
            CalculateScale(window, mainElement);
        }

        private static void CalculateScale(Window window, FrameworkElement mainElement)
        {
            Size denominators = GetDenominators(mainElement);
            double xScale = window.ActualWidth / denominators.Width;
            double yScale = window.ActualHeight / denominators.Height;
            double value = Math.Min(xScale, yScale);
            SetScaleValue(window, value);
        }

        #endregion // ScaleValue

        #region Denominators

        public static readonly DependencyProperty DenominatorsProperty =
            DependencyProperty.RegisterAttached("Denominators",
                                                typeof(Size),
                                                typeof(ScaleToWindowSizeBehavior),
                                                new UIPropertyMetadata(new Size(1000.0, 700.0)));

        public static Size GetDenominators(DependencyObject target)
        {
            return (Size)(target?.GetValue(DenominatorsProperty) ?? throw new ApplicationException("Expected Denominators"));
        }

        public static void SetDenominators(DependencyObject target, Size value)
        {
            target.SetValue(DenominatorsProperty, value);
        }

        #endregion // Denominators
    }
}
