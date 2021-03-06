﻿using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Dispatcher;
using System.Web.OData;
using System.Web.OData.Extensions;
using Nuwa;
using WebStack.QA.Common.WebHost;
using WebStack.QA.Common.XUnit;
using WebStack.QA.Test.OData.Common;
using Xunit;
using Xunit.Extensions;

namespace WebStack.QA.Test.OData.UriParserExtension
{
    [NuwaFramework]
    [NuwaTrace(NuwaTraceAttribute.Tag.Off)]
    public class EnumPrefixFreeTest
    {
        [NuwaBaseAddress]
        public string BaseAddress { get; set; }

        [NuwaHttpClient]
        public HttpClient Client { get; set; }

        [NuwaConfiguration]
        public static void UpdateConfiguration(HttpConfiguration configuration)
        {
            var controllers = new[] { typeof(CustomersController), typeof(OrdersController), typeof(MetadataController) };
            TestAssemblyResolver resolver = new TestAssemblyResolver(new TypesInjectionAssembly(controllers));

            configuration.IncludeErrorDetailPolicy = IncludeErrorDetailPolicy.Always;
            configuration.Services.Replace(typeof(IAssembliesResolver), resolver);

            configuration.EnableEnumPrefixFree(true);

            configuration.Routes.Clear();

            configuration.MapODataServiceRoute(routeName: "odata",
                routePrefix: "odata", model: UriParserExtenstionEdmModel.GetEdmModel());

            configuration.EnsureInitialized();
        }

        [NuwaWebConfig]
        public static void UpdateWebConfig(WebConfigHelper webConfig)
        {
            webConfig.AddRAMFAR(true);
        }

        public static TheoryDataSet<string, string, HttpStatusCode> EnumPrefixFreeCases
        {
            get
            {
                return new TheoryDataSet<string, string, HttpStatusCode>()
                {
                    { "gender=WebStack.QA.Test.OData.UriParserExtension.Gender'Male'", "gender='Male'", HttpStatusCode.OK },
                    { "gender=WebStack.QA.Test.OData.UriParserExtension.Gender'UnknownValue'", "gender='UnknownValue'", HttpStatusCode.NotFound },
                };
            }
        }

        [Theory]
        [PropertyData("EnumPrefixFreeCases")]
        public async Task EnableEnumPrefixFreeTest(string prefix, string prefixFree, HttpStatusCode statusCode)
        {
            // Enum with prefix
            var prefixUri = string.Format("{0}/odata/Customers/Default.GetCustomerByGender({1})", this.BaseAddress, prefix);
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, prefixUri);
            HttpResponseMessage response = await Client.SendAsync(request);

            Assert.Equal(statusCode, response.StatusCode);
            string prefixResponse = await response.Content.ReadAsStringAsync();

            // Enum prefix free
            var prefixFreeUri = string.Format("{0}/odata/Customers/Default.GetCustomerByGender({1})", this.BaseAddress, prefixFree);
            request = new HttpRequestMessage(HttpMethod.Get, prefixFreeUri);
            response = await Client.SendAsync(request);

            Assert.Equal(statusCode, response.StatusCode);
            string prefixFreeResponse = await response.Content.ReadAsStringAsync();

            if (statusCode == HttpStatusCode.OK)
            {
                Assert.Equal(prefixResponse, prefixFreeResponse);
            }
        }
    }
}
