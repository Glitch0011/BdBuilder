# BdBuilder

Automatically generates `.cs` files from `.feature` files.

For example, `Example.feature`:
```
Eating cucumbers

	Given there are "12" cucumbers
	When I eat "5" cucumbers
	Then I should have "7" cucumbers
```

Becomes `Example.feature.cs`:

```

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
namespace Tests
{
	[TestClass]
	public class Example
	{
		[TestMethod]
		public void EatingCucumbers()
		{
			var step = new Steps();
			step.GivenThereAreXCucumbers(x: "12");
			step.WhenIEatXCucumbers(x: "5");
			step.ThenIShouldHaveXCucumbers(x: "7");
		}
	}
}
```

# Install

Installation is trivial, simply add the package and it'll run before a build.

```
Install-Package bdbuilder
```

# Improvements

There is a very long list of things to improve, just say if you'd like to help.
