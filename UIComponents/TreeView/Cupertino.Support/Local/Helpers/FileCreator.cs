using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cupertino.Support.Local.Helpers
{
    public class FileCreator
    {
        public string BasePath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        public void Create()
        {
            string textData = "Test File Content";

            string[] tempFiles =
            {
                @"\shk\Microsoft\Visual Studio\solution.txt",
                @"\shk\Microsoft\Visual Studio\debug.mp3",
                @"\shk\Microsoft\Visual Studio\class.cs",
                @"\shk\Microsoft\Sql Manager\query.txt",
                @"\shk\Apple\iphone\store.txt",
                @"\shk\Apple\iphone\calculator.mp3",
                @"\shk\Apple\iphone\safari.cs",
            };

            foreach (string file in tempFiles)
            {
                string fullPath = BasePath + file;
                string dirName = Path.GetDirectoryName(fullPath);

                if (!Directory.Exists(dirName))
                {
                    Directory.CreateDirectory(dirName);
                }
                File.WriteAllText(fullPath, textData);
            }
        }
    }
}
