using FFmpegFreeUI.Page;
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

namespace FFmpegFreeUI
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        // ���� SettingsNavItem �ֶ��������á����á�������
        private NavigationViewItem? SettingsNavItem;

        public MainWindow()
        {
            this.InitializeComponent();

            // ��ʼ�� SettingsNavItem
            SettingsNavItem = FindSettingsNavItem();

            // ȷ���������¼�������֮ǰ NavigationView �Ѿ���ʼ��
            if (nvSample != null)
            {
                nvSample.SelectionChanged += NvSample_SelectionChanged;
                // Ĭ�ϵ�������ҳ����Ҫȷ��ҳ�����ʹ���
                contentFrame.Navigate(typeof(Home));
            }
        }

        // ���� Tag="Settings" �� NavigationViewItem
        private NavigationViewItem? FindSettingsNavItem()
        {
            // ��� FooterMenuItems
            foreach (var obj in nvSample.FooterMenuItems)
            {
                if (obj is NavigationViewItem item && (item.Tag?.ToString() == "Settings"))
                    return item;
            }
            // ��� MenuItems�����������÷������˵���
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
                        
                        _ => typeof(Home)  // Ĭ�ϵ�������ҳ
                    };

                    // ȷ�� contentFrame ��Ϊ��
                    if (contentFrame != null)
                    {
                        // ʹ�� try-catch ��װ��������
                        try
                        {
                            contentFrame.Navigate(pageType);
                        }
                        catch (Exception ex)
                        {
                            // ����ʧ��ʱ�Ĵ���
                            System.Diagnostics.Debug.WriteLine($"Navigation failed: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // ����ѡ������¼��е��쳣
                System.Diagnostics.Debug.WriteLine($"Selection changed error: {ex.Message}");
            }
        }

        // ���һ���������ڿ��� InfoBadge ��ʾ
        public void SetSettingsInfoBadgeVisible(bool visible)
        {
            if (SettingsNavItem != null && SettingsNavItem.InfoBadge is InfoBadge badge)    
            {
                badge.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
            }
        }
    }
}
