using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System.IO;

namespace BdBuilder
{
    public class SayHello : Task
    {
        public string FileName { get; set; }
        
        public override bool Execute()
        {
            var fileInfo = new FileInfo(FileName);

            Log.LogMessage(MessageImportance.High, $"Does {fileInfo.Name} exist = {fileInfo.Exists}");
            
            File.WriteAllText(Path.ChangeExtension(fileInfo.FullName, ".feature.cs"), "Hello, World");

            return true;
        }
    }
}