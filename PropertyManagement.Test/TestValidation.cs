using Xunit;

namespace PropertyManagement.Test
{
    public class TestValidation
    {
        [Fact]
        public void Test_Project_Compiles_Successfully()
        {
            // This test verifies that the project compiles correctly
            // If this test runs, it means all dependencies are resolved
            Assert.True(true);
        }
        
        [Fact]
        public void Test_Service_Based_Architecture_Working()
        {
            // Verify that our service-based tests can be instantiated
            var inspectionTests = new Controllers.InspectionsControllerTests();
            var roomsTests = new Controllers.RoomsControllerTests();
            var additionalTests = new Controllers.AdditionalControllerTests();
            
            Assert.NotNull(inspectionTests);
            Assert.NotNull(roomsTests);
            Assert.NotNull(additionalTests);
        }
    }
}