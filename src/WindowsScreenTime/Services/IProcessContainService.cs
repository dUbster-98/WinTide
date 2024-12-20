using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WindowsScreenTime.Models;

namespace WindowsScreenTime.Services
{
    public interface IProcessContainService
    {
        string BasePath { get; }

        SelectedProcess SelectedProcess { get; set; }

        void CreateList();
    }

    public class ProcessContainService : IProcessContainService
    {
        public string BasePath { get; }
        public SelectedProcess SelectedProcess { get; set; }

        string textData="";

        public void CreateList()
        {

        }
    }
}
