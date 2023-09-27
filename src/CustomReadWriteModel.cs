using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Markup;
using Autodesk.DataExchange.BaseModels;
using Autodesk.DataExchange.Core;
using Autodesk.DataExchange.Core.Enums;
using Autodesk.DataExchange.Core.Models;
using Autodesk.DataExchange.DataModels;
using Autodesk.DataExchange.Interface;
using Autodesk.DataExchange.Models;
using Autodesk.DataExchange.SchemaObjects.Assets;
using Autodesk.DataExchange.SchemaObjects.Units;
using Autodesk.DataExchange.Schemas.Models;
using Autodesk.GeometryPrimitives.Design;
using Autodesk.GeometryPrimitives.Geometry;
using Autodesk.Parameters;
using PrimitiveGeometry = Autodesk.GeometryPrimitives;

namespace SampleConnector
{
    class CustomReadWriteModel : BaseReadWriteExchangeModel
    {
        public Autodesk.DataExchange.UI.Application Application;
        private string currentRevision;
        private ExchangeData currentExchangeData;
        private GeometryConfiguration geometryConfiguration;
        public CustomReadWriteModel(IClient client) : base(client)
        {
            AfterCreateExchange += AfterCreateExchangeAction;
            GetLatestExchangeDetails += GetLatestExchangeDataAsync;
            geometryConfiguration = _sDKOptions?.GeometryConfiguration;
        }

        List<DataExchange> localStorage = new List<DataExchange>();

        public override async Task<List<DataExchange>> GetExchangesAsync(ExchangeSearchFilter exchangeSearchFilter)
        {
            
            localStorage =  await GetValidExchangesAsync(exchangeSearchFilter,localStorage);
           
            return localStorage;
        }

        public override async Task<DataExchange> GetExchangeAsync(DataExchangeIdentifier dataExchangeIdentifier)
        {
            var response = await base.GetExchangeAsync(dataExchangeIdentifier);

            if (localStorage.Find(item => item.ExchangeID == response.ExchangeID) == null)
            {
                response.IsExchangeFromRead = true;
                localStorage.Add(response);
            }

            return response;
        }


        public async Task GetLatestExchangeDataAsync(Object sender,  ExchangeItem exchangeItem)
        {
            //start loader
            Application.ClearBusyState();
            Application.SetBusyState("Downloading..");

            //clear existing notifications
            Application.ClearAllNotification();
            Application.ShowNotification(exchangeItem.Name + " Downloading", Autodesk.DataExchange.Core.Enums.NotificationType.Information);

            var exchangeIdentifier = new DataExchangeIdentifier
            {
                CollectionId = exchangeItem.ContainerID,
                ExchangeId = exchangeItem.ExchangeID,
                HubId = exchangeItem.HubId,
                Region = exchangeItem.HubRegion
            };
            try
            {
                
                //Get a list of all revisions
                var revisions = await Client.GetExchangeRevisionsAsync(exchangeIdentifier);
                //Get the latest revision

                var firstRev = revisions.First().Id;
                List<string> newerRevisions = new List<string>();

                if (!string.IsNullOrEmpty(currentRevision) && currentRevision == firstRev)
                {
                    Console.WriteLine("No changes found");
                    return;
                }
                // Use ElementDataModel Wrapper
                ElementDataModel data = null;

                // Get Exchange data
                if (currentExchangeData == null)
                {
                    // Get full data exchange data till the latest revision
                    currentExchangeData = await Client.GetExchangeDataAsync(exchangeIdentifier);
                    currentRevision = firstRev;

                    data = ElementDataModel.Create(Client, currentExchangeData);
                    newerRevisions.Add(firstRev);
                }
                else
                {
                    // Update data exchange data with Delta
                    var newRevision = await Client.RetrieveLatestExchangeDataAsync(currentExchangeData);
                    if (!string.IsNullOrEmpty(newRevision))
                    {
                        foreach (var revision in revisions)
                        {
                            if (revision.Id == currentRevision)
                            {
                                break;
                            }

                            newerRevisions.Add(revision.Id);
                        }
                        currentRevision = newRevision;
                    }

                    // Use ElementDataModel Wrapper
                    data = ElementDataModel.Create(Client, currentExchangeData);
                }

                // Get all Wall Elements
                var wallElements = data.Elements.Where(element => element.Category == "Walls").ToList();

                // Get all added Elements
                var addedElements = data.GetCreatedElements(newerRevisions);

                // Get all modified Elements
                var modifiedElements = data.GetModifiedElements(newerRevisions);

                // Get all deleted Elements
                var deletedElements = data.GetDeletedElements(newerRevisions);


                List<Task> geometryResults = new List<Task>();
                foreach (var element in data.Elements)
                {
                    geometryResults.Add(data.GetElementGeometryByElementAsync(element));
                }

                await Task.WhenAll(geometryResults).ConfigureAwait(false);


                //Get Geometry of whole data exchange file as STEP
                var wholeGeometryPath = Client.DownloadCompleteExchangeAsSTEP(data.ExchangeData.ExchangeID);

                // Get Type Parameters of an element
                var typeParams = wallElements.FirstOrDefault()?.TypeParameters;

                // Get Reference Element
                if (typeParams != null)
                {
                    var refEelement = currentExchangeData.GetAssetById(typeParams[typeParams.Count() - 1].Value);
                }

                Application.ShowNotification(exchangeItem.Name + " Download complete.", Autodesk.DataExchange.Core.Enums.NotificationType.Information);

                UpdateLocalExchange(exchangeItem);
            }
            catch (Exception e)
            {
                Application.ShowNotification(exchangeItem.Name + " Download failed.", Autodesk.DataExchange.Core.Enums.NotificationType.Error);
                Console.WriteLine(e);
            }
            finally
            {
                //clear loader after loading data exchange
                Application.ClearBusyState();
                Application.ClearAllNotification();
            }

        }

