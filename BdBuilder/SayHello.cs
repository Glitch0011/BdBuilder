using Microsoft.Build.Framework;

namespace BdBuilder
{
    public class SayHello : Microsoft.Build.Utilities.Task
    {
        public override bool Execute()
        {
            Log.LogMessage(MessageImportance.High, "Test");
            return true;
        }
    }
}