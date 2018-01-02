using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace BdBuilder.Test
{
    [TestClass]
    public class Test
    {
        [TestMethod]
        public void RunTest()
        {
            CompileFeatureFile test = new CompileFeatureFile();

            var tempFile = Path.GetTempFileName();

            var testCode = new string[]
            {
                "Return a Road Corridor object when the road is valid",
                "",
                "	Given a valid road ID is specified",
                "	When Get Road Status is called",
                "   Then the function returns a Road Corridor object",
                "",
                "Throw an exception if the road is invalids",
                "",
                "   Given an invalid road ID is specified",
                "   When Get Road Status is called",
                "   Then the function throw an Api Error Exception"
            };

            File.WriteAllLines(tempFile, testCode);

            test.FileName = tempFile;
            test.RootNameSpace = "Test";

            test.Execute();
        }
    }
}
