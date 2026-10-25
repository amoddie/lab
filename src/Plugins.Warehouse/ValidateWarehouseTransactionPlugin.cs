using System.Globalization;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using TALXIS.DevKit.Build.Dataverse.Plugin;

namespace Plugins.Warehouse
{
    [PluginStep("Create", "lab_warehousetransaction", PluginStage.PreValidation)]
    public sealed class ValidateWarehouseTransactionPlugin : PluginBase
    {
        private const int Outbound = 100000001;

        public ValidateWarehouseTransactionPlugin(string unsecureConfiguration, string secureConfiguration)
            : base(typeof(ValidateWarehouseTransactionPlugin))
        {
        }

        protected override void ExecuteDataversePlugin(ILocalPluginContext localPluginContext)
        {
            var executionContext = localPluginContext.PluginExecutionContext;
            if (!executionContext.InputParameters.Contains("Target") ||
                !(executionContext.InputParameters["Target"] is Entity target) ||
                target.LogicalName != "lab_warehousetransaction")
            {
                return;
            }

            if (!target.Contains("lab_quantity") ||
                !target.Contains("lab_itemid") ||
                !target.Contains("lab_transactiontype"))
            {
                return;
            }

            var itemReference = target.GetAttributeValue<EntityReference>("lab_itemid");
            var transactionType = target.GetAttributeValue<OptionSetValue>("lab_transactiontype");
            if (itemReference == null || transactionType == null || transactionType.Value != Outbound)
            {
                return;
            }

            var quantity = target.GetAttributeValue<int>("lab_quantity");
            var service = localPluginContext.OrgSvcFactory.CreateOrganizationService(executionContext.UserId);
            var item = service.Retrieve("lab_warehouseitem", itemReference.Id, new ColumnSet("lab_availablequantity"));
            var available = item.GetAttributeValue<int?>("lab_availablequantity") ?? 0;

            if (quantity > available)
            {
                throw new InvalidPluginExecutionException(string.Format(
                    CultureInfo.InvariantCulture,
                    "Not enough product in stock. Available: {0}, requested: {1}.",
                    available,
                    quantity));
            }
        }
    }
}