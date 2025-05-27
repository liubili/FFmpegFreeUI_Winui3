using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace FFmpegFreeUI
{
    public static class NavigationBadgeHelper
    {
        /// <summary>
        /// ���ơ����á�������� InfoBadge ��ʾ
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