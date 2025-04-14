using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Messaging;
using WindowsScreenTime.Models;
using CommunityToolkit.Mvvm.Input;
using WindowsScreenTime.Views;

namespace WindowsScreenTime.ViewModels
{
    public partial class AlarmYesOrNoPopupViewModel : ObservableObject
    {
        [ObservableProperty]
        private string alarmMessage;

        public AlarmYesOrNoPopupViewModel()
        {
            WeakReferenceMessenger.Default.Register<TransferAlarmMessage>(this, OnTransferAlarmMessage);
        }

        private void OnTransferAlarmMessage(object recipient, TransferAlarmMessage message)
        {
            AlarmMessage = message.Message;
        }
    }
}
