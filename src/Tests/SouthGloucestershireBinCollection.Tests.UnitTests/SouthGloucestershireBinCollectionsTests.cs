using System;
using System.Linq;
using Xunit;

namespace SouthGloucestershireBinCollection.Tests.UnitTests
{
    public class SouthGloucestershireCouncilTests
    {
        [Fact]
        public void GetAddressesAsync_When_InvalidAddressProvided_Then_AddressesNotReturned()
        {
            // Arrange
            var council = new SouthGloucestershireBinCollections();

            // Act
            var result = council.GetAddressesAsync(new Address()
            {
                Postcode = "BA1 1AA"
            }).Result;

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void GetAddressesAsync_When_ValidAddressProvided_Then_MultipleAddressesReturned()
        {
            // Arrange
            var council = new SouthGloucestershireBinCollections();

            // Act
            var result = council.GetAddressesAsync(new Address()
            {
                Postcode = "BS15 1PR"
            }).Result;

            // Assert
            Assert.NotEmpty(result);
            Assert.True(result.Count() > 1, "Multiple addresses were expected");
        }

        [Fact]
        public void GetAddressesAsync_When_SpecificAddressProvided_Then_SingleAddressesReturned()
        {
            // Arrange
            var council = new SouthGloucestershireBinCollections();

            // Act
            var result = council.GetAddressesAsync(new Address()
            {
                Property = "77",
                Postcode = "BS15 1PR"
            }).Result;

            // Assert
            Assert.NotEmpty(result);
            Assert.Single(result);
        }
    }
}
