using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace WindowsScreenTime.Models
{
    public class PresetItem
    {
        public ObservableCollection<PresetContent> PresetItems = new();

        public PresetItem()
        {
            PresetItems = new()
            {
                new PresetContent { Content = "1", Tag = "M10,7V9H12V17H14V7H10M12,2A10,10 0 0,1 22,12A10,10 0 0,1 12,22A10,10 0 0,1 2,12A10,10 0 0,1 12,2Z" },
                new PresetContent { Content = "2", Tag = "M9,7V9H13V11H11A2,2 0 0,0 9,13V17H11L15,17V15H11V13H13A2,2 0 0,0 15,11V9A2,2 0 0,0 13,7H9M12,2A10,10 0 0,1 22,12A10,10 0 0,1 12,22A10,10 0 0,1 2,12A10,10 0 0,1 12,2Z" },
                new PresetContent { Content = "3", Tag = "M15,15V13.5A1.5,1.5" },
                new PresetContent { Content = "4", Tag = "M15,15V13.5A1.5,1.5" },
                new PresetContent { Content = "3", Tag = "M15,15V13.5A1.5,1.5" },
            };
        }
    }

    public class PresetContent
    {
        public string Content { get; set; }
        public string Tag { get; set; }
    }
}
