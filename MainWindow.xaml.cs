using FFmpegWinUI.Page;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Windows;
//using System.Windows.Controls;
// Removed the incorrect using directive for System.Windows.Controls
// and replaced it with the correct namespace for WinUI controls.

using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace FFmpegWinUI
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        // 声明 SettingsNavItem 字段用于引用“设置”导航项
        private NavigationViewItem? SettingsNavItem;

        public MainWindow()
        {
            this.InitializeComponent();

            // 初始化 SettingsNavItem
            SettingsNavItem = FindSettingsNavItem();

            // 确保在设置事件处理器之前 NavigationView 已经初始化
            if (nvSample != null)
            {
                nvSample.SelectionChanged += NvSample_SelectionChanged;
                // 默认导航到主页，但要确保页面类型存在
                contentFrame.Navigate(typeof(Home));
            }
        }

        // 查找 Tag="Settings" 的 NavigationViewItem
        private NavigationViewItem? FindSettingsNavItem()
        {
            // 检查 FooterMenuItems
            foreach (var obj in nvSample.FooterMenuItems)
            {
                if (obj is NavigationViewItem item && (item.Tag?.ToString() == "Settings"))
                    return item;
            }
            // 检查 MenuItems（如果你把设置放在主菜单）
            foreach (var obj in nvSample.MenuItems)
            {
                if (obj is NavigationViewItem item && (item.Tag?.ToString() == "Settings"))
                    return item;
            }
            return null;
        }

        private void NvSample_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            try
            {
                if (args.SelectedItem is NavigationViewItem item)
                {
                    var pageTag = item.Tag?.ToString();
                    Type pageType = pageTag switch
                    {
                        "Home" => typeof(Home),
                        "Settings" => typeof(Settings),
                        "Transcoder" => typeof(Transcoder), 
                        "Info" => typeof(Info),
                        
                        _ => typeof(Home)  // 默认导航到主页
                    };

                    // 确保 contentFrame 不为空
                    if (contentFrame != null)
                    {
                        // 使用 try-catch 包装导航操作
                        try
                        {
                            contentFrame.Navigate(pageType);
                        }
                        catch (Exception ex)
                        {
                            // 导航失败时的处理
                            System.Diagnostics.Debug.WriteLine($"Navigation failed: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // 处理选择更改事件中的异常
                System.Diagnostics.Debug.WriteLine($"Selection changed error: {ex.Message}");
            }
        }

        // 添加一个方法用于控制 InfoBadge 显示
        public void SetSettingsInfoBadgeVisible(bool visible)
        {
            if (SettingsNavItem != null && SettingsNavItem.InfoBadge is InfoBadge badge)    
            {
                badge.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
            }
        }
    }
}
