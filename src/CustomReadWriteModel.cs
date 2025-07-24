using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Autodesk.DataExchange.BaseModels;
using Autodesk.DataExchange.Core;
using Autodesk.DataExchange.Core.Enums;
using Autodesk.DataExchange.Core.Events;
using Autodesk.DataExchange.Core.Models;
using Autodesk.DataExchange.DataModels;
using Autodesk.DataExchange.Interface;
using Autodesk.DataExchange.Models;
using Autodesk.DataExchange.UI.Core.Interfaces;
using SeverityEnum = Autodesk.DataExchange.UI.Core.Enums.Severity;

namespace SampleConnector
{
    class CustomReadWriteModel : BaseReadWriteExchangeModel
    {
        internal IInteropBridge Bridge { get; set; }

        private string currentRevision;
        private ElementDataModel currentElementDataModel;
        private GeometryConfiguration geometryConfiguration;

        public CustomReadWriteModel(IClient client) : base(client)
        {
            AfterCreateExchange += AfterCreateExchangeAction;
            GetLatestExchangeDetails += GetLatestExchangeDataAsync;
        }

        private List<DataExchange> localStorage = new List<DataExchange>();
        private const string SyncingMessage = "Syncing Exchange Data...";
        private const string GeneratingViewableMessage = "Generating ACC Viewable...";
        private const string DownloadingMessage = "Downloading...";
        private const int ViewableGenerationDelayMs = 5000;