        public void AfterCreateExchangeAction(Object sender, DataExchange exchange)
        {
            localStorage.Add(exchange);
        }

        public void AfterUpdateExchange(ExchangeDetails exchange)
        {
            var dataExchange = localStorage.FirstOrDefault(exchangeItem => exchangeItem.ExchangeID == exchange.ExchangeID);
            if (dataExchange != null)
            {
                dataExchange.Updated = exchange.LastModifiedTime;
                dataExchange.FileVersionId = exchange.FileVersionUrn;
            }
            _sDKOptions.Storage.Add("LocalExchanges", localStorage);
            _sDKOptions.Storage.Save();
        }

        public override async Task UpdateExchangeAsync(ExchangeItem ExchangeItem)
        {
            try
            {
                ElementDataModel currentElementDataModel = null;
                currentExchangeData = await Client.GetExchangeDataAsync(
                    new DataExchangeIdentifier 
                    { 
                        ExchangeId = ExchangeItem.ExchangeID, 
                        CollectionId = ExchangeItem.ContainerID,
                        HubId = ExchangeItem.HubId,
                        Region = ExchangeItem.HubRegion
                    });

                CreateExchangeHelper createExchangeHelper = new CreateExchangeHelper();
                //Check if this is the initial sync to newly created blank Exchange
                if (currentExchangeData == null)
                {
                    /*Code block to Add Elements to blank Exchange for syncing*/

                    //Create a new ElementDataModel wrapper
                    currentElementDataModel = ElementDataModel.Create(Client);

                    //Set Unit info on Root Asset
                    (currentElementDataModel.ExchangeData.RootAsset as DesignAsset).LengthUnit = UnitFactory.Feet;
                    (currentElementDataModel.ExchangeData.RootAsset as DesignAsset).DisplayLengthUnit = UnitFactory.Feet;

                    //Add a basic wall geometry
                    createExchangeHelper.AddWallGeometry(currentElementDataModel);

                    //Add geometry with specific length unit
                    createExchangeHelper.AddGeometryWithLengthUnit(currentElementDataModel);

                    //Add primitive geometries - Line, Point and Circle
                    createExchangeHelper.AddPrimitiveGeometries(currentElementDataModel);

                    //Add Mesh geometry
                    createExchangeHelper.AddMeshGeometry(currentElementDataModel);

                    //Add IFC
                    createExchangeHelper.AddIFCGeometry(currentElementDataModel);  
                  
                    //Add NIST object
                    var newBRep = currentElementDataModel.AddElement(new ElementProperties("NISTSTEP", "Generics", "Generic", "Generic Object"));
                    createExchangeHelper.AddNISTObject(currentElementDataModel, newBRep);

                    //Create built in parameters
                    createExchangeHelper.AddInstanceParametersToElement(newBRep);

                    //create bool Custom parameter for instance
                    createExchangeHelper.AddCustomParametersToElement(newBRep, ExchangeItem.SchemaNamespace);
                }
                else
                {
                    /*Code block to update exchange. Add few dummy elements for sync*/

                    //Create ElementDataModel wrapper on top of existing ExchangeData object
                    currentElementDataModel = ElementDataModel.Create(Client,currentExchangeData);
                    
                    //Try deleting an element
                    currentElementDataModel.DeleteElement("1");

                    //Add few elements with Geometries to update exchange for Sync
                    createExchangeHelper.AddElementsForExchangeUpdate(currentElementDataModel);
                }

                DataExchangeIdentifier exchangeIdentifier = new DataExchangeIdentifier()
                {
                    CollectionId = ExchangeItem.ContainerID,
                    ExchangeId = ExchangeItem.ExchangeID,
                    HubId = ExchangeItem.HubId,
                    Region = ExchangeItem.HubRegion
                };
               
                await Client.SyncExchangeDataAsync(exchangeIdentifier, currentElementDataModel.ExchangeData);

                Application.ClearBusyState();
                Application.SetBusyState("Generate ACC Viewable");
                await Task.Run(() =>
                {
                    try
                    {
                        Thread.Sleep(5000);
                        Client.GenerateViewableAsync(ExchangeItem.Name, ExchangeItem.ExchangeID, ExchangeItem.ContainerID, ExchangeItem.FileURN).Wait();
                    }
                    catch (Exception ex)
                    {
                        _sDKOptions.Logger.Error(ex);
                        throw ex;
                    }
                });
            }
            catch (Exception e)
            {
                System.Windows.MessageBox.Show(e.Message);
                Console.WriteLine(e);
                throw;
            }
            finally
            {
                _ = Task.Run(async () =>
                {
                    DataExchangeIdentifier dataExchangeIdentifier = new DataExchangeIdentifier()
                    {
                        CollectionId = ExchangeItem.ContainerID,
                        ExchangeId = ExchangeItem.ExchangeID,
                        HubId = ExchangeItem.HubId,
                        Region = ExchangeItem.HubRegion
                    };

                    ExchangeDetails exchangeDetails = await Client.GetExchangeDetailsAsync(dataExchangeIdentifier);
                    if (exchangeDetails != null)
                    {
                        ExchangeItem.FileVersion = exchangeDetails.FileVersionUrn;
                        ExchangeItem.LastModified = exchangeDetails.LastModifiedTime;
                    }
                    AfterUpdateExchange(exchangeDetails);
                });
                Application.ClearBusyState();
            }
        }

