using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace FFmpegWinUI
{
    public static class NavigationBadgeHelper
    {
        /// <summary>
        /// 控制“设置”导航项的 InfoBadge 显示
        /// </summary>
        public static void SetSettingsInfoBadgeVisible(bool visible)
        {
            if (App.MainWindow is MainWindow mainWindow)
            {
                mainWindow.SetSettingsInfoBadgeVisible(visible);
            }
        }
    }
}