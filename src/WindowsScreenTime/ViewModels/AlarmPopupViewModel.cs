using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Messaging;
using WindowsScreenTime.Models;
using CommunityToolkit.Mvvm.Input;

namespace WindowsScreenTime.ViewModels
{
    public partial class AlarmPopupViewModel : ObservableObject
    {
        [ObservableProperty]
        private string alarmMessage;

        public AlarmPopupViewModel()
        {
            WeakReferenceMessenger.Default.Register<TransferAlarmMessage>(this, OnTransferAlarmMessage);
        }

        private void OnTransferAlarmMessage(object recipient, TransferAlarmMessage message)
        {
            AlarmMessage = message.Message;
        }
    }
}
