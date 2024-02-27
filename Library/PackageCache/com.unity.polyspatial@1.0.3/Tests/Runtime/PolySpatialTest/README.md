# PolySpatialTest Library

PolySpatialTest is a collection of base test classes and utilities to simplify creating PolySpatial project tests.
Users can create different types of tests with built-in functionality and helper methods by creating child test classes
of the provided base classes and begin writing test cases.

The PolySpatialTest library aims to improve on several key areas when writing tests:
* managing environment setup and cleanup
* exposing common test action methods
* exposing common test validation methods
* adding visual elements for manual validation
* built-in test suite templates

## Getting Started
Users can follow the examples here to begin creating or extending PolySpatial test suites. Users are encouraged to first read through PolySpatial
documentation, starting from the PolySpatial project's [`README`](https://github.cds.internal.unity3d.com/unity/polyspatial/blob/main/README.md).

### The PolySpatial Test Development Process
Overall, users writing new or extending existing PolySpatial tests can expect to go through the following process:
1. Identify what PolySpatial feature/component requires testing. Users may check with the PolySpatialCore team to identify the target feature/component.
2. Determine what type of testing the PolySpatial feature/component requires. Users should aim to provide test coverage across all major areas (unit, functional, perf, etc), though it is normally best to focus on one category at a time.
3. Once the testing category is defined (for example, functional), users should define the difference scenarios and use cases that the feature/component will be tested against.
4. Run through the scenarios and/or use cases manually at least once. This step is important as it provides the following information:
    1. Determine if the test case is a candidate for automation. Some scenarios are difficult or virtually impossible to automate, so its best to leave these for last or agree to leave as a manual test case (don't remove the scenario, just document the decision).
    2. Identify the input parameters and expected results. In case of an unexpected result, this is a first step at identifying a bug and being aware of it when implementing the test case.
    3. Identify the runtime environment. Does the scenario require any setup steps or additional dependencies to carry out the use case?
5. Define which test cases have the highest priority. There are several factors to consider:
    1. Is the scenario a critical path?
    2. Is the scenario a low hanging fruit?
    3. Does the scenario provide additional value (for example, useful for developers)?
6. Identify the target test suite. This may be an existing test suite already in the PolySpatial repo, or a new test suite users will be creating.
7. Start adding test cases to the target test suite. This is an iterative step, which may require multiple PRs to complete.
8. As users iterate through step 7, it is important to run all tests both locally and remotely (Yamato) to verify no existing test break, and if they do, understand why and file necessary JIRAs if needed.

### Examples

#### Creating a new PolySpatial component functional test suite
For the sakes of this example, we'll assume there is currently no functional test suite for the `PolySpatialLight` component.

1. Define a child class of `PolySpatialComponentlTest` named `PolySpatialLightTest` under `TestProject/Assets/Tests/Components`
```
namespace PolySpatial.Tests.Component
{
    /// <summary>
    /// PolySpatialLIght functional test suite
    /// </summary>
    public class PolySpatialLightTest: PolySpatialComponentTest
    {
        public PolySpatialLightTest() : base(true) {}
    }
}
```

There are several things to note from this example:
* namespace `PolySpatial.Tests.Component` - `PolySpatial.Tests` is the shared namespace for all PolySpatial functional tests, and `.Components` is the specific namespace element for all PolySpatial component tests.
* test class documentation - it is highly recommended to add at least a generic description for new test classes, preferably with details about the feature/component under test.
* parent test class `PolySpatialComponentTest` - this is one of the available base test classes provided by the PolySpatialTest library, specifically for defining PolySpatial component functional tests.
* test class constructor `public PolySpatialLightTest() : base(true) {}` - this calls the parent class's constructor, overriding flag `enableVisibleScene`, which, when set to `true`, will allow users to visualize the rendered Scene.

#### Adding tests to an existing PolySpatial functional test suite
TBD

## Components
### Base Test Classes
There are several base test classes defined under the `PolySpatialTest/Base` directory:
* `PolySpatialTest`
* `PolySpatialUnitTest`
* `PolySpatialFunctionalTest`

Additionally, there are extensions of these base test classes, also made to be base test classes:
* `PolySpatialComponentTest` - extends from `PolySpatialFunctionalTest`

Users can choose to extend from any one of these when creating a new test suite, depending on the type of test suite being created.
If there is no specific base test class that makes sense, reach out to the PolySpatial QA team (currently `manuel.gonzalez`) if
a new type is needed.

#### PolySpatialTest
A generic PolySpatial project test class, with shared attributes and methods applicable to all test categories and test suites.
This is the most generic base test class available; typically users should not extend directly from this class unless no
child base test class exist that fits the target test suite.

#### PolySpatialUnitTest
Encapsulates shared attributes and methods applicable to PolySpatial unit test suites. This base test class is recommended when creating
a new unit test suite, and users are encouraged to follow best practices for unit test case implementation, isolating the target
component and/or classes under test as best as possible.

**Note**: this class is currently a stub for future development; no mocking framework is yet available in the PolySpatial repo to add helper
methods to isolate target feature/components. As such, extending this class today has not added value to extending directly from `PolySpatialTest`,
however, users should continue to extend from this to gain future features.

#### PolySpatialFunctionalTest
Encapsulates shared attributes and methods applicable to PolySpatial functional test suites. This base test class is recommended when creating
new functional tests, and users are recommended to define scenarios with partial or full integration with the end user environment.
`PolySpatialFunctionalTest` does not attempt to mock any dependencies nor isolate features/components from the runtime environment.

#### PolySpatialComponentTest
TBD

### Test Scene Builder
TBD
### Test Scene Actions
TBD
### Test Scene Validations
TBD

## Development
TBD

