using Autodesk.DataExchange.Core.Interface;
using Autodesk.DataExchange.DataModels;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SampleConnector;
using System;
using System.Threading.Tasks;

namespace SampleConnectorUnitTests
{
    [TestClass]
    public class CreateExchangeHelperTests
    {

        public void TestInit()
        {
        }

        [TestMethod]
        public void GetRandomId_ShouldReturnNonEmptyString()
        {
            // Act
            var result = CreateExchangeHelper.GetRandomId();

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Length > 0);
            Assert.AreEqual(5, result.Length); // Method returns substring of 5 characters
        }

        [TestMethod]
        public void GetRandomId_ShouldReturnDifferentValuesOnMultipleCalls()
        {
            // Act
            var result1 = CreateExchangeHelper.GetRandomId();
            var result2 = CreateExchangeHelper.GetRandomId();

            // Assert
            Assert.AreNotEqual(result1, result2);
        }

        [TestMethod]
        public async Task AddUniqueStringParameter_WithNullElement_ShouldThrowArgumentNullException()
        {
            // Arrange
            // No arrangement needed for null test

            // Act & Assert
            var exception = await Assert.ThrowsExceptionAsync<ArgumentNullException>(
                () => CreateExchangeHelper.AddUniqueStringParameter(null));

            Assert.AreEqual("element", exception.ParamName);
        }

        [TestMethod]
        public void AddPrimitiveGeometries_MethodSignature_ShouldBeCorrect()
        {
            // This verifies the method contract without executing complex SDK code
            var methodInfo = typeof(CreateExchangeHelper).GetMethod("AddPrimitiveGeometries");

            Assert.IsNotNull(methodInfo);
            Assert.AreEqual(typeof(void), methodInfo.ReturnType);

            var parameters = methodInfo.GetParameters();
            Assert.AreEqual(1, parameters.Length);
            Assert.AreEqual(typeof(ElementDataModel), parameters[0].ParameterType);
        }
    }
}