        public override async Task<List<DataExchange>> GetExchangesAsync(ExchangeSearchFilter exchangeSearchFilter)
        {

            localStorage = await GetValidExchangesAsync(exchangeSearchFilter, localStorage);

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

        
        public async Task GetLatestExchangeDataAsync(GetLatestExchangeDetailsEventArgs arg)
        {
            var exchangeItem = arg.ExchangeItem;
            this.Bridge?.SetProgressMessage(DownloadingMessage);
            this.Bridge?.SendNotification($"Downloading '{exchangeItem.Name}'", SeverityEnum.Info, 5000);

            var exchangeIdentifier = CreateDataExchangeIdentifier(exchangeItem);

            try
            {
                await this.ProcessExchangeRevisions(exchangeIdentifier, exchangeItem);

                // Logs Skipped Elements
                this._sDKOptions?.Logger?.LogSkippedElement(SkippedElementType.Failed, "elementId", "Line", "PolyLine");
                this._sDKOptions?.Logger?.LogSkippedElement(SkippedElementType.Unsupported, "elementId", "Line", "FeatureLine");
                this._sDKOptions?.Logger?.LogSkippedElement(SkippedElementType.Miscellaneous, "elementId", "Line", "CurveSet");
                this._sDKOptions?.Logger?.LogSkippedElement(SkippedElementType.Failed, "elementId");
                this._sDKOptions?.Logger?.LogSkippedElement(SkippedElementType.Failed, "elementId", "Line", "CurveSet");

                await this.DownloadExchangeGeometry(exchangeIdentifier);

                var successMessage = $"Successfully downloaded '{exchangeItem.Name}'";
                this.Bridge?.SendNotification(successMessage, SeverityEnum.Success, 0);

                await this.UpdateLocalExchange(exchangeItem);
            }
            catch (Exception e)
            {
                var errorMessage = $"Failed to download '{exchangeItem.Name}'";
                this.Bridge?.SendNotification(errorMessage, SeverityEnum.Error, 0);
                Console.WriteLine($"{errorMessage}: {e.Message}");
            }
        }

        private async Task DownloadExchangeGeometry(DataExchangeIdentifier exchangeIdentifier)
        {
            // Download complete exchange as different formats for demonstration
            var stepFilePath = this.Client.DownloadCompleteExchangeAsSTEP(exchangeIdentifier);
            var objFilePath = this.Client.DownloadCompleteExchangeAsOBJ(
                exchangeIdentifier.ExchangeId,
                exchangeIdentifier.CollectionId);

            Console.WriteLine($"Downloaded geometry: STEP={stepFilePath}, OBJ={objFilePath}");
        }

        private async Task ProcessExchangeRevisions(DataExchangeIdentifier exchangeIdentifier, ExchangeItem exchangeItem)
        {
            var revResponse = await this.Client.GetExchangeRevisionsAsync(exchangeIdentifier);
            var revisions = revResponse.Value;
            var latestRevisionId = revisions.First().Id;
            var newerRevisions = new List<string>();

            if (!string.IsNullOrEmpty(this.currentRevision) && this.currentRevision == latestRevisionId)
            {
                Console.WriteLine("No changes found");
                return;
            }

            ElementDataModel data = await this.GetOrUpdateElementData(exchangeIdentifier, latestRevisionId, revisions, newerRevisions);
            await this.AnalyzeExchangeElements(data, newerRevisions);
        }

        private async Task AnalyzeExchangeElements(ElementDataModel data, List<string> newerRevisions)
        {
            // Demonstrate various element analysis operations
            var wallElements = data.Elements.Where(element => element.Category == "Walls").ToList();
            var addedElements = data.GetCreatedElements(newerRevisions);
            var modifiedElements = data.GetModifiedElements(newerRevisions);
            var deletedElements = data.GetDeletedElements(newerRevisions);

            // Load geometry for all elements
            var geometries = await data.GetElementGeometriesAsync(data.Elements);

            // Log analysis results for sample purposes
            Console.WriteLine($"Analysis: {wallElements.Count} walls, {addedElements.Count()} added, " +
                            $"{modifiedElements.Count()} modified, {deletedElements.Count()} deleted elements");
        }

        private async Task<ElementDataModel> GetOrUpdateElementData(
            DataExchangeIdentifier exchangeIdentifier,
            string latestRevisionId,
            IEnumerable<ExchangeRevision> revisions,
            List<string> newerRevisions)
        {
            ElementDataModel data;

            if (this.currentElementDataModel == null)
            {
                // Get full exchange data for the first time
                var response = await this.Client.GetElementDataModelAsync(exchangeIdentifier);
                this.currentElementDataModel = response.Value;
                this.currentRevision = latestRevisionId;
                data = ElementDataModel.Create(Client);
                newerRevisions.Add(latestRevisionId);
            }
            else
            {
                // Update with delta changes
                var response = await this.Client.RetrieveLatestExchangeDataAsync(this.currentElementDataModel);
                var newRevision = response.Value;

                if (!string.IsNullOrEmpty(newRevision))
                {
                    foreach (var revision in revisions)
                    {
                        if (revision.Id == this.currentRevision) break;
                        newerRevisions.Add(revision.Id);
                    }
                    this.currentRevision = newRevision;
                }

                data = ElementDataModel.Create(Client);
            }

            return data;
        }


        public void AfterCreateExchangeAction(object sender, AfterCreateExchangeEventArgs e)
        {
            this.localStorage.Add(e.DataExchange);
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

        public override List<DataExchange> GetCachedExchanges()
        {
            return this.localStorage?.ToList() ?? new List<DataExchange>();
        }

        public override async Task UpdateExchangeAsync(ExchangeItem exchangeItem, CancellationToken cancellationToken = default)
        {
            try
            {
                var dataExchangeIdentifier = CreateDataExchangeIdentifier(exchangeItem);
                var response = await this.Client.GetElementDataModelAsync(dataExchangeIdentifier);

                // Logs Skipped Elements
                this._sDKOptions?.Logger?.LogSkippedElement(SkippedElementType.Failed, "elementId", "Line", "PolyLine");
                this._sDKOptions?.Logger?.LogSkippedElement(SkippedElementType.Unsupported, "elementId", "Line", "FeatureLine");
                this._sDKOptions?.Logger?.LogSkippedElement(SkippedElementType.Miscellaneous, "elementId", "Line", "CurveSet");
                this._sDKOptions?.Logger?.LogSkippedElement(SkippedElementType.Failed, "elementId");
                this._sDKOptions?.Logger?.LogSkippedElement(SkippedElementType.Failed, "elementId", "Line", "CurveSet");

                this.currentElementDataModel = response.Value;

                ElementDataModel elementDataModel = await this.PrepareElementDataModel(exchangeItem);

                this.Bridge?.SetProgressMessage(SyncingMessage);
                await this.Client.SyncExchangeDataAsync(dataExchangeIdentifier, elementDataModel);

                await this.GenerateViewableAsync(exchangeItem);
            }
            catch (Exception e)
            {
                this.HandleUpdateError(e, exchangeItem.Name);
                throw;
            }
            finally
            {
                // Update exchange details in background
                _ = Task.Run(async () => await this.UpdateExchangeDetailsAsync(exchangeItem));
            }
        }

        private static DataExchangeIdentifier CreateDataExchangeIdentifier(ExchangeItem exchangeItem)
        {
            return new DataExchangeIdentifier
            {
                ExchangeId = exchangeItem.ExchangeID,
                CollectionId = exchangeItem.ContainerID,
                HubId = exchangeItem.HubId,
            };
        }

        private async Task<ElementDataModel> PrepareElementDataModel(ExchangeItem exchangeItem)
        {
            if (this.currentElementDataModel == null)
            {
                return await this.CreateInitialExchangeData();
            }
            else
            {
                return await this.UpdateExistingExchangeData();
            }
        }

        [Obsolete]
        private async Task GenerateViewableAsync(ExchangeItem exchangeItem)
        {
            this.Bridge?.SetProgressMessage(GeneratingViewableMessage);

            await Task.Run(async () =>
            {
                try
                {
                    // Simulate processing time - in real scenario this would be determined by the actual process
                    await Task.Delay(ViewableGenerationDelayMs);
                    await this.Client.GenerateViewableAsync(exchangeItem.ExchangeID, exchangeItem.ContainerID);
                }
                catch (Exception ex)
                {
                    this._sDKOptions?.Logger?.Error(ex);
                    throw;
                }
            });
        }

        private void HandleUpdateError(Exception exception, string exchangeName)
        {
            var errorMessage = $"Failed to update exchange '{exchangeName}': {exception.Message}";
            // Log the error
            Console.WriteLine(errorMessage);
            Console.WriteLine(exception);

            // For demo purposes, show message box - in production, this should be handled by UI layer
            System.Windows.MessageBox.Show(exception.Message, "Update Error",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }

        private async Task UpdateExchangeDetailsAsync(ExchangeItem exchangeItem)
        {
            try
            {
                var dataExchangeIdentifier = CreateDataExchangeIdentifier(exchangeItem);
                var response = await this.Client.GetExchangeDetailsAsync(dataExchangeIdentifier);
                var exchangeDetails = response.Value;

                if (exchangeDetails != null)
                {
                    exchangeItem.FileVersion = exchangeDetails.FileVersionUrn;
                    exchangeItem.LastModified = exchangeDetails.LastModifiedTime;
                    this.AfterUpdateExchange(exchangeDetails);
                }
            }
            catch (Exception ex)
            {
                this._sDKOptions?.Logger?.Error($"Failed to update exchange details: {ex.Message}");
            }
        }

        private async Task<ElementDataModel> CreateInitialExchangeData()
        {
            // Create a new ElementDataModel for blank exchange
            var elementDataModel = ElementDataModel.Create(Client);

            // Demonstrate various geometry types with sample data
            await CreateExchangeHelper.AddVariedGeometryObjects(elementDataModel, 3);

            return elementDataModel;
        }


        private async Task<ElementDataModel> UpdateExistingExchangeData()
        {
            // Create wrapper on existing exchange data
            var elementDataModel = ElementDataModel.Create(Client);

            // Demonstrate element deletion (if elements exist)
            this.DeleteSampleElement(elementDataModel);

            // Add new elements to demonstrate updates
            await CreateExchangeHelper.AddVariedGeometryObjects(elementDataModel, 4);
            // Add parameters to new elements
            await this.AddSampleParametersToNewElements(elementDataModel);

            return elementDataModel;
        }

        private async Task AddSampleParametersToNewElements(ElementDataModel elementDataModel)
        {
            var newElements = elementDataModel.Elements.Reverse().Take(6).Reverse().ToList();

            if (newElements.Count > 0) await CreateExchangeHelper.AddUniqueStringParameter(newElements[0]);
        }

        private void DeleteSampleElement(ElementDataModel elementDataModel)
        {
            var existingElements = elementDataModel.Elements.ToList();
            if (existingElements.Count > 0)
            {
                elementDataModel.DeleteElement(existingElements[0].Id);
            }
        }

        private async Task UpdateLocalExchange(ExchangeItem exchangeItem)
        {
            var dataExchangeIdentifier = new DataExchangeIdentifier
            {
                CollectionId = exchangeItem.ContainerID,
                ExchangeId = exchangeItem.ExchangeID,
                HubId = exchangeItem.HubId,
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

        public override Task<bool> SelectElementsAsync(List<string> exchangeIds)
        {
            return Task.FromResult(true);
        }
    }
}
