﻿using AngleSharp.Html.Parser;
using bbxBE.Application.Commands.cmdNAV;
using bbxBE.Application.Queries.qCustomer;
using bbxBE.Common;
using bbxBE.Common.Consts;
using bbxBE.Common.Enums;
using bbxBE.Common.Exceptions;
using bbxBE.Common.NAV;
using bbxBE.Domain.Entities;
using bbxBE.Domain.Settings;
using bxBE.Application.Commands.cmdEmail;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace bbxBE.Application.BLL
{
    public class bllNAV
    {
        NAVSettings _NAVSettings { get; }
        private readonly ILogger _logger;

        public bllNAV(NAVSettings p_NAVSettings, ILogger logger)
        {
            _NAVSettings = p_NAVSettings;
            _logger = logger;
        }

        public bool NAVPost(string p_uri, string p_requestId, string p_content, string p_procname, out string o_response)
        {
            o_response = "";
            try
            {
                System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;

                p_content = p_content.Replace("xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\"", "");
                p_content = p_content.Replace("xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\"", "");

                //Util.Log2File(String.Format("{0} NAV POST. requestId:{1}, uri:{2}, Content:\n{3} ", p_procname, p_requestId, p_uri, p_content), Global.POSTLOG_NAME);

                var request = (HttpWebRequest)WebRequest.Create(p_uri);
                request.Method = "POST";
                request.ContentType = "application/xml; charset=utf-8";
                //     request.ContentType = "application/xml";
                byte[] postBytes = Encoding.UTF8.GetBytes(p_content);
                request.ContentLength = postBytes.Length;
                var requestStream = request.GetRequestStream();
                requestStream.Write(postBytes, 0, postBytes.Length);
                requestStream.Close();

                return GetNAVResponse(request, p_requestId, p_content, p_procname, out o_response);
            }
            catch (Exception)
            {
                //Util.Log2File(String.Format("{0} NAV POST exception. p_requestId:{1}, uri:{2}, Content:\n{3}", p_procname, p_requestId, p_uri, p_content), Global.POSTLOG_NAME);
                //Util.ExceptionLog(ex);
                //ExceptionDispatchInfo.Capture(ex).Throw();
                throw;
            }
        }

        private bool GetNAVResponse(WebRequest request, string p_requestId, string p_content, string p_procname, out string o_response)
        {
            o_response = "";
            try
            {

                HttpWebResponse response = null;
                try
                {
                    response = (HttpWebResponse)request.GetResponse();
                }
                catch (WebException)
                {
                    throw;
                    //      response = (HttpWebResponse)ex.Response;
                }

                if (response != null)
                {

                    //Response kiolvasása
                    long length = response.ContentLength;
                    Stream stream = response.GetResponseStream();
                    StreamReader reader = new StreamReader(stream, Encoding.UTF8);
                    o_response = reader.ReadToEnd();


                    if (response.StatusCode.ToString() == NAVGlobal.NAV_OK)
                    {
                        //Util.Log2File(String.Format("{0} NAV OK response. requestId:{1}, status:{2}, response length:{3}, response:{4}", p_procname, p_requestId, response.StatusDescription, response.ContentLength, o_response), Global.POSTLOG_NAME);
                        Console.WriteLine(String.Format("{0} NAV OK response. requestId:{1}, status:{2}, response length:{3}, response:{4}", p_procname, p_requestId, response.StatusDescription, response.ContentLength, o_response));
                        return true;
                    }
                    else
                    {
                        // Util.Log2File(String.Format("{0} NAV error response. requestId:{1}, status:{2}, response:{3}", p_procname, p_requestId, response.StatusDescription, o_response), Global.POSTLOG_NAME);
                        //                        Console.WriteLine(String.Format("{0} NAV error response. requestId:{1}, status:{2}, response:{3}", p_procname, p_requestId, response.StatusDescription, o_response));
                        throw new NAVException(String.Format("{ 0} NAV error response. requestId:{1}, status:{2}, response:{3}", p_procname, p_requestId, response.StatusDescription, o_response));
                        //      return false;
                    }
                }
                return false;
            }
            catch (Exception)
            {
                //Util.Log2File(String.Format("{0} NAV GetResponse exception. requestId:{1}", p_procname, p_requestId), Global.POSTLOG_NAME);
                Console.WriteLine(String.Format("{0} NAV GetResponse exception. requestId:{1}", p_procname, p_requestId));
                //Util.ExceptionLog(we);
                //ExceptionDispatchInfo.Capture(ex).Throw();
                throw;
            }
        }

        public List<InvoiceDigestType> QueryInvoiceDigest(importFromNAVCommand request)
        {
            var invoiceDigestRes = new List<InvoiceDigestType>();

            var ter = new TokenExchangeRequest(_NAVSettings.Taxnum, _NAVSettings.TechUser, _NAVSettings.TechUserPwd, _NAVSettings.SignKey);

            var reqTer = XMLUtil.Object2XMLString<TokenExchangeRequest>(ter, Encoding.UTF8, NAVGlobal.XMLNamespaces);

            string response = "";
            if (NAVPost(_NAVSettings.TokenExchange, ter.header.requestId, reqTer, MethodBase.GetCurrentMethod().Name, out response))
            {
                TokenExchangeResponse resp = XMLUtil.XMLStringToObject<TokenExchangeResponse>(response);
                var page = 1;

                var dig = new QueryInvoiceDigestRequest(_NAVSettings.Taxnum, _NAVSettings.TechUser, _NAVSettings.TechUserPwd, _NAVSettings.SignKey,
                                page, Enum.Parse<InvoiceDirectionType>(request.InvoiceDirection), true, request.IssueDateFrom, request.IssueDateTo);

                var digTer = XMLUtil.Object2XMLString<QueryInvoiceDigestRequest>(dig, Encoding.UTF8, NAVGlobal.XMLNamespaces);

                if (NAVPost(_NAVSettings.QueryInvoiceDigest, dig.header.requestId, digTer, MethodBase.GetCurrentMethod().Name, out response))
                {

                    QueryInvoiceDigestResponse respDigest = XMLUtil.XMLStringToObject<QueryInvoiceDigestResponse>(response);
                    if (respDigest.result.funcCode == FunctionCodeType.OK)
                    {
                        if (respDigest.invoiceDigestResult.availablePage > 0)
                        {
                            invoiceDigestRes.AddRange(respDigest.invoiceDigestResult.invoiceDigest.ToList());
                            var availablePage = respDigest.invoiceDigestResult.availablePage;
                            while (page++ < availablePage)
                            {
                                var digPg = new QueryInvoiceDigestRequest(_NAVSettings.Taxnum, _NAVSettings.TechUser, _NAVSettings.TechUserPwd, _NAVSettings.SignKey,
                                     page, Enum.Parse<InvoiceDirectionType>(request.InvoiceDirection), true, request.IssueDateFrom, request.IssueDateTo);

                                var digTerPg = XMLUtil.Object2XMLString<QueryInvoiceDigestRequest>(digPg, Encoding.UTF8, NAVGlobal.XMLNamespaces);

                                if (NAVPost(_NAVSettings.QueryInvoiceDigest, digPg.header.requestId, digTerPg, MethodBase.GetCurrentMethod().Name, out response))
                                {
                                    QueryInvoiceDigestResponse respDigestPg = XMLUtil.XMLStringToObject<QueryInvoiceDigestResponse>(response);
                                    if (respDigestPg.result.funcCode == FunctionCodeType.OK)
                                    {
                                        invoiceDigestRes.AddRange(respDigestPg.invoiceDigestResult.invoiceDigest.ToList());
                                    }
                                }
                                else
                                {
                                    var errmsg = String.Format(bbxBEConsts.NAV_QINVDIGEST_NEXTPG_ERR, MethodBase.GetCurrentMethod().Name, response);
                                    _logger.Error(errmsg);
                                    throw new NAVException(errmsg);
                                }
                            }
                        }
                    }
                    else
                    {
                        var errmsg = String.Format(bbxBEConsts.NAV_QINVDIGEST_FIRSTPG_ERR, MethodBase.GetCurrentMethod().Name, response);
                        _logger.Error(errmsg);
                        throw new NAVException(errmsg);
                    }

                    string msg = String.Format(bbxBEConsts.NAV_QINVDIGEST_OK, MethodBase.GetCurrentMethod().Name,
                        request.InvoiceDirection, true, request.IssueDateFrom, request.IssueDateTo);
                    Console.WriteLine(msg);
                    _logger.Information(msg);
                }
            }
            else
            {
                var msg = String.Format(bbxBEConsts.NAV_TOKENEXCHANGE_ERR, MethodBase.GetCurrentMethod().Name, response);
                _logger.Error(msg);
                throw new NAVException(msg);
            }

            return invoiceDigestRes;
        }

        public InvoiceData QueryInvoiceData(string p_invoiceNumber, InvoiceDirectionType p_invoiceDirection)
        {
            InvoiceData result = null;

            var ter = new TokenExchangeRequest(_NAVSettings.Taxnum, _NAVSettings.TechUser, _NAVSettings.TechUserPwd, _NAVSettings.SignKey);
            var reqTer = XMLUtil.Object2XMLString<TokenExchangeRequest>(ter, Encoding.UTF8, NAVGlobal.XMLNamespaces);
            string response = "";
            if (NAVPost(_NAVSettings.TokenExchange, ter.header.requestId, reqTer, MethodBase.GetCurrentMethod().Name, out response))
            {
                TokenExchangeResponse resp = XMLUtil.XMLStringToObject<TokenExchangeResponse>(response);

                var invData = new QueryInvoiceDataRequest(_NAVSettings.Taxnum, _NAVSettings.TechUser, _NAVSettings.TechUserPwd, _NAVSettings.SignKey,
                                p_invoiceNumber, p_invoiceDirection);

                var invDataReq = XMLUtil.Object2XMLString<QueryInvoiceDataRequest>(invData, Encoding.UTF8, NAVGlobal.XMLNamespaces);

                if (NAVPost(_NAVSettings.QueryInvoiceData, invData.header.requestId, invDataReq, MethodBase.GetCurrentMethod().Name, out response))
                {

                    QueryInvoiceDataResponse respInvData = XMLUtil.XMLStringToObject<QueryInvoiceDataResponse>(response);
                    if (respInvData.result.funcCode == FunctionCodeType.OK)
                    {
                        var respInvoiceData = XMLUtil.XMLStringToObject<QueryInvoiceDataResponse>(response);
                        if (respInvoiceData.invoiceDataResult != null && respInvoiceData.invoiceDataResult != null)
                        {
                            string InvoiceDataStr = "";
                            if (respInvoiceData.invoiceDataResult.compressedContentIndicator)
                            {
                                //TODO: Tömörített számlám még nincs, tesztelni !!! a megoldás elvileg jó.
                                InvoiceDataStr = Utils.Unzip(respInvoiceData.invoiceDataResult.invoiceData);
                                throw new NotImplementedException("Tömörített számlát tesztelni !!! (compressedContentIndicator)");
                            }
                            else
                            {
                                InvoiceDataStr = Encoding.UTF8.GetString(respInvoiceData.invoiceDataResult.invoiceData);
                            }
                            result = XMLUtil.XMLStringToObject<InvoiceData>(InvoiceDataStr);
                        }
                        else
                        {
                            var errmsg = String.Format(bbxBEConsts.NAV_QINVDATA_NOTFND_ERR, MethodBase.GetCurrentMethod().Name, p_invoiceNumber);
                            _logger.Error(errmsg);
                            throw new NAVException(errmsg);
                        }

                    }
                    else
                    {
                        var errmsg = String.Format(bbxBEConsts.NAV_QINVDATA_ERR, MethodBase.GetCurrentMethod().Name, response);
                        _logger.Error(errmsg);
                        throw new NAVException(errmsg);
                    }

                    var msg = String.Format(bbxBEConsts.NAV_QINVDATA_OK, MethodBase.GetCurrentMethod().Name, resp.result.funcCode,
                                   (resp.result.errorCode != null ? resp.result.errorCode : ""), (resp.result.message != null ? resp.result.message : ""));
                    Console.WriteLine(msg);
                    _logger.Information(msg);
                }
            }
            else
            {
                var msg = String.Format(bbxBEConsts.NAV_TOKENEXCHANGE_ERR, MethodBase.GetCurrentMethod().Name, response);
                _logger.Error(msg);
                throw new NAVException(msg);
            }

            return result;
        }

        public TaxpayerDataType QueryTaxPayer(QueryTaxPayer request)
        {
            var invoiceDigestRes = new List<InvoiceDigestType>();

            var ter = new TokenExchangeRequest(_NAVSettings.Taxnum, _NAVSettings.TechUser, _NAVSettings.TechUserPwd, _NAVSettings.SignKey);

            var reqTer = XMLUtil.Object2XMLString<TokenExchangeRequest>(ter, Encoding.UTF8, NAVGlobal.XMLNamespaces);
            TaxpayerDataType result = null;
            string response = "";
            string msg = "";
            if (NAVPost(_NAVSettings.TokenExchange, ter.header.requestId, reqTer, MethodBase.GetCurrentMethod().Name, out response))
            {
                TokenExchangeResponse resp = XMLUtil.XMLStringToObject<TokenExchangeResponse>(response);

                var qtp = new QueryTaxpayerRequest(_NAVSettings.Taxnum, _NAVSettings.TechUser, _NAVSettings.TechUserPwd, _NAVSettings.SignKey, request.Taxnumber);

                var qtpTer = XMLUtil.Object2XMLString<QueryTaxpayerRequest>(qtp, Encoding.UTF8, NAVGlobal.XMLNamespaces);

                if (NAVPost(_NAVSettings.QueryTaxPayer, qtp.header.requestId, qtpTer, MethodBase.GetCurrentMethod().Name, out response))
                {
                    QueryTaxpayerResponse respQtp = XMLUtil.XMLStringToObject<QueryTaxpayerResponse>(response);
                    if (respQtp.taxpayerData != null)
                    {
                        result = respQtp.taxpayerData;
                        msg = String.Format(bbxBEConsts.NAV_QTAXPAYERT_OK, MethodBase.GetCurrentMethod().Name, request.Taxnumber);
                        Console.WriteLine(msg);
                        _logger.Information(msg);
                    }
                    else
                    {
                        // egyelőre csak logoljuk a hibát
                        msg = String.Format(bbxBEConsts.NAV_QTAXPAYER_ERR, MethodBase.GetCurrentMethod().Name, request.Taxnumber, response);
                        _logger.Error(msg);
                    }
                }
            }
            else
            {
                // egyelőre csak logoljuk a hibát
                msg = String.Format(bbxBEConsts.NAV_QTAXPAYER_TOKEN_ERR, MethodBase.GetCurrentMethod().Name, request.Taxnumber, response);
                _logger.Error(msg);
            }
            return result;
        }

        private static void ProcessGeneralErrorResponse(NAVXChange xChangeResult, string generalErrResp)
        {
            xChangeResult.Status = enNAVStatus.ERROR.ToString();
            xChangeResult.SendResponse = generalErrResp;
            // Általános hibák nem blokkolják a feldolgozást, 
            // ezért a Status nincs ERROR-ra állítva. A program a következő menetben 
            // újra probálja a beküldést
            if (generalErrResp.Contains(bbxBEConsts.DEF_NAVGeneralExceptionResponse))
            {
                GeneralExceptionResponse gex = XMLUtil.XMLStringToObject<GeneralExceptionResponse>(generalErrResp);

                //                                p_XChange.Status = Global.NAV_STATUS_ERROR;
                xChangeResult.SendResponse = gex.funcCode.ToString();
                xChangeResult.SendMessage = (gex.errorCode + " " + gex.message).Trim();
            }

            if (generalErrResp.Contains(bbxBEConsts.DEF_NAVGeneralErrorResponse))
            {
                GeneralErrorResponseType ger = XMLUtil.XMLStringToObject<GeneralErrorResponseType>(generalErrResp);
                xChangeResult.SendResponse = ger.result.funcCode.ToString();
                xChangeResult.SendMessage = (ger.result.errorCode + " " + ger.result.message).Trim();

                //                                p_XChange.Status = Global.NAV_STATUS_ERROR;
                if (ger.technicalValidationMessages != null)
                {
                    xChangeResult.NAVXResults = new List<NAVXResult>();

                    foreach (var err in ger.technicalValidationMessages)
                    {
                        var xres = new NAVXResult()
                        {
                            ResultCode = err.validationResultCode.ToString(),
                            ErrorCode = err.validationErrorCode,
                            Message = err.message,
                        };
                        xChangeResult.NAVXResults.Add(xres);
                    }
                }
            }
        }

        public static void ProcessTransactionResult(NAVXChange xChangeResult, QueryTransactionStatusResponse p_resp)
        {
            if (xChangeResult.NAVXResults == null)
            {
                xChangeResult.NAVXResults = new List<NAVXResult>();
            }
            if (p_resp.processingResults != null)
            {
                foreach (var pres in p_resp.processingResults.processingResult)
                {
                    if (pres.businessValidationMessages != null)
                    {
                        foreach (var businessres in pres.businessValidationMessages)
                        {
                            NAVXResult xres = new NAVXResult()
                            {
                                ErrorCode = businessres.validationErrorCode,
                                ResultCode = businessres.validationResultCode.ToString(),
                                Message = businessres.message,
                                Tag = businessres.pointer.tag,
                                Value = businessres.pointer.value,
                                Line = businessres.pointer.line
                            };
                            xChangeResult.NAVXResults.Add(xres);
                        }
                    }

                    if (pres.technicalValidationMessages != null)
                    {
                        if (pres.technicalValidationMessages != null)
                        {
                            foreach (var techres in pres.technicalValidationMessages)
                            {
                                NAVXResult xres = new NAVXResult()
                                {
                                    ErrorCode = techres.validationErrorCode,
                                    ResultCode = techres.validationResultCode.ToString(),
                                    Message = techres.message
                                };
                                xChangeResult.NAVXResults.Add(xres);
                            }
                        }
                    }
                }
            }
        }

        public NAVXChange CallManageInvoiceFull(Invoice invoice)
        {
            var resNAVXChange = new NAVXChange();

            var invoiceNAVXML = bllInvoice.GetInvoiceNAVXML(invoice);
            resNAVXChange.InvoiceID = invoice.ID;
            resNAVXChange.InvoiceNumber = invoice.InvoiceNumber;
            resNAVXChange.InvoiceXml = XMLUtil.Object2XMLString<InvoiceData>(invoiceNAVXML, Encoding.UTF8, NAVGlobal.XMLNamespaces);
            resNAVXChange.Operation = enNAVOperation.MANAGEINVOICE.ToString();
            return ManageInvoiceByXChange(resNAVXChange);
        }

        public NAVXChange CallManageAnnulmentFull(Invoice invoice)
        {
            var resNAVXChange = new NAVXChange();

            var annulmentData = new InvoiceAnnulment(invoice.InvoiceNumber);
            var annulmentDataStr = XMLUtil.Object2XMLString<InvoiceAnnulment>(annulmentData, Encoding.UTF8, NAVGlobal.XMLNamespaces);

            resNAVXChange.InvoiceID = invoice.ID;
            resNAVXChange.InvoiceNumber = invoice.InvoiceNumber;
            resNAVXChange.InvoiceXml = annulmentDataStr;
            resNAVXChange.Operation = enNAVOperation.MANAGEANNULMENT.ToString();
            return ManageAnnulmentByXChange(resNAVXChange);
        }

        public NAVXChange ManageInvoiceByXChange(NAVXChange NAVXChange)
        {
            var ter = new TokenExchangeRequest(_NAVSettings.Taxnum, _NAVSettings.TechUser, _NAVSettings.TechUserPwd, _NAVSettings.SignKey);

            var reqTer = XMLUtil.Object2XMLString<TokenExchangeRequest>(ter, Encoding.UTF8, NAVGlobal.XMLNamespaces);

            NAVXChange.TokenTime = DateTime.UtcNow;
            NAVXChange.TokenRequest = reqTer;
            NAVXChange.Status = enNAVStatus.CREATED.ToString();

            string resp = "";

            if (NAVPost(_NAVSettings.TokenExchange, ter.header.requestId, reqTer, MethodBase.GetCurrentMethod().Name, out resp))
            {
                TokenExchangeResponse tokenResponse = XMLUtil.XMLStringToObject<TokenExchangeResponse>(resp);

                var token = tokenResponse.encodedExchangeToken;
                NAVXChange.TokenResponse = resp;
                NAVXChange.Token = Convert.ToBase64String(token);
                NAVXChange.TokenFuncCode = tokenResponse.result.funcCode.ToString();
                NAVXChange.TokenMessage = (tokenResponse.result.errorCode + " " + tokenResponse.result.message).Trim();
                NAVXChange.Status = enNAVStatus.TOKEN.ToString();

                var mir = new ManageInvoiceRequest(_NAVSettings.Taxnum, _NAVSettings.TechUser, _NAVSettings.TechUserPwd, _NAVSettings.SignKey, _NAVSettings.ExchangeKey,
                    token, new string[] { NAVXChange.InvoiceXml });
                var reqManageInvoice = XMLUtil.Object2XMLString<ManageInvoiceRequest>(mir, Encoding.UTF8, NAVGlobal.XMLNamespaces);
                
                NAVXChange.SendTime = DateTime.UtcNow;
                NAVXChange.SendRequest = reqManageInvoice;

                resp = "";

                if (NAVPost(_NAVSettings.ManageInvoice, mir.header.requestId, reqManageInvoice, MethodBase.GetCurrentMethod().Name, out resp))
                {
                    ManageInvoiceResponse miresp = XMLUtil.XMLStringToObject<ManageInvoiceResponse>(resp);

                    NAVXChange.Status = enNAVStatus.DATA_SENT.ToString();
                    NAVXChange.SendResponse = resp;
                    NAVXChange.SendFuncCode = miresp.result.funcCode.ToString();
                    NAVXChange.SendMessage = (miresp.result.errorCode + " " + miresp.result.message).Trim();
                    NAVXChange.TransactionID = miresp.transactionId;
                }
                else
                {
                    var msg = String.Format(bbxBEConsts.NAV_MANAGEINVOICE_ERR, MethodBase.GetCurrentMethod().Name, resp);
                    _logger.Error(msg);
                    ProcessGeneralErrorResponse(NAVXChange, resp);
                }
            }
            else
            {
                var msg = String.Format(bbxBEConsts.NAV_TOKENEXCHANGE_ERR, MethodBase.GetCurrentMethod().Name, resp);
                _logger.Error(msg);
            }
            return NAVXChange;
        }

        public NAVXChange ManageAnnulmentByXChange(NAVXChange NAVXChange)
        {
            var ter = new TokenExchangeRequest(_NAVSettings.Taxnum, _NAVSettings.TechUser, _NAVSettings.TechUserPwd, _NAVSettings.SignKey);

            var reqTer = XMLUtil.Object2XMLString<TokenExchangeRequest>(ter, Encoding.UTF8, NAVGlobal.XMLNamespaces);

            NAVXChange.TokenTime = DateTime.UtcNow;
            NAVXChange.TokenRequest = reqTer;
            NAVXChange.Status = enNAVStatus.CREATED.ToString();

            string resp = "";

            if (NAVPost(_NAVSettings.TokenExchange, ter.header.requestId, reqTer, MethodBase.GetCurrentMethod().Name, out resp))
            {
                TokenExchangeResponse tokenResponse = XMLUtil.XMLStringToObject<TokenExchangeResponse>(resp);

                var token = tokenResponse.encodedExchangeToken;
                NAVXChange.TokenResponse = resp;
                NAVXChange.Token = Convert.ToBase64String(token);
                NAVXChange.TokenFuncCode = tokenResponse.result.funcCode.ToString();
                NAVXChange.TokenMessage = (tokenResponse.result.errorCode + " " + tokenResponse.result.message).Trim();
                NAVXChange.Status = enNAVStatus.TOKEN.ToString();

                var mar = new ManageAnnulmentRequest(_NAVSettings.Taxnum, _NAVSettings.TechUser, _NAVSettings.TechUserPwd, _NAVSettings.SignKey, _NAVSettings.ExchangeKey,
                    token, new string[] { NAVXChange.InvoiceXml });
                var reqManageAnnulment = XMLUtil.Object2XMLString<ManageAnnulmentRequest>(mar, Encoding.UTF8, NAVGlobal.XMLNamespaces);

                NAVXChange.SendTime = DateTime.UtcNow;
                NAVXChange.SendRequest = reqManageAnnulment;

                resp = "";

                if (NAVPost(_NAVSettings.ManageAnnulment, mar.header.requestId, reqManageAnnulment, MethodBase.GetCurrentMethod().Name, out resp))
                {
                    ManageAnnulmentResponse maresp = XMLUtil.XMLStringToObject<ManageAnnulmentResponse>(resp);

                    NAVXChange.Status = enNAVStatus.DATA_SENT.ToString();
                    NAVXChange.SendResponse = resp;
                    NAVXChange.SendFuncCode = maresp.result.funcCode.ToString();
                    NAVXChange.SendMessage = (maresp.result.errorCode + " " + maresp.result.message).Trim();
                    NAVXChange.TransactionID = maresp.transactionId;
                }
                else
                {
                    var msg = String.Format(bbxBEConsts.NAV_MANAGEINVOICE_ERR, MethodBase.GetCurrentMethod().Name, resp);
                    _logger.Error(msg);
                    ProcessGeneralErrorResponse(NAVXChange, resp);
                }
            }
            else
            {
                var msg = String.Format(bbxBEConsts.NAV_TOKENEXCHANGE_ERR, MethodBase.GetCurrentMethod().Name, resp);
                _logger.Error(msg);
            }
            return NAVXChange;
        }

        public NAVXChange QueryTransactionStatusByXChange(NAVXChange NAVXChange)
        {
            var ter = new TokenExchangeRequest(_NAVSettings.Taxnum, _NAVSettings.TechUser, _NAVSettings.TechUserPwd, _NAVSettings.SignKey);

            var reqTer = XMLUtil.Object2XMLString<TokenExchangeRequest>(ter, Encoding.UTF8, NAVGlobal.XMLNamespaces);
            string resp = "";
            if (NAVPost(_NAVSettings.TokenExchange, ter.header.requestId, reqTer, MethodBase.GetCurrentMethod().Name, out resp))
            {
                TokenExchangeResponse tokenResponse = XMLUtil.XMLStringToObject<TokenExchangeResponse>(resp);
                var qtr = new QueryTransactionStatusRequest(_NAVSettings.Taxnum, _NAVSettings.TechUser, _NAVSettings.TechUserPwd, _NAVSettings.SignKey, NAVXChange.TransactionID);
                var reqQtr = XMLUtil.Object2XMLString<QueryTransactionStatusRequest>(qtr, Encoding.UTF8, NAVGlobal.XMLNamespaces);

                NAVXChange.QueryTime = DateTime.UtcNow;
                NAVXChange.QueryRequest = reqQtr;

                resp = "";

                if (NAVPost(_NAVSettings.QueryTransactionStatus, qtr.header.requestId, reqQtr, MethodBase.GetCurrentMethod().Name, out resp))
                {
                    NAVXChange.QueryResponse = resp;
                    QueryTransactionStatusResponse qresp = XMLUtil.XMLStringToObject<QueryTransactionStatusResponse>(resp);
                    NAVXChange.QueryFuncCode = qresp.result.funcCode.ToString();
                    NAVXChange.QueryMessage = (qresp.result.errorCode + " " + qresp.result.message).Trim();

                    if (qresp.processingResults != null && qresp.processingResults.processingResult != null)
                    {

                        //FONTOS : Egyelőre egy tranzakcióban csak egy számla van,m ezért a qresp.processingResults.processingResult 
                        //         csak egy elemű lehet

                        //foreach (var prr in qresp.processingResults.processingResult)
                        var prr = qresp.processingResults.processingResult.First();

                        {

                            // InvoiceStatusType -> NAV_enums.enTransactionStatus átfordítás
                            var status = (NAV_enums.enTransactionStatus)Enum.Parse(typeof(NAV_enums.enTransactionStatus), prr.invoiceStatus.ToString());

                            //A DONE és ERROR végstátusz, a folyamaban levőséget jelző RECEIVED és PROCESSING státusz nem blokkol
                            if (prr.invoiceStatus == InvoiceStatusType.DONE ||
                                prr.invoiceStatus == InvoiceStatusType.ABORTED)

                            {
                                NAVXChange.Status = prr.invoiceStatus.ToString();
                                ProcessTransactionResult(NAVXChange, qresp);
                                if (prr.invoiceStatus == InvoiceStatusType.ABORTED && !string.IsNullOrWhiteSpace(_NAVSettings.NotificationEmailTo))
                                {
                                    SendNAVErrorMessageMailAsync(NAVXChange).GetAwaiter().GetResult();
                                }
                            }
                        }
                    }
                    else
                    {
                        //valami probléma törént, nincs státuszállítás, hogy újra megpróbálhassuk
                        var msg = String.Format(bbxBEConsts.NAV_QUERYTRANSACTION_ERR, MethodBase.GetCurrentMethod().Name, resp);
                        _logger.Error(msg);
                        ProcessGeneralErrorResponse(NAVXChange, resp);
                    }
                }
                else
                {
                    //valami probléma törént, nincs státuszállítás, hogy újra megpróbálhassuk
                    var msg = String.Format(bbxBEConsts.NAV_QUERYTRANSACTION_ERR2, MethodBase.GetCurrentMethod().Name, resp);
                    _logger.Error(msg);
                    ProcessGeneralErrorResponse(NAVXChange, resp);
                }
            }
            else
            {
                var msg = String.Format(bbxBEConsts.NAV_TOKENEXCHANGE_ERR, MethodBase.GetCurrentMethod().Name, resp);
                _logger.Error(msg);
            }
            return NAVXChange;
        }

        public async Task SendNAVErrorMessageMailAsync(NAVXChange NAVXChange)
        {
            try
            {
                // convert string to stream

                var mailBodyHtml = CreateNAVNotificationMailBodyHtml(NAVXChange);

                var parser = new HtmlParser();
                var document = parser.ParseDocument(mailBodyHtml);
                var mailBodyText = document.Body.TextContent;

                string[] addr = _NAVSettings.NotificationEmailTo.Replace(",", ";").Split(';');

                foreach (var toEmail in addr)
                {
                    /**********/

                    var att = new SendGrid.Helpers.Mail.Attachment()
                    {
                        Filename = NAVXChange.InvoiceNumber + ".xml",
                        Content = Utils.ConvertToBase64String(NAVXChange.InvoiceXml),
                        Type = "application/xml",
                        ContentId = "ContentId"
                    };
                    var emailCommand = new sendEmailCommand()
                    {
                        From = new _EmailAddress() { Name = _NAVSettings.NotificationEmailFrom, Email = _NAVSettings.NotificationEmailFrom },
                        To = new _EmailAddress() { Name = toEmail, Email = toEmail },
                        Body_plain_text = mailBodyText,
                        Body_html_text = mailBodyHtml,
                        Subject = string.Format(_NAVSettings.NotificationEmailSubject, NAVXChange.InvoiceNumber),
                        Attachments = new System.Collections.Generic.List<SendGrid.Helpers.Mail.Attachment>() { att }
                    };

                    var emailResult = await bllSendgrid.SendEmailAsync(emailCommand, default(CancellationToken));
                    if (emailResult.Succeeded)
                    {
                        var msg = String.Format(bbxBEConsts.NAV_NOTIFICATIONEMAIL_SENT, toEmail, NAVXChange.InvoiceNumber);
                        _logger.Information(msg);
                    }
                    else
                    {
                        var msg = String.Format(bbxBEConsts.NAV_NOTIFICATIONEMAIL_SENT_ERR, toEmail, NAVXChange.InvoiceNumber, JsonConvert.SerializeObject(emailResult));
                        _logger.Information(msg);
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public static string CreateNAVNotificationMailBodyHtml(NAVXChange NAVXChange)
        {
            var line = "<tr>" + Environment.NewLine +
                        "<td>%%ResultCode</td>" + Environment.NewLine +
                        "<td>%%ErrorCode</td>" + Environment.NewLine +
                        "<td>%%Message</td>" + Environment.NewLine +
                        "<td>%%Line</td> " + Environment.NewLine +
                        "<td>%%Tag</td>" + Environment.NewLine +
                        "<td>%%Value</td>" + Environment.NewLine +
                        "</tr>" + Environment.NewLine;
            var mailBody = "<!DOCTYPE html>" + Environment.NewLine +
                        "<html>" + Environment.NewLine +
                        "<head>" + Environment.NewLine +
                        "<style>" + Environment.NewLine +
                        "	table {" + Environment.NewLine +
                        "	  width:100%;" + Environment.NewLine +
                        "	}" + Environment.NewLine +
                        "	table, th, td {" + Environment.NewLine +
                        "	  border: 1px solid black;" + Environment.NewLine +
                        "	  border-collapse: collapse;" + Environment.NewLine +
                        "	}" + Environment.NewLine +
                        "	th, td {" + Environment.NewLine +
                        "	  padding: 15px;" + Environment.NewLine +
                        "	  text-align: left;" + Environment.NewLine +
                        "	}" + Environment.NewLine +
                        "	#t01 tr:nth-child(even) {" + Environment.NewLine +
                        "	  background-color: #eee;" + Environment.NewLine +
                        "	}" + Environment.NewLine +
                        "	#t01 tr:nth-child(odd) {" + Environment.NewLine +
                        "	 background-color: azure;" + Environment.NewLine +
                        "	}" + Environment.NewLine +
                        "	#t01 th {" + Environment.NewLine +
                        "	  background-color: cyan;" + Environment.NewLine +
                        "	  color: black;" + Environment.NewLine +
                        "	}" + Environment.NewLine +
                        "</style>" + Environment.NewLine +
                        "</head>" + Environment.NewLine +
                        "<body>" + Environment.NewLine +
                        "<p>Sz&aacute;mla:<strong>%%INVOICE</strong></p>" + Environment.NewLine +
                        "<p>Transaction ID:<strong>%%TRANSACTIONID</strong></p>" + Environment.NewLine +
                        "<table id=\"t01\">" + Environment.NewLine +
                        "<tr>" + Environment.NewLine +
                        "<th>ResultCode</th>" + Environment.NewLine +
                        "<th>ErrorCode</th>" + Environment.NewLine +
                        "<th>Message</th>" + Environment.NewLine +
                        "<th>Line</th>" + Environment.NewLine +
                        "<th>Tag</th>" + Environment.NewLine +
                        "<th>Value</th>" + Environment.NewLine +
                        "</tr>" + Environment.NewLine +
                        "%%LINES" + Environment.NewLine +
                        "</table>" + Environment.NewLine +
                        "</body>" + Environment.NewLine +
            "<html>";

            var lines = "";
            foreach (var xr in NAVXChange.NAVXResults)
            {
                /*
                                lines += line.Replace("%%ResultCode", xr.ResultCode)
                                             .Replace("%%ErrorCode", Util.ConvertEncoding(xr.ErrorCode, Encoding.UTF8, Encoding.GetEncoding("ISO-8859-1")))
                                             .Replace("%%Message", Util.ConvertEncoding(xr.Message, Encoding.UTF8, Encoding.GetEncoding("ISO-8859-1")))
                                             .Replace("%%Line", xr.Line)
                                             .Replace("%%Tag", xr.Tag)
                                             .Replace("%%Value", Util.ConvertEncoding(xr.Value, Encoding.UTF8, Encoding.GetEncoding("ISO-8859-1"))) + "árvíztűrő tükörfúrógép";
                */
                lines += line.Replace("%%ResultCode", xr.ResultCode)
                             .Replace("%%ErrorCode", xr.ErrorCode)
                             .Replace("%%Message", xr.Message)
                             .Replace("%%Line", xr.Line)
                             .Replace("%%Tag", xr.Tag)
                             .Replace("%%Value", xr.Value);
            }

            var body = mailBody.Replace("%%INVOICE", NAVXChange.InvoiceNumber)
                                .Replace("%%TRANSACTIONID", NAVXChange.TransactionID)
                               .Replace("%%LINES", lines);

            return body;
        }
        /******************/
    }
}
