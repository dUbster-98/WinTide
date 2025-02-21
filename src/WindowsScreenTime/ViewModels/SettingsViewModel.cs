using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace WindowsScreenTime.ViewModels
{
    public class SettingsViewModel
    {



        //private void Window_Loaded(object sender, RoutedEventArgs e)
        //{
        //    // 시작프로그램 등록 여부 확인
        //    if (runRegKey.GetValue("프로그램명") == null) chkRunProgram.IsChecked = false;
        //    else chkRunProgram.IsChecked = true;
        //}

        //RegistryKey runRegKey = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);

        //private void CheckBox_Click(object sender, RoutedEventArgs e)
        //{
        //    if (chkRunProgram.IsChecked == true)
        //    {
        //        runRegKey.SetValue("프로그램명", Environment.CurrentDirectory + "\\" + AppDomain.CurrentDomain.FriendlyName);
        //    }
        //    if (chkRunProgram.IsChecked == false)
        //    {
        //        runRegKey.DeleteValue("프로그램명", false);
        //    }
        //}
    }
}
