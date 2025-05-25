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
        public MainWindow()
        {
            InitializeComponent();
            nvSample.SelectionChanged += NvSample_SelectionChanged;
            // 默认导航到主页
            contentFrame.Navigate(typeof(Home));
        }

        private void NvSample_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (args.SelectedItem is NavigationViewItem item)
            {
                var pageTag = item.Tag?.ToString();
                Type pageType = pageTag switch
                {
                    "Home" => typeof(Home),
                    "SamplePage2" => typeof(Home),
                    "SamplePage3" => typeof(Home),
                    "SamplePage4" => typeof(Home),
                    "Settings" => typeof(Settings),
                    _ => null
                };
                if (pageType != null)
                {
                    Console.WriteLine(pageType);
                    contentFrame.Navigate(pageType);
                }
            }
        }
    }
}
