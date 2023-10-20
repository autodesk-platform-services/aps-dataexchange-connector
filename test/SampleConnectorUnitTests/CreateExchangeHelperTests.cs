using Autodesk.DataExchange.DataModels;
using Autodesk.DataExchange.Interface;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SampleConnector;
using System.Linq;

namespace SampleConnectorUnitTests
{
    [TestClass]
    public class CreateExchangeHelperTests
    {
        private CreateExchangeHelper _createExchangeHelper;
        private ElementDataModel _dataModel;
        private IClient _client;

        [TestInitialize]
        public void TestInit()
        {
            _createExchangeHelper = new CreateExchangeHelper();
            _dataModel = ElementDataModel.Create(_client);
        }

        [TestMethod]
        public void TestAddWallGeometry()
        {
            _createExchangeHelper.AddWallGeometry(_dataModel);
            var wallElements = _dataModel.Elements.Where(element => element.Category == "Walls").ToList();

            Assert.IsTrue(wallElements != null && wallElements.Any());
        }

        [TestMethod]
        public void TestAddMeshGeometry()
        {
            _createExchangeHelper.AddMeshGeometry(_dataModel);
            var meshObjects = _dataModel.Elements.Where(element => element.Type == "Mesh Object").ToList();

            Assert.IsTrue(meshObjects != null && meshObjects.Any());
        }

        [TestMethod]
        public void TestAddInstanceParametersToElement()
        {
            var genericElement = _dataModel.AddElement(new ElementProperties("NISTSTEP", "Generics", "Generic", "Generic Object"));
            _createExchangeHelper.AddInstanceParametersToElement(genericElement);

            Assert.IsTrue(genericElement.InstanceParameters.Any());
        }

        [TestMethod]
        public void TestAddElementsForExchangeUpdate()
        {
            _createExchangeHelper.AddElementsForExchangeUpdate(_dataModel);
            var updateMeshElement = _dataModel.Elements.Where(element => element.Type == "Mesh Object Update").ToList();

            Assert.IsTrue(updateMeshElement != null && updateMeshElement.Any());
        }

        [TestMethod]
        public void TestAddPolylinePrimitiveGeometry()
        {
            _createExchangeHelper.AddPrimitiveGeometries(_dataModel);
            var polylineElement = _dataModel.Elements.FirstOrDefault(element => element.Id == "Polyline");

            Assert.IsTrue(polylineElement != null);
        }

        [TestMethod]
        public void TestCompositeCurvePrimitiveGeometry()
        {
            _createExchangeHelper.AddPrimitiveGeometries(_dataModel);
            var compositeCurveElement = _dataModel.Elements.FirstOrDefault(element => element.Id == "CompositeCurve");

            Assert.IsTrue(compositeCurveElement != null);
        }
    }
}
