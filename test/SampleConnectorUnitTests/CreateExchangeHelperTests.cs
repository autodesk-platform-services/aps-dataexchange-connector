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
        public void TestAddPolylinePrimitiveGeometry()
        {
            _createExchangeHelper.AddPrimitiveGeometries(_dataModel);
            var polylineElement = _dataModel.Elements.Where(element => element.Id == "Polyline").ToList();

            Assert.IsTrue(polylineElement != null);
        }
    }
}
