using BR_productsservice;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Text;
using System.Threading.Tasks;

namespace Nadobe.EvidenceSources.ES_BR
{
    class BRProductsUtils
    {
        private const string System = "ALTIN"; //!sic
      
        internal static Product[] Convert(productImpl[] products)
        {
            if (products != null)
            {
                return products.Select(Convert).ToArray();
            }
            return null;
        }

        internal static Product Convert(productImpl product)
        {
            if (product != null)
            {
                return new Product()
                {
                    Name = product.name,
                    DeliveryUnit = product.deliveryUnit,
                    DeliveryMethod = product.deliveryMethod,
                    Code = product.code,
                    accountYear = product.accountYear
                };
            }
            return null;
        }
        internal static productImpl[] Convert(Product[] products)
        {
            if (products != null)
            {
                return products.Select(Convert).ToArray();
            }
            return null;
        }

        /// <summary>
        /// Converts a productProduced(br) object to a OrderedProduct(mapper) object
        /// </summary>
        /// <param name="product"></param>
        /// <returns></returns>
        internal static OrderedProduct Convert(productProduced product)
        {
            if (product != null)
            {
                return new OrderedProduct()
                {
                    AccountYear = product.accountingYear,
                    Bestref = product.bestref,
                    Code = product.code,
                    DeliveryMethod = product.deliveryMethod,
                    Lnr = product.lnr,
                    Name = product.name,
                    Url = product.URL,
                    Produced = true
                };
            }
            return null;
        }

        /// <summary>
        /// Converts an array of productProduced(br) to an array of OrderedProduct(mapper)
        /// </summary>
        /// <param name="products"></param>
        /// <returns></returns>
        internal static OrderedProduct[] Convert(productProduced[] products)
        {
            if (products != null)
            {
                return products.Select(Convert).ToArray();
            }
            return null;
        }

        internal static productImpl Convert(Product product)
        {
            if (product != null)
            {
                return new productImpl()
                {
                    accountYear = product.accountYear,
                    code = product.Code,
                    deliveryMethod = product.DeliveryMethod,
                    deliveryUnit = product.DeliveryUnit,
                    name = product.Name,
                    system = System
                };
            }
            return null;
        }        
    }
   
}

