using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using WindowsScreenTime.Models;

namespace WindowsScreenTime.Services
{
    public interface IProcessContainService
    {
        BitmapImage GetProcessIcon(string path);
        string? GetProcessPath(Process process);
        string? GetProcessPathByString(string processName);
    }

    public class ProcessContainService : IProcessContainService
    {
        public BitmapImage GetProcessIcon(string path)
        {
            try
            {
                Icon? icon = Icon.ExtractAssociatedIcon(path);
                if (icon == null) return null;

                using (var stream = new MemoryStream())
                {
                    icon.ToBitmap().Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                    stream.Seek(0, SeekOrigin.Begin);

                    BitmapImage bitmap = null;
                    App.Current.Dispatcher.Invoke(() =>
                    {
                        bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.StreamSource = stream;
                        bitmap.DecodePixelWidth = 32;
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.EndInit();
                        bitmap.Freeze(); // 스레드 안전성을 위해 Freeze 호출
                    });

                    return bitmap;
                }
            }
            catch
            {
                return null; // 아이콘을 가져올 수 없으면 null 반환
            }
        }

        public string? GetProcessPath(Process process)
        {
            try
            {
                return process.MainModule.FileName;
            }
            catch
            {
                return null; // 경로에 접근할 수 없을 경우
            }
        }

        public string? GetProcessPathByString(string processName)
        {
            foreach (var process in Process.GetProcessesByName(processName))
            {
                try
                {
                    return process.MainModule.FileName;
                }
                catch
                {
                    return null; // 경로에 접근할 수 없을 경우
                }
            }
            return null;
        }
    }
}
