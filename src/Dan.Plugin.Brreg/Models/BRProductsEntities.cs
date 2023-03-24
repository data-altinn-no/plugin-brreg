using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Dan.Plugin.Brreg.Models
{

    #region #Outbound DataContracts
    /// <summary>
    /// Definition of product. (output to infopath forms)
    /// </summary>
    [DataContract]
    public class Product
    {
        [DataMember]
        public string Name;

        [DataMember]
        public int Code;

        [DataMember]
        public int DeliveryMethod;

        [DataMember]
        public int DeliveryUnit;

        [DataMember]
        public int accountYear;
    }

    /// <summary>
    /// Definition of ordered product after production. (output to infopath forms)
    /// </summary>
    [DataContract]
    public class OrderedProduct
    {
        [DataMember]
        public string Name;

        [DataMember]
        public int Code;

        [DataMember]
        public int DeliveryMethod;

        [DataMember]
        public int AccountYear;

        [DataMember]
        public string Url;

        [DataMember]
        public bool Produced;

        [DataMember]
        public string Bestref;

        [DataMember]
        public int Lnr;
    }

    [DataContract]
    public class Person
    {
        [DataMember]
        public string Fnr;

        [DataMember]
        public string Fornavn;

        [DataMember]
        public string Mellomnavn;

        [DataMember]
        public string Slektsnavn;

        [DataMember]
        public string Adresse1;

        [DataMember]
        public string Adresse2;

        [DataMember]
        public string Adresse3;

        [DataMember]
        public string Postnr;

        [DataMember]
        public string Poststed;

        [DataMember]
        public string Landkode;
    }

    [DataContract]
    public class Enhet
    {
        [DataMember]
        public string Navn1;

        [DataMember]
        public string Navn2;

        [DataMember]
        public string Navn3;

        [DataMember]
        public string Navn4;

        [DataMember]
        public string Navn5;

        [DataMember]
        public string OrgFormDesc;

        [DataMember]
        public string OrgFormKode;

        [DataMember]
        public string Orgnr;

        [DataMember]
        public Postadresse Postadr;

        [DataMember]
        public Forretningsadresse Forretningsadr;

        [DataMember]
        public int Hovedstatus;

        [DataMember]
        public int?[] UnderStatuser;
    }

    [DataContract]
    public class Forretningsadresse
    {
        [DataMember]
        public string Adresse1;
        [DataMember]
        public string Adresse2;
        [DataMember]
        public string Adresse3;
        [DataMember]
        public string Land;
        [DataMember]
        public string Postnummer;
        [DataMember]
        public string Poststed;

    }

    [DataContract]
    public class Postadresse
    {
        [DataMember]
        public string Adresse1;
        [DataMember]
        public string Adresse3;
        [DataMember]
        public string Adresse2;
        [DataMember]
        public string Land;
        [DataMember]
        public string Postnummer;
        [DataMember]
        public string Poststed;

    }

    #region #WrapperContracts


    [DataContract]
    public abstract class ResponseBase
    {
        [DataMember]
        public string StatusText;

        [DataMember]
        public bool Status;
    }

    [DataContract]
    public class PersonResponse : ResponseBase
    {
        [DataMember]
        public Person Person;
    }

    [DataContract]
    public class ProductsResponse : ResponseBase
    {
        [DataMember]
        public Product[] Products;
    }

    [DataContract]
    public class OrderedProductsResponse : ResponseBase
    {
        [DataMember]
        public OrderedProduct[] OrderedProducts;
    }

    [DataContract]
    public class EnhetsResponse : ResponseBase
    {
        [DataMember]
        public Enhet Enhetsdata;
    }

    #endregion

    #endregion

    #region #Inbound DataContracts
    [DataContract]
    public abstract class BaseRequest
    {

    }

    [DataContract]
    public class GetEnhetRequest : BaseRequest
    {
    }

    [DataContract]
    public class GetProductListRequest : BaseRequest
    {
    }

    [DataContract]
    public class OrderProductRequest : BaseRequest
    {
        [DataMember]
        public string Language;

        [DataMember]
        public Product[] Products;
    }

}
#endregion
