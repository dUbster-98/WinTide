using System;
using System.Collections.Generic;
using System.Xml.Linq;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Collections.ObjectModel;
using WindowsScreenTime.Models;

namespace WindowsScreenTime.Services
{
    public interface IXmlSetService
    {
        void SavePreset(string presetName, ObservableCollection<ProcessUsage> viewList);
        void SaveSelectedPreset(string selectedPreset);
        string LoadSelectedPreset();
        List<ProcessUsage> LoadPresetProcess(string presetName);
    }
    public class XmlSetService : IXmlSetService
    {
        private static string currentDirectory = Directory.GetCurrentDirectory();
        private static string pathDir = Path.Combine(currentDirectory, "data");
        public static string _filePath = pathDir + "/data.xml";
        
        public XmlSetService()
        {

        }

        public void SavePreset(string presetName, ObservableCollection<ProcessUsage> viewList)
        {
            XDocument doc;

            // 파일이 존재하면 불러오고, 없으면 새로 만든다.
            if (System.IO.File.Exists(_filePath))
            {
                doc = XDocument.Load(_filePath);
            }
            else
            {
                doc = new XDocument(new XElement("PresetData"));
            }

            // 기존의 Preset을 찾아 제거 후 새로 추가
            XElement existingPreset = doc.Root.Elements("Preset")
                .FirstOrDefault(e => e.Attribute("Name")?.Value == presetName);

            if (existingPreset != null)
            {
                existingPreset.Remove(); // 기존의 Preset 제거
            }

            // 새로운 Preset 섹션 추가
            XElement presetElement = new XElement("Preset", new XAttribute("Name", presetName));

            // 새로운 Process 추가
            foreach (ProcessUsage process in viewList)
            {
                XElement processElement = new XElement("Process",
                    new XAttribute("Name", process.ProcessName), 
                    new XAttribute("EditedName", process.EditedName),
                    new XAttribute("BaseName", process.BaseName));
                presetElement.Add(processElement);
            }

            // 새로 추가된 Preset을 XML에 추가
            doc.Root.Add(presetElement);

            // 변경된 내용을 파일에 저장
            doc.Save(_filePath);
        }

        public void AddProcess(XElement precessElement)
        {

        }

        // SelectedPreset을 저장하는 개별적인 메서드
        public void SaveSelectedPreset(string selectedPreset)
        {
            XDocument doc;

            // 파일이 존재하면 불러오고, 없으면 새로 만든다.
            if (System.IO.File.Exists(_filePath))
            {
                doc = XDocument.Load(_filePath);
            }
            else
            {
                doc = new XDocument(new XElement("PresetData"));
            }

            // SelectedPreset 값을 업데이트
            XElement selectedPresetElement = doc.Root.Element("SelectedPreset");
            if (selectedPresetElement == null)
            {
                selectedPresetElement = new XElement("SelectedPreset");
                doc.Root.Add(selectedPresetElement);
            }

            selectedPresetElement.Value = selectedPreset;

            // 변경된 내용을 파일에 저장
            doc.Save(_filePath);
        }

        // SelectedPreset을 불러오는 메서드
        public string LoadSelectedPreset()
        {
            if (!System.IO.File.Exists(_filePath))
                return string.Empty;

            XDocument doc = XDocument.Load(_filePath);
            XElement selectedPresetElement = doc.Root.Element("SelectedPreset");

            return selectedPresetElement?.Value ?? "0";  // 값이 없으면 빈 문자열 반환
        }

        public List<ProcessUsage> LoadPresetProcess(string presetName)
        {
            List<ProcessUsage> processes = new();

            if (!File.Exists(_filePath))
                return null;
            XDocument doc = XDocument.Load(_filePath);
            XElement presetElement = doc.Root.Elements("Preset")
                .FirstOrDefault(e => e.Attribute("Name")?.Value == presetName);

            
            if (presetElement != null)
            {
                var processElement = presetElement.Elements("Process");
                foreach (var e in processElement)
                {
                    ProcessUsage process = new();
                    process.ProcessName = e.Attribute("Name")?.Value ?? string.Empty;
                    process.EditedName = e.Attribute("EditedName")?.Value ?? string.Empty;
                    process.BaseName = e.Attribute("BaseName")?.Value ?? string.Empty;
                    processes.Add(process);
                }
            }

            return processes;
        }
    }
}
