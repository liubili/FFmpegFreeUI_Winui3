## 如何在其他页面控制“设置”导航项的 InfoBadge（角标）

你可以通过静态帮助类 `NavigationBadgeHelper` 在任意页面显示或隐藏“设置”项的 InfoBadge。例如：

```Csharp
// 显示角标 NavigationBadgeHelper.SetSettingsInfoBadgeVisible(true);
// 隐藏角标 NavigationBadgeHelper.SetSettingsInfoBadgeVisible(false);
```

只需在需要的地方调用上述方法，无需直接操作 MainWindow 实例。