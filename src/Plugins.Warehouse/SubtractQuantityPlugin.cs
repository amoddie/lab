using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using TALXIS.DevKit.Build.Dataverse.Plugin;

namespace Plugins.Warehouse
{
    [PluginStep("Create", "lab_warehousetransaction", PluginStage.PostOperation)]
    public sealed class SubtractQuantityPlugin : PluginBase
    {
        private const int Inbound = 100000000;
        private const int Outbound = 100000001;

        public SubtractQuantityPlugin(string unsecureConfiguration, string secureConfiguration)
            : base(typeof(SubtractQuantityPlugin))
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
            if (itemReference == null || transactionType == null)
            {
                return;
            }

            var quantity = target.GetAttributeValue<int>("lab_quantity");
            var service = localPluginContext.OrgSvcFactory.CreateOrganizationService(executionContext.UserId);
            var item = service.Retrieve("lab_warehouseitem", itemReference.Id, new ColumnSet("lab_availablequantity"));
            var available = item.GetAttributeValue<int?>("lab_availablequantity") ?? 0;

            int updatedQuantity;
            if (transactionType.Value == Inbound)
            {
                updatedQuantity = available + quantity;
            }
            else if (transactionType.Value == Outbound)
            {
                updatedQuantity = available - quantity;
            }
            else
            {
                return;
            }

            var itemUpdate = new Entity("lab_warehouseitem", itemReference.Id)
            {
                ["lab_availablequantity"] = updatedQuantity
            };
            service.Update(itemUpdate);
        }
    }
}