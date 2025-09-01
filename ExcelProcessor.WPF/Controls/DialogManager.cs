using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace ExcelProcessor.WPF.Controls
{
    public class DialogManager
    {
        private static Grid _overlayGrid;
        private static ConfigDetailDialog _currentDialog;

        public static void Initialize(Grid overlayGrid)
        {
            _overlayGrid = overlayGrid;
        }

            public static void ShowDialog(ConfigDetailDialog dialog, Action onSave = null, Action onTest = null, Action onCancel = null)
    {
        if (_overlayGrid == null)
        {
            throw new InvalidOperationException("DialogManager not initialized. Call Initialize() first.");
        }

        // 清除之前的对话框
        if (_currentDialog != null)
        {
            _overlayGrid.Children.Remove(_currentDialog);
        }

        _currentDialog = dialog;

        // 设置对话框位置
        Grid.SetRowSpan(dialog, Math.Max(1, _overlayGrid.RowDefinitions.Count));
        Grid.SetColumnSpan(dialog, Math.Max(1, _overlayGrid.ColumnDefinitions.Count));

        // 注册事件 - 确保每个事件只注册一次
        if (onSave != null)
        {
            dialog.SaveClicked += (s, e) => 
            {
                try
                {
                    onSave.Invoke();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Save callback error: {ex.Message}");
                }
                finally
                {
                    CloseDialog();
                }
            };
        }
        
        if (onTest != null)
        {
            dialog.TestClicked += (s, e) => 
            {
                try
                {
                    onTest.Invoke();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Test callback error: {ex.Message}");
                }
                finally
                {
                    CloseDialog();
                }
            };
        }
        
        if (onCancel != null)
        {
            dialog.CancelClicked += (s, e) => 
            {
                try
                {
                    onCancel.Invoke();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Cancel callback error: {ex.Message}");
                }
                finally
                {
                    CloseDialog();
                }
            };
        }

        dialog.CloseClicked += (s, e) => CloseDialog();

        // 显示遮罩层
        var overlay = _overlayGrid.Parent as Border;
        if (overlay != null)
        {
            overlay.Visibility = Visibility.Visible;
        }

        // 添加到网格
        _overlayGrid.Children.Add(dialog);

        // 显示动画
        dialog.Opacity = 0;
        dialog.RenderTransform = new System.Windows.Media.ScaleTransform(0.8, 0.8);

        var opacityAnimation = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(200));
        var scaleAnimation = new DoubleAnimation(0.8, 1.0, TimeSpan.FromMilliseconds(200));

        dialog.BeginAnimation(UIElement.OpacityProperty, opacityAnimation);
        dialog.RenderTransform.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleXProperty, scaleAnimation);
        dialog.RenderTransform.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleYProperty, scaleAnimation);
    }

            public static void CloseDialog()
    {
        if (_currentDialog == null || _overlayGrid == null) return;

        // 关闭动画
        var opacityAnimation = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(150));
        var scaleAnimation = new DoubleAnimation(1.0, 0.8, TimeSpan.FromMilliseconds(150));

        opacityAnimation.Completed += (s, e) =>
        {
            _overlayGrid.Children.Remove(_currentDialog);
            _currentDialog = null;
            
            // 隐藏遮罩层
            var overlay = _overlayGrid.Parent as Border;
            if (overlay != null)
            {
                overlay.Visibility = Visibility.Collapsed;
            }
        };

        _currentDialog.BeginAnimation(UIElement.OpacityProperty, opacityAnimation);
        _currentDialog.RenderTransform.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleXProperty, scaleAnimation);
        _currentDialog.RenderTransform.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleYProperty, scaleAnimation);
    }

        public static bool IsDialogOpen => _currentDialog != null;
    }
} 