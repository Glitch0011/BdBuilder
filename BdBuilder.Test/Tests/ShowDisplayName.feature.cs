using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
namespace BdBuilder.Test {
[TestClass]
public class ShowDisplayName {
[TestMethod]
public void ShowTheDisplayNameWhenTheRoadIsValid() {
var step = new Steps();
step.GivenAValidRoadIDIsSpecified();
step.WhenTheClientIsRunning();
step.ThenTheRoadXShouldBeDisplayed(x: "displayName");
step.ShowTheStatusseverityWhenTheRoadIsValid();
step.GivenAValidRoadIDIsSpecified();
step.WhenTheClientIsRun();
step.ThenTheRoadX(x: "statusSeverityDescription’ should be displayed as ‘Road Status Description");
}
}
}