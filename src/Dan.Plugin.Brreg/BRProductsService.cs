using BR_productsservice;
using Dan.Plugin.Brreg.Config;
using System;
using System.Linq;
using System.ServiceModel;
using System.Threading.Tasks;
using System.ServiceModel.Channels;

namespace Nadobe.EvidenceSources.ES_BR
{
    class BRProductsService
    {
        private const int DeliveryMethod = 26;
        private const int DeliveryUnit = 49;       

        internal static async Task<Product[]> GetProductList(string organization, int reportCode, Settings settings)
        {            
            Product[] result;
            var request = new getProductListRequest()
            {                                   
                orgnr = organization
            };

            using (var proxy = new ProductsClient())
            using (var scope = new FlowingOperationContextScope(proxy.InnerChannel))
            {
                AddCredentialsToRequest(settings);               
                proxy.Endpoint.Address = new EndpointAddress(settings.BR_endpoint_address);
                var response = await proxy.getProductListAsync(request).ContinueOnScope(scope);
                result = BRProductsUtils.Convert(response.@return);
            }

            return result == null ? null : (from t in result where t.Code == reportCode select t).ToArray();
        }

        internal static async Task<OrderedProduct[]> OrderProducts(string organization, Product[] productOrderList, Settings settings)
        {
            var productImplementationList = BRProductsUtils.Convert(productOrderList);
            OrderedProduct[] result;

            var request = new orderProductsRequest
            {
                enhetsnr = organization,
                language = "NO",
                products = productImplementationList
            };

            using (var proxy = new ProductsClient())
            using (var scope = new FlowingOperationContextScope(proxy.InnerChannel))
            {
                AddCredentialsToRequest(settings);               
                proxy.Endpoint.Address = new EndpointAddress(settings.BR_endpoint_address);
                var response = await proxy.orderProductsAsync(request).ContinueOnScope(scope);
                result = BRProductsUtils.Convert(response.@return);
            }

            return result;
        }

        private static void AddCredentialsToRequest(Settings settings)
        {            
            OperationContext.Current.OutgoingMessageHeaders.Add(MessageHeader.CreateHeader("userid", "oa.brreg.no", settings.ES_BR_ProductsUserName.Trim()));
            OperationContext.Current.OutgoingMessageHeaders.Add(MessageHeader.CreateHeader("password", "oa.brreg.no", settings.ES_BR_ProductsPassword.Trim()));
        }
    }
}
