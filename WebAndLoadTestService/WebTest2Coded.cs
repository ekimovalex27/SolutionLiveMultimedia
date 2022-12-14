//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.34014
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace WebAndLoadTestService {
    using System;
    using System.Collections.Generic;
    using System.Text;
    using Microsoft.VisualStudio.TestTools.WebTesting;
    using Microsoft.VisualStudio.TestTools.WebTesting.Rules;

  using LiveMultimediaService;
    
    public class WebTest2Coded : WebTest {
        
        public WebTest2Coded() {
            this.PreAuthenticate = true;
            this.Proxy = "default";
        }
        
        public override IEnumerator<WebTestRequest> GetRequestEnumerator() {
            // Инициализация правил проверки, применяемых ко всем запросам в веб-тесте
            if ((this.Context.ValidationLevel >= Microsoft.VisualStudio.TestTools.WebTesting.ValidationLevel.Low)) {
                ValidateResponseUrl validationRule1 = new ValidateResponseUrl();
                this.ValidateResponse += new EventHandler<ValidationEventArgs>(validationRule1.Validate);
            }
            if ((this.Context.ValidationLevel >= Microsoft.VisualStudio.TestTools.WebTesting.ValidationLevel.Low)) {
                ValidationRuleResponseTimeGoal validationRule2 = new ValidationRuleResponseTimeGoal();
                validationRule2.Tolerance = 0D;
                this.ValidateResponseOnPageComplete += new EventHandler<ValidationEventArgs>(validationRule2.Validate);
            }

            WebTestRequest request1 = new WebTestRequest("http://api.bing.com/qsml.aspx");
            request1.QueryStringParameters.Add("query", "ww", false, false);
            request1.QueryStringParameters.Add("maxwidth", "32765", false, false);
            request1.QueryStringParameters.Add("rowheight", "20", false, false);
            request1.QueryStringParameters.Add("sectionHeight", "200", false, false);
            request1.QueryStringParameters.Add("FORM", "IE11SS", false, false);
            request1.QueryStringParameters.Add("market", "ru", false, false);
            yield return request1;
            request1 = null;

            WebTestRequest request2 = new WebTestRequest("http://api.bing.com/qsml.aspx");
            request2.QueryStringParameters.Add("query", "www", false, false);
            request2.QueryStringParameters.Add("maxwidth", "32765", false, false);
            request2.QueryStringParameters.Add("rowheight", "20", false, false);
            request2.QueryStringParameters.Add("sectionHeight", "200", false, false);
            request2.QueryStringParameters.Add("FORM", "IE11SS", false, false);
            request2.QueryStringParameters.Add("market", "ru", false, false);
            yield return request2;
            request2 = null;

            WebTestRequest request3 = new WebTestRequest("http://api.bing.com/qsml.aspx");
            request3.ThinkTime = 1;
            request3.QueryStringParameters.Add("query", "www.", false, false);
            request3.QueryStringParameters.Add("maxwidth", "32765", false, false);
            request3.QueryStringParameters.Add("rowheight", "20", false, false);
            request3.QueryStringParameters.Add("sectionHeight", "200", false, false);
            request3.QueryStringParameters.Add("FORM", "IE11SS", false, false);
            request3.QueryStringParameters.Add("market", "ru", false, false);
            yield return request3;
            request3 = null;

            WebTestRequest request4 = new WebTestRequest("http://api.bing.com/qsml.aspx");
            request4.QueryStringParameters.Add("query", "www.l", false, false);
            request4.QueryStringParameters.Add("maxwidth", "32765", false, false);
            request4.QueryStringParameters.Add("rowheight", "20", false, false);
            request4.QueryStringParameters.Add("sectionHeight", "200", false, false);
            request4.QueryStringParameters.Add("FORM", "IE11SS", false, false);
            request4.QueryStringParameters.Add("market", "ru", false, false);
            yield return request4;
            request4 = null;

            WebTestRequest request5 = new WebTestRequest("http://api.bing.com/qsml.aspx");
            request5.QueryStringParameters.Add("query", "www.li", false, false);
            request5.QueryStringParameters.Add("maxwidth", "32765", false, false);
            request5.QueryStringParameters.Add("rowheight", "20", false, false);
            request5.QueryStringParameters.Add("sectionHeight", "200", false, false);
            request5.QueryStringParameters.Add("FORM", "IE11SS", false, false);
            request5.QueryStringParameters.Add("market", "ru", false, false);
            yield return request5;
            request5 = null;

            WebTestRequest request6 = new WebTestRequest("http://api.bing.com/qsml.aspx");
            request6.ThinkTime = 1;
            request6.QueryStringParameters.Add("query", "www.lie", false, false);
            request6.QueryStringParameters.Add("maxwidth", "32765", false, false);
            request6.QueryStringParameters.Add("rowheight", "20", false, false);
            request6.QueryStringParameters.Add("sectionHeight", "200", false, false);
            request6.QueryStringParameters.Add("FORM", "IE11SS", false, false);
            request6.QueryStringParameters.Add("market", "ru", false, false);
            yield return request6;
            request6 = null;

            WebTestRequest request7 = new WebTestRequest("http://api.bing.com/qsml.aspx");
            request7.QueryStringParameters.Add("query", "www.li", false, false);
            request7.QueryStringParameters.Add("maxwidth", "32765", false, false);
            request7.QueryStringParameters.Add("rowheight", "20", false, false);
            request7.QueryStringParameters.Add("sectionHeight", "200", false, false);
            request7.QueryStringParameters.Add("FORM", "IE11SS", false, false);
            request7.QueryStringParameters.Add("market", "ru", false, false);
            yield return request7;
            request7 = null;

            WebTestRequest request8 = new WebTestRequest("http://api.bing.com/qsml.aspx");
            request8.QueryStringParameters.Add("query", "www.liv", false, false);
            request8.QueryStringParameters.Add("maxwidth", "32765", false, false);
            request8.QueryStringParameters.Add("rowheight", "20", false, false);
            request8.QueryStringParameters.Add("sectionHeight", "200", false, false);
            request8.QueryStringParameters.Add("FORM", "IE11SS", false, false);
            request8.QueryStringParameters.Add("market", "ru", false, false);
            yield return request8;
            request8 = null;

            WebTestRequest request9 = new WebTestRequest("http://www.live-mm.com/");
            request9.ThinkTime = 2;
            yield return request9;
            request9 = null;

            WebTestRequest request10 = new WebTestRequest("http://www.live-mm.com/Demo.aspx");
            request10.ThinkTime = 2;
            request10.ExpectedResponseUrl = "http://www.live-mm.com/LiveMultimedia.aspx";
            request10.Headers.Add(new WebTestRequestHeader("Referer", "http://www.live-mm.com/"));
            ExtractHiddenFields extractionRule1 = new ExtractHiddenFields();
            extractionRule1.Required = true;
            extractionRule1.HtmlDecode = true;
            extractionRule1.ContextParameterName = "1";
            request10.ExtractValues += new EventHandler<ExtractionEventArgs>(extractionRule1.Extract);
            yield return request10;
            request10 = null;

            WebTestRequest request11 = new WebTestRequest("http://www.live-mm.com/LiveMultimedia.aspx");
            request11.ThinkTime = 1;
            request11.Method = "POST";
            request11.Headers.Add(new WebTestRequestHeader("X-Requested-With", "XMLHttpRequest"));
            request11.Headers.Add(new WebTestRequestHeader("X-MicrosoftAjax", "Delta=true"));
            request11.Headers.Add(new WebTestRequestHeader("Referer", "http://www.live-mm.com/LiveMultimedia.aspx"));
            FormPostHttpBody request11Body = new FormPostHttpBody();
            request11Body.FormPostParameters.Add("ctl00$ContentPlaceHolder1$ScriptManager1", "ctl00$ContentPlaceHolder1$PanelListMultimedia|ctl00$ContentPlaceHolder1$GridView1" +
                    "$ctl02$ctl01");
            request11Body.FormPostParameters.Add("__EVENTTARGET", this.Context["$HIDDEN1.__EVENTTARGET"].ToString());
            request11Body.FormPostParameters.Add("__EVENTARGUMENT", this.Context["$HIDDEN1.__EVENTARGUMENT"].ToString());
            request11Body.FormPostParameters.Add("__VIEWSTATE", this.Context["$HIDDEN1.__VIEWSTATE"].ToString());
            request11Body.FormPostParameters.Add("__EVENTVALIDATION", this.Context["$HIDDEN1.__EVENTVALIDATION"].ToString());
            request11Body.FormPostParameters.Add("ctl00$ContentPlaceHolder1$HiddenFieldGUID", this.Context["$HIDDEN1.ctl00$ContentPlaceHolder1$HiddenFieldGUID"].ToString());
            request11Body.FormPostParameters.Add("__ASYNCPOST", "true");
            request11Body.FormPostParameters.Add("ctl00$ContentPlaceHolder1$GridView1$ctl02$ctl01", "music");
            request11.Body = request11Body;
            ExtractHiddenFields extractionRule2 = new ExtractHiddenFields();
            extractionRule2.Required = true;
            extractionRule2.HtmlDecode = true;
            extractionRule2.ContextParameterName = "2";
            request11.ExtractValues += new EventHandler<ExtractionEventArgs>(extractionRule2.Extract);
            yield return request11;
            request11 = null;

            WebTestRequest request12 = new WebTestRequest("http://www.live-mm.com/LiveMultimedia.aspx");
            request12.Method = "POST";
            request12.Headers.Add(new WebTestRequestHeader("Referer", "http://www.live-mm.com/LiveMultimedia.aspx"));
            FormPostHttpBody request12Body = new FormPostHttpBody();
            request12Body.FormPostParameters.Add("ctl00$ContentPlaceHolder1$HiddenFieldGUID", this.Context["$HIDDEN1.ctl00$ContentPlaceHolder1$HiddenFieldGUID"].ToString());
            request12Body.FormPostParameters.Add("ctl00$ContentPlaceHolder1$ddlPlaylist", "13");
            request12Body.FormPostParameters.Add("__EVENTTARGET", this.Context["$HIDDEN2.__EVENTTARGET"].ToString());
            request12Body.FormPostParameters.Add("__EVENTARGUMENT", this.Context["$HIDDEN2.__EVENTARGUMENT"].ToString());
            request12Body.FormPostParameters.Add("__VIEWSTATE", this.Context["$HIDDEN2.__VIEWSTATE"].ToString());
            request12Body.FormPostParameters.Add("__CALLBACKID", "__Page");
            request12Body.FormPostParameters.Add("__CALLBACKPARAM", "1162108F-D1B9-486F-B765-300EE3C9F85C*99ADA1F7-8BB7-490F-92DC-DBCCBFA2AF37*1*mp3");
            request12Body.FormPostParameters.Add("__EVENTVALIDATION", this.Context["$HIDDEN2.__EVENTVALIDATION"].ToString());
            request12.Body = request12Body;
            yield return request12;
            request12 = null;

            WebTestRequest request13 = new WebTestRequest("http://www.live-mm.com/GetMultimedia.ashx");
            request13.ThinkTime = 88;
            request13.Headers.Add(new WebTestRequestHeader("Accept", "*/*"));
            request13.Headers.Add(new WebTestRequestHeader("Referer", "http://www.live-mm.com/LiveMultimedia.aspx"));
            request13.Headers.Add(new WebTestRequestHeader("Pragma", "getIfoFileURI.dlna.org"));
            request13.QueryStringParameters.Add("GUID", "1162108F-D1B9-486F-B765-300EE3C9F85C*99ADA1F7-8BB7-490F-92DC-DBCCBFA2AF37*1*mp3", false, false);
            yield return request13;
            request13 = null;

            WebTestRequest request14 = new WebTestRequest("http://www.live-mm.com/LiveMultimedia.aspx");
            request14.Method = "POST";
            request14.Headers.Add(new WebTestRequestHeader("Referer", "http://www.live-mm.com/LiveMultimedia.aspx"));
            FormPostHttpBody request14Body = new FormPostHttpBody();
            request14Body.FormPostParameters.Add("ctl00$ContentPlaceHolder1$HiddenFieldGUID", this.Context["$HIDDEN1.ctl00$ContentPlaceHolder1$HiddenFieldGUID"].ToString());
            request14Body.FormPostParameters.Add("ctl00$ContentPlaceHolder1$ddlPlaylist", "13");
            request14Body.FormPostParameters.Add("__EVENTTARGET", this.Context["$HIDDEN2.__EVENTTARGET"].ToString());
            request14Body.FormPostParameters.Add("__EVENTARGUMENT", this.Context["$HIDDEN2.__EVENTARGUMENT"].ToString());
            request14Body.FormPostParameters.Add("__VIEWSTATE", this.Context["$HIDDEN2.__VIEWSTATE"].ToString());
            request14Body.FormPostParameters.Add("__CALLBACKID", "__Page");
            request14Body.FormPostParameters.Add("__CALLBACKPARAM", "1162108F-D1B9-486F-B765-300EE3C9F85C*40BDD4AC-88EC-4E6D-A517-C63571B025DC*1*mp3");
            request14Body.FormPostParameters.Add("__EVENTVALIDATION", @"3CqjTFdhMiZlv9NnvXKTMyQyRGJ4fGgfUIrLlii5rQlFXNi9uBOLYKMoJdFttiZ/AXV+ZZpGiLVNKvCgPqzj5gQ6n8S0ZflT4eruw37hXgfrGbBDW2ObUvU9wY8wBG5GdDl6qjLCVcoh4M7ngqhq2FXrpAqBKBxr/CRe9kSrSp9JTkT3WTUGVnX8FfCV3kDMnLZQm230dPBumey7orCu4d93aLUzjzgfbMIK8WHmEHaJyj+J3yVMEHgZoI1yTk7LYHLH14kxX87aogbN5oU1Wta+9wAftclUbmr0BQJ+0Gbge6qLEzuYBAHyTsqx/z3DqFo223enbDAqjXGaZEhub5kEwSxezs+rYSGA6mJtAm0KBmF8WQddrglvMx0mn7jSb3YrQYlL1OP/HuomC2CDl5OaNUtaVgmK/OXmXXXPeWCpP13v");
            request14.Body = request14Body;
            yield return request14;
            request14 = null;

            WebTestRequest request15 = new WebTestRequest("http://www.live-mm.com/LiveMultimedia.aspx");
            request15.ThinkTime = 2;
            request15.Method = "POST";
            request15.Headers.Add(new WebTestRequestHeader("X-Requested-With", "XMLHttpRequest"));
            request15.Headers.Add(new WebTestRequestHeader("X-MicrosoftAjax", "Delta=true"));
            request15.Headers.Add(new WebTestRequestHeader("Referer", "http://www.live-mm.com/LiveMultimedia.aspx"));
            FormPostHttpBody request15Body = new FormPostHttpBody();
            request15Body.FormPostParameters.Add("ctl00$ContentPlaceHolder1$ScriptManager1", "ctl00$ContentPlaceHolder1$PanelListMultimedia|ctl00$ContentPlaceHolder1$GridView1" +
                    "$ctl02$ctl00");
            request15Body.FormPostParameters.Add("ctl00$ContentPlaceHolder1$HiddenFieldGUID", this.Context["$HIDDEN1.ctl00$ContentPlaceHolder1$HiddenFieldGUID"].ToString());
            request15Body.FormPostParameters.Add("ctl00$ContentPlaceHolder1$ddlPlaylist", "13");
            request15Body.FormPostParameters.Add("__EVENTTARGET", this.Context["$HIDDEN2.__EVENTTARGET"].ToString());
            request15Body.FormPostParameters.Add("__EVENTARGUMENT", this.Context["$HIDDEN2.__EVENTARGUMENT"].ToString());
            request15Body.FormPostParameters.Add("__VIEWSTATE", this.Context["$HIDDEN2.__VIEWSTATE"].ToString());
            request15Body.FormPostParameters.Add("__EVENTVALIDATION", @"Yr9IzUBVCbEtooPPY5LIXNZykgRbiYzXengZsh1jZAnUYQRUNUmXap9isWY4gUfo843xLBWbeY6kXggVxRDL+jm5ioorCDLMR1hHe6oVzzTuzvoP/EhFZdbeSFzGoNLwmjyganouGqv2n3/Wvj+zBK8O61c8kIQ239Bs392sndJop8l9vtxUrCY1DlYHesKxJ3/NQgpYkSj94R7J9JHxwx34mx1zoDU/IcstylRxmu4XNjTTdac7/RfYgNFFM7J0bmptY9/fzwTUF9fnZSbK3jKJuzuxdNpBJHIiaV+AT5navg8n+d0xHKtqpfbbFi5FE8ueL9GuxCVSEx1X53FohTO4GXdEDHtTcidH36VHU1lcG69MLgFQbvaNWp+6JLAaQqt/mcTQTEvUwT1nHmrRrc7LcVs8+VDxPB5g0bh1gXwFcn0z");
            request15Body.FormPostParameters.Add("__ASYNCPOST", "true");
            request15Body.FormPostParameters.Add("ctl00$ContentPlaceHolder1$GridView1$ctl02$ctl00", "Home Albums");
            request15.Body = request15Body;
            yield return request15;
            request15 = null;

            WebTestRequest request16 = new WebTestRequest("http://www.live-mm.com/Logout.aspx");
            request16.ExpectedResponseUrl = "http://www.live-mm.com/Default.aspx";
            request16.Headers.Add(new WebTestRequestHeader("Referer", "http://www.live-mm.com/LiveMultimedia.aspx"));
            yield return request16;
            request16 = null;
        }
    }
}
