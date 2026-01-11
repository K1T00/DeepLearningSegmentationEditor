using Xunit;

// Disables all parallel test executions (needed for memory leak tests)
[assembly: CollectionBehavior(DisableTestParallelization = true)]
namespace AnnotationTool.Tests
{}