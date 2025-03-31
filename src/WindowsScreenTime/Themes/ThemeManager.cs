using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace WindowsScreenTime.Themes
{
    public enum ThemeType
    {
        Light,
        Dark
    }
    public static class ThemeManager
    {
        private static ThemeType _currentTheme = ThemeType.Light;

        // 현재 테마 속성
        public static ThemeType CurrentTheme
        {
            get => _currentTheme;
            private set => _currentTheme = value;
        }

        // 테마 변경 이벤트
        public static event EventHandler<ThemeType> ThemeChanged;

        // 테마 변경 메서드
        public static void ChangeTheme(ThemeType theme)
        {
            if (CurrentTheme == theme)
                return;

            CurrentTheme = theme;

            // 리소스 딕셔너리 변경
            var oldThemeDict = new Uri($"/Themes/{CurrentTheme}Theme.xaml", UriKind.Relative);
            var newThemeDict = new Uri($"/Themes/{theme}Theme.xaml", UriKind.Relative);

            ResourceDictionary resourceDict = new ResourceDictionary();
            resourceDict.Source = newThemeDict;

            // 이전 테마 리소스 제거
            var mergedDicts = Application.Current.Resources.MergedDictionaries;
            for (int i = 0; i < mergedDicts.Count; i++)
            {
                var dict = mergedDicts[i];
                if (dict.Source != null && dict.Source.OriginalString.Contains("Theme.xaml"))
                {
                    mergedDicts.RemoveAt(i);
                    break;
                }
            }

            // 새 테마 리소스 추가
            Application.Current.Resources.MergedDictionaries.Add(resourceDict);

            // 애니메이션에 사용할 이벤트 발생
            ThemeChanged?.Invoke(null, theme);
        }

        // 테마 토글 메서드
        public static void ToggleTheme()
        {
            ThemeType newTheme = CurrentTheme == ThemeType.Light ? ThemeType.Dark : ThemeType.Light;
            ChangeTheme(newTheme);
        }
    }
}