        private async Task UpdateLocalExchange(ExchangeItem exchangeItem)
        {
            var dataExchangeIdentifier = new DataExchangeIdentifier
            {
                CollectionId = exchangeItem.ContainerID,
                ExchangeId = exchangeItem.ExchangeID,
                Region = exchangeItem.HubRegion
            };
            DataExchange exchange = await base.GetExchangeAsync(dataExchangeIdentifier);
            if (exchange != null)
            {
                exchangeItem.FileVersion = exchange.FileVersionId;
                exchangeItem.LastModified = exchange.Updated;
            }
            var localExchange = localStorage.FirstOrDefault(item => item.ExchangeID == exchange.ExchangeID);
            if (localExchange != null)
            {
                if (localExchange.FileVersionId != exchange.FileVersionId)
                    localExchange.FileVersionId = exchange.FileVersionId;
                if (localExchange.Updated != exchange.Updated)
                    localExchange.Updated = exchange.Updated;
            }
        }

        public List<DataExchange> GetLocalExchanges()
        {
            return localStorage?.ToList();
        }

        public void SetLocalExchanges(List<DataExchange> dataExchanges)
        {
            localStorage.AddRange(dataExchanges);
        }

        public override Task<IEnumerable<string>> UnloadExchangesAsync(List<ExchangeItem> exchanges)
        {
            return Task.Run(() => exchanges.Select(n => n.ExchangeID));
        }

        public override void SelectElements(List<string> exchangeIds)
        {
            //throw new NotImplementedException();
        }
    }
}
