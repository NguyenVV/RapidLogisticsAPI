﻿using CrawlDataService.BusinessLayer;
using CrowlData.BusinessLayer;
using CrowlData.ValueObject;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Net;
using System.ServiceProcess;
using System.Text;
using System.Timers;

namespace CrawlDataService
{
    public partial class CrawlDataService : ServiceBase
    {
        System.Timers.Timer timerCheck = new System.Timers.Timer();//create timer
        string zipcode = string.Empty;
        string baseAddress = string.Empty;
        string procName = string.Empty;
        string apiFunctionPath = string.Empty;
        double timeBetweenRuns = 10000;
        int limit = 1;
        CpnBUS cpn = new CpnBUS();
        WebAPIUtils apiUtils = new WebAPIUtils();

        public CrawlDataService()
        {
            InitializeComponent();
            try
            {
                zipcode = ConfigurationManager.AppSettings["zipcode"];
                baseAddress = ConfigurationManager.AppSettings["baseAPIAddress"];
                procName = ConfigurationManager.AppSettings["procSelectOneRowName"];
                timeBetweenRuns = double.Parse(ConfigurationManager.AppSettings["timeBetweenRuns"]);
                apiFunctionPath = ConfigurationManager.AppSettings["apiFunctionPath"];
                limit = int.Parse(ConfigurationManager.AppSettings["limit"]);
                apiUtils.InitWebClient(zipcode, baseAddress, procName, timeBetweenRuns);
            }
            catch (Exception ex) { apiUtils.WriteLog(ex); }
        }
        protected override void OnStart(string[] args)
        {
            System.Diagnostics.Debugger.Launch();
            timerCheck.Elapsed += new ElapsedEventHandler(timerCheck_Elapsed);
            timerCheck.Interval = timeBetweenRuns;//set timer interval (1000 = 1s)
            timerCheck.Enabled = true;
            timerCheck.Start();//start timer
        }

        protected override void OnStop()
        {
        }

        public void onDebug()
        {
            OnStart(null);
        }

        private void timerCheck_Elapsed(object sender, EventArgs e)
        {
            if (apiUtils.CheckForInternetConnection())
            {
                PostDataToAPI();
            }
        }

        private void PostDataToAPI()
        {
            try
            {
                PostData dataPost = new PostData();
                dataPost.Status = "overover";
                
                
                DataTable dataToPost = cpn.GetAllDataNewWithStatusZero();
                if (dataToPost != null && dataToPost.Rows.Count > 0)
                {
                    //JObject json = JObject.Parse(apiUtils.GetJson(dataToPost));
                    dataPost.ShipmentNo = apiUtils.GetJsonFromDataTable(dataToPost);
                    HttpWebResponse response = (HttpWebResponse)apiUtils.CallToPostWebAPI(dataPost, apiFunctionPath);

                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        // Get the request stream.  
                        Stream dataStream = response.GetResponseStream();
                        // Open the stream using a StreamReader for easy access.  
                        StreamReader reader = new StreamReader(response.GetResponseStream());
                        // Read the content.  
                        string responseFromServer = reader.ReadToEnd();
                        // Display the content.  
                        Console.WriteLine(responseFromServer);
                        
                        string res = string.Empty;
                        
                            // ... Read the string.
                            //Task<string> result = content.ReadAsStringAsync();
                            //res = result.Result;
                            JObject json = JObject.Parse(responseFromServer);

                            if (json["code"].ToString() == "success")
                            {
                                apiUtils.WriteLog("\n******Post data success: "+DateTime.Now);
                            //string idList = json["data"]["idE2"].ToString();
                            //List<KeyValuePair<string, object>> paramList = new List<KeyValuePair<string, object>>();
                            //KeyValuePair<string, object> idListToUpdate = new KeyValuePair<string, object>("@idE2", idList);
                            //KeyValuePair<string, object> isUpdate = new KeyValuePair<string, object>("@update", 1);
                            //if (idList.Contains(',')){
                            //    string[] array = idList.Split(',');
                            //    if (array.Length > 1)
                            //    {
                            //        string newIdList = "";
                            //        foreach(string id in array)
                            //        {
                            //            if (newIdList == "")
                            //                newIdList += "'" + id + "'";
                            //            else
                            //                newIdList += ",'" + id + "'";
                            //        }
                            //        idListToUpdate = new KeyValuePair<string, object>("@idE2", newIdList);
                            //    }
                            //}
                            //paramList.Add(idListToUpdate);
                            //paramList.Add(isUpdate);
                            StringBuilder ids = new StringBuilder();
                            StringBuilder shipmentIdList = new StringBuilder();
                            int totalRow = dataToPost.Rows.Count;
                            for (int i=0;i< totalRow; i++)
                            {
                                if (ids.Length == 0)
                                {
                                    ids.Append(dataToPost.Rows[i]["Id"]);
                                    shipmentIdList.Append(dataToPost.Rows[i]["ShipmentID"]);
                                }
                                else
                                {
                                    ids.Append(",");
                                    ids.Append(dataToPost.Rows[i]["Id"]);
                                    shipmentIdList.Append(",");
                                    shipmentIdList.Append( dataToPost.Rows[i]["ShipmentID"]);
                                }
                            }
                                int updateData = cpn.UpdateDataAfterSuccess(ids.ToString());
                                if (updateData > 0)
                                    apiUtils.WriteLog("\n******Success update "+ totalRow +"("+ updateData + ") rows to database. List id = " + ids+ "\nList ShipmentId:" + shipmentIdList.ToString());
                                else
                                    apiUtils.WriteLog("\n******Fail update " + totalRow + " rows to database list id = " + ids + "\nList ShipmentId:" + shipmentIdList.ToString());
                            }
                            else
                            {
                                apiUtils.WriteLog("\n******Post data success but get error: " + json.Values("message").ToString());
                            }
                        // Clean up the streams.  
                        reader.Close();
                        dataStream.Close();
                        response.Close();
                    }
                    else
                    {
                        apiUtils.WriteLog("\n******Post data fail : " + "Error Code");
                        //response.StatusCode + " : Message - " + response.ReasonPhrase);
                    }
                }
            }
            catch (Exception ex)
            {
                apiUtils.WriteLog(ex);
            }
        }
    }
}