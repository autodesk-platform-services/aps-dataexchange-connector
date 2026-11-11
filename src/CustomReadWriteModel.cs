namespace SampleConnector
{
    using System;
    using System.Collections.Generic;
    using System.IO;
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
    using Autodesk.DataExchange.ProgressManager.Enums;

    class CustomReadWriteModel : BaseReadWriteExchangeModel
    {
        internal IInteropBridge Bridge { get; set; }

        private string currentRevision;
        private string currentExchangeId;
        private ElementDataModel currentElementDataModel;
        private GeometryConfiguration geometryConfiguration;
        private const int MaxElementsForGeometryPreview = 20;

        public CustomReadWriteModel(IClient client) : base(client)
        {
            AfterCreateExchange += AfterCreateExchangeAction;
            GetLatestExchangeDetails += GetLatestExchangeDataAsync;
        }

        private List<DataExchange> localStorage = new List<DataExchange>();

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
            var cancellationToken = arg.CancellationToken;
            var progressManager = this.Client.ProgressStepsManager;
            var fetchExchangeStep = progressManager.GetProgressStep(ProgressStepId.FetchExchange);
            var downloadExchangeDataStep = progressManager.GetProgressStep(ProgressStepId.DownloadExchangeData);

            fetchExchangeStep.SubSteps = 3;

            var exchangeIdentifier = CreateDataExchangeIdentifier(exchangeItem);
            this.EnsureExchangeSession(exchangeItem.ExchangeID);

            this.Bridge?.SendNotification($"Downloading '{exchangeItem.Name}'", SeverityEnum.Info, 5000);

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                fetchExchangeStep.UpdateProgress();
                var revResponse = await this.Client.GetExchangeRevisionsAsync(exchangeIdentifier).ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();

                fetchExchangeStep.UpdateProgress();
                var revisions = revResponse.Value;
                var latestRevisionId = revisions.First().Id;
                fetchExchangeStep.MarkAsComplete();

                if (!string.IsNullOrEmpty(this.currentRevision) && this.currentRevision == latestRevisionId)
                {
                    downloadExchangeDataStep?.MarkAsComplete();
                    await this.UpdateLocalExchange(exchangeItem).ConfigureAwait(false);

                    var noChangesMessage = $"Exchange '{exchangeItem.Name}' is already up to date.";
                    this.Bridge?.SendNotification(noChangesMessage, SeverityEnum.Success, 5000);
                    return;
                }

                var newerRevisions = new List<string>();
                var data = await this.GetOrUpdateElementDataAsync(
                    exchangeIdentifier,
                    latestRevisionId,
                    revisions,
                    newerRevisions,
                    cancellationToken).ConfigureAwait(false);

                this.LogSkippedElementsSample();

                await this.AnalyzeExchangeElementsAsync(data, newerRevisions, cancellationToken).ConfigureAwait(false);
                await this.DownloadExchangeGeometryAsync(exchangeIdentifier, downloadExchangeDataStep, cancellationToken).ConfigureAwait(false);

                var successMessage = $"Successfully downloaded '{exchangeItem.Name}'";
                this.Bridge?.SendNotification(successMessage, SeverityEnum.Success, 0);

                await this.UpdateLocalExchange(exchangeItem).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception e)
            {
                var errorMessage = $"Failed to download '{exchangeItem.Name}'";
                this.Bridge?.SendNotification(errorMessage, SeverityEnum.Error, 0);
                this._sDKOptions?.Logger?.Error(e);
                throw;
            }
        }

        private void EnsureExchangeSession(string exchangeId)
        {
            if (this.currentExchangeId != exchangeId)
            {
                this.currentExchangeId = exchangeId;
                this.currentRevision = null;
                this.currentElementDataModel = null;
            }
        }

        private void LogSkippedElementsSample()
        {
            this._sDKOptions?.Logger?.LogSkippedElement(SkippedElementType.Failed, "elementId", "Line", "PolyLine");
            this._sDKOptions?.Logger?.LogSkippedElement(SkippedElementType.Unsupported, "elementId", "Line", "FeatureLine");
            this._sDKOptions?.Logger?.LogSkippedElement(SkippedElementType.Failed, "elementId");
            this._sDKOptions?.Logger?.LogSkippedElement(SkippedElementType.Failed, "elementId", "Line", "CurveSet");
        }

        private async Task DownloadExchangeGeometryAsync(
            DataExchangeIdentifier exchangeIdentifier,
            Autodesk.DataExchange.ProgressManager.Interfaces.IProgressStep downloadExchangeDataStep,
            CancellationToken cancellationToken)
        {
            var downloadPath = Path.Combine(
                Path.GetTempPath(),
                "SampleConnector",
                exchangeIdentifier.ExchangeId);

            Directory.CreateDirectory(downloadPath);

            await Task.Run(
                () =>
                {
                    var stepResult = this.Client.DownloadCompleteExchangeAsSTEP(exchangeIdentifier, downloadPath, cancellationToken);
                    if (stepResult.IsFailed)
                    {
                        this._sDKOptions?.Logger?.Error($"STEP download failed: {string.Join(", ", stepResult.Errors)}");
                    }
                    else
                    {
                        this._sDKOptions?.Logger?.Information($"STEP downloaded to: {stepResult.Value}");
                    }

                    var satResult = this.Client.DownloadCompleteExchangeAsSAT(exchangeIdentifier, downloadPath, cancellationToken);
                    if (satResult.IsFailed)
                    {
                        this._sDKOptions?.Logger?.Error($"SAT download failed: {string.Join(", ", satResult.Errors)}");
                    }
                    else
                    {
                        this._sDKOptions?.Logger?.Information($"SAT downloaded to: {satResult.Value}");
                    }

                    var objResult = this.Client.DownloadCompleteExchangeAsOBJ(exchangeIdentifier, downloadPath, cancellationToken);

                    if (objResult.IsFailed)
                    {
                        this._sDKOptions?.Logger?.Error($"OBJ download failed: {string.Join(", ", objResult.Errors)}");
                    }
                    else
                    {
                        this._sDKOptions?.Logger?.Information($"OBJ downloaded to: {objResult.Value}");
                    }

                    var ifc4Result = this.Client.DownloadCompleteExchangeAsIFC(
                        exchangeIdentifier,
                        Path.Combine(downloadPath, "ifc4"),
                        cancellationToken);

                    if (ifc4Result.IsFailed)
                    {
                        this._sDKOptions?.Logger?.Error($"IFC4 download failed: {string.Join(", ", ifc4Result.Errors)}");
                    }
                    else
                    {
                        this._sDKOptions?.Logger?.Information($"IFC4 downloaded to: {ifc4Result.Value}");
                    }
                },
                cancellationToken).ConfigureAwait(false);

            downloadExchangeDataStep?.MarkAsComplete();
        }

        private async Task AnalyzeExchangeElementsAsync(
            ElementDataModel data,
            List<string> newerRevisions,
            CancellationToken cancellationToken)
        {
            if (data == null)
            {
                return;
            }

            var wallElements = data.Elements.Where(element => element.Category == "Walls").ToList();
            var addedElements = data.GetCreatedElements(newerRevisions);
            var modifiedElements = data.GetModifiedElements(newerRevisions);
            var deletedElements = data.GetDeletedElements(newerRevisions);

            var elementList = data.Elements.ToList();
            if (elementList.Count > 0 && elementList.Count <= MaxElementsForGeometryPreview)
            {
                await data.GetElementGeometriesAsync(elementList, cancellationToken).ConfigureAwait(false);
            }

            Console.WriteLine(
                $"Analysis: {wallElements.Count} walls, {addedElements.Count()} added, " +
                $"{modifiedElements.Count()} modified, {deletedElements.Count()} deleted elements");
        }

        private async Task<ElementDataModel> GetOrUpdateElementDataAsync(
            DataExchangeIdentifier exchangeIdentifier,
            string latestRevisionId,
            IEnumerable<ExchangeRevision> revisions,
            List<string> newerRevisions,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (this.currentElementDataModel == null)
            {
                var response = await this.Client.GetElementDataModelAsync(exchangeIdentifier).ConfigureAwait(false);
                this.currentElementDataModel = (ElementDataModel)response.Value;
                this.currentRevision = latestRevisionId;
                newerRevisions.Add(latestRevisionId);
                return this.currentElementDataModel;
            }

            var deltaResponse = await this.Client.RetrieveLatestExchangeAsync(this.currentElementDataModel, cancellationToken).ConfigureAwait(false);
            var newRevision = deltaResponse.Value;

            if (!string.IsNullOrEmpty(newRevision))
            {
                foreach (var revision in revisions)
                {
                    if (revision.Id == this.currentRevision)
                    {
                        break;
                    }

                    newerRevisions.Add(revision.Id);
                }

                this.currentRevision = newRevision;
            }

            return this.currentElementDataModel;
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
            _sDKOptions.Storage.Save("LocalExchanges");
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
                this._sDKOptions?.Logger?.LogSkippedElement(SkippedElementType.Failed, "elementId");
                this._sDKOptions?.Logger?.LogSkippedElement(SkippedElementType.Failed, "elementId", "Line", "CurveSet");

                this.currentElementDataModel = (ElementDataModel)response.Value;

                ElementDataModel elementDataModel = await this.PrepareElementDataModel(exchangeItem);

                await this.Client.SyncExchangeDataAsync(dataExchangeIdentifier, elementDataModel);

                // Viewable generation is handled server-side in SDK 7.5.0; the client-side
                // Client.GenerateViewableAsync API was removed (no replacement).
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
            // Mutate the loaded model in place (ExchangeData wrapper is internal to the SDK)
            var elementDataModel = this.currentElementDataModel;

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
                elementDataModel.DeleteElementByUniqueId(existingElements[0].UniqueId);
            }
        }

        private async Task UpdateLocalExchange(ExchangeItem exchangeItem)
        {
            var loadExchangeStep = this.Client.ProgressStepsManager.GetProgressStep(ProgressStepId.LoadExchange);
            var dataExchangeIdentifier = CreateDataExchangeIdentifier(exchangeItem);

            DataExchange exchange = await base.GetExchangeAsync(dataExchangeIdentifier).ConfigureAwait(false);
            if (exchange != null)
            {
                exchangeItem.FileVersion = exchange.FileVersionId;
                exchangeItem.LastModified = exchange.Updated;
            }

            var localExchange = this.localStorage.FirstOrDefault(item => item.ExchangeID == exchange?.ExchangeID);
            if (localExchange != null && exchange != null)
            {
                if (localExchange.FileVersionId != exchange.FileVersionId)
                {
                    localExchange.FileVersionId = exchange.FileVersionId;
                }

                if (localExchange.Updated != exchange.Updated)
                {
                    localExchange.Updated = exchange.Updated;
                }
            }

            loadExchangeStep?.MarkAsComplete();
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
