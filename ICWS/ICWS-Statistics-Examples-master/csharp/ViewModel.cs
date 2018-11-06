using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Media;
using WpfStatistics.Model;

namespace WpfStatistics
{
    class ViewModel : INotifyPropertyChanged
    {
        private const string AppName = "Statistics";
        private string _sessionId = null;
        private string _csrfToken = null;
        private string _setCookie = null; 
        private string _userId = "devlab_user";
        private string _password = "1234";
        //private string _server = "http://172.19.34.165:8018/icws/";
        private string _server = "http://morbo:8018/icws/";
        private Timer _pollTimer = null;
        private SynchronizationContext _syncContext;
        Dictionary<string, JObject> _alertDefinitions = new Dictionary<string, JObject>();
        Dictionary<string, JObject> _workgroupAlerts = new Dictionary<string, JObject>();
        string _lastAlertWorkgroup = null;
        private readonly List<StatisticViewModel> _statistics  ;

        public event PropertyChangedEventHandler PropertyChanged;

        private Parameter _selectedWorkgroup;
        public Parameter SelectedWorkgroup
        {
            get
            {
                return _selectedWorkgroup;
            }
            set
            {
                _selectedWorkgroup = value;
                RaisePropertyChanged("SelectedWorkgroup");
                StartWorkgroupStatWatches();
                AlertWorkgroupChanged(value.value);
            }
        }

        public ObservableCollection<Parameter> Workgroups
        {
            get;
            set;
        }

        private Parameter _selectedInterval;
        public Parameter SelectedInterval
        {
            get
            {
                return _selectedInterval;
            }
            set
            {
                _selectedInterval = value;
                RaisePropertyChanged("SelectedInterval");
                StartWorkgroupStatWatches();
                if (SelectedWorkgroup != null)
                {
                    AlertWorkgroupChanged(SelectedWorkgroup.value);
                }
                
            }
        }
        public ObservableCollection<Parameter> Intervals
        {
            get;
            set;
        }

        private void RaisePropertyChanged(string name)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(name));
            }
        }

        public ViewModel()
        {
            _syncContext = SynchronizationContext.Current;
            Workgroups = new ObservableCollection<Parameter>();
            Intervals = new ObservableCollection<Parameter>();
            ThreadPool.QueueUserWorkItem(new WaitCallback(o=> Login() ));

            _agentsLoggedInAndActivated = new StatisticViewModel("inin.workgroup:AgentsLoggedInAndActivated");
            _longestAvailable = new StatisticViewModel("inin.workgroup:LongestAvailable");
            _notAvailable = new StatisticViewModel("inin.workgroup:NotAvailable");
            _numberAvailableForACDInteractions = new StatisticViewModel("inin.workgroup:NumberAvailableForACDInteractions");
            _interactionsConnected = new StatisticViewModel("inin.workgroup:InteractionsConnected");
            _percentAvailable = new StatisticViewModel("inin.workgroup:PercentAvailable");

            _averageHoldTime = new StatisticViewModel("inin.workgroup:AverageHoldTime");
            _averageTalkTime = new StatisticViewModel("inin.workgroup:AverageTalkTime");
            _averageWaitTime = new StatisticViewModel("inin.workgroup:AverageWaitTime");
            _interactionsAbandoned = new StatisticViewModel("inin.workgroup:InteractionsAbandoned");
            _interactionsAnswered = new StatisticViewModel("inin.workgroup:InteractionsAnswered");
            _interactionsCompleted = new StatisticViewModel("inin.workgroup:InteractionsCompleted");

            _statistics = new List<StatisticViewModel>(){
                _longestAvailable,_agentsLoggedInAndActivated,_notAvailable,_numberAvailableForACDInteractions, _interactionsConnected , _percentAvailable, _averageHoldTime, _averageTalkTime , _averageWaitTime, 
                _interactionsAbandoned, _interactionsAnswered , _interactionsCompleted
            };
        }

        private void SendRequest(string verb, string url, object data, Action<string> callback, Action<string> errorCallback)
        {
            HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(_server + url);
            httpWebRequest.Method = verb;
            httpWebRequest.Headers.Add("Accept-Language", Boolean.TrueString);

            if (data != null)
            {
                var jsSerializer = new JavaScriptSerializer();

                Stream requestStream = httpWebRequest.GetRequestStream();
                using(StreamWriter writer = new StreamWriter(requestStream))
                {
                    writer.Write(jsSerializer.Serialize(data));
                }
            }

            if (!String.IsNullOrEmpty(_csrfToken))
            {
                httpWebRequest.Headers.Add("ININ-ICWS-CSRF-Token", _csrfToken);
            }

            if (_setCookie != null)
            {
                httpWebRequest.Headers.Add("Cookie", _setCookie);
            }

            try
            {
                HttpWebResponse webResponse = httpWebRequest.GetResponse() as HttpWebResponse;
                string responseString = String.Empty;

                using (Stream respStream = webResponse.GetResponseStream())
                {
                    StreamReader reader = new StreamReader(respStream, Encoding.UTF8);
                    responseString = reader.ReadToEnd();
                }

                if (_setCookie == null)
                {
                    _setCookie = webResponse.Headers["Set-Cookie"];
                    
                }

                if ((webResponse.StatusCode == HttpStatusCode.OK || webResponse.StatusCode == HttpStatusCode.Created) && callback != null)
                {
                    callback(responseString);    
                }
                else if(errorCallback != null)
                {
                    errorCallback(responseString);
                }
            }
            catch(WebException webEx)
            {
                var errorString = String.Empty;

                using (Stream respStream = webEx.Response.GetResponseStream())
                {
                    StreamReader reader = new StreamReader(respStream, Encoding.UTF8);
                    errorString = reader.ReadToEnd();
                    Console.WriteLine(errorString);
                }

                if (errorCallback != null)
                {
                    errorCallback(errorString);
                }
            }
        }
        
        private void Login()
        {
            var loginData = new LoginData() { applicationName = AppName, password = _password, userID = _userId };
            SendRequest("POST", "connection", loginData, AfterLogin, null);
        }

        private void AfterLogin(string data)
        {
            var loginData = JsonConvert.DeserializeObject<LoginResult>(data);
            _csrfToken = loginData.csrfToken;
            _sessionId = loginData.sessionId;
            StartIcws();
        }

        private void StartIcws()
        {
            _pollTimer = new Timer(new TimerCallback(o => {
                SendRequest("GET", _sessionId + "/messaging/messages", null, MessagePollProcesser, null);
            }), null, 1000, 1000);
            
            SendRequest("POST", _sessionId + "/statistics/statistic-parameter-values/queries", new ParameterRequest{ parameterTypeId = "ININ.People.WorkgroupStats:Workgroup"}, WorkgroupParameterCallback, null);
	
	        //get the queue intervals
	        SendRequest("POST", _sessionId + "/statistics/statistic-parameter-values/queries", new ParameterRequest{ parameterTypeId ="ININ.Queue:Interval"}, IntervalCallback, null);

            //get the alert catalog
	        SendRequest("PUT", _sessionId + "/messaging/subscriptions/alerts/alert-catalog", new AlertCatalogRequest {alertSetCategories = new [] {2,3,4,5}}, null, null);

            //SendRequest("PUT", _sessionId + "/messaging/subscriptions/alerts/alert-notifications", null, null, null);
        }

        private void IntervalCallback(string obj)
        {
            var data = JsonConvert.DeserializeObject<ParameterResponse>(obj);

            _syncContext.Post(o =>
            {
                Intervals.Clear();
                foreach (var parameter in data.parameterValues)
                {
                    Intervals.Add(parameter);
                }
            }, null);
        }

        private void WorkgroupParameterCallback(string obj)
        {
            var data = JsonConvert.DeserializeObject<ParameterResponse>(obj);

            _syncContext.Post(o =>
            {
                Workgroups.Clear();
                foreach (var parameter in data.parameterValues)
                {
                    Workgroups.Add(parameter);
                }
            }, null);
        }

        private void MessagePollProcesser(string obj)
        {
            //you could create a representation for every message type that can be returned, but it might be easier to use a dynamic
            dynamic data = JsonConvert.DeserializeObject(obj);
          
            if (data != null)
            {
                //handle catalog changes first
                foreach (var message in data)
                {
                    if (message.__type == "urn:inin.com:alerts:alertCatalogChangedMessage")
                    {
                        HandleAlertCatalogChange(message);
                    }
                }

                foreach (var message in data)
                {
                    if (message.__type == "urn:inin.com:statistics:statisticValueMessage")
                    {
                        HandleStatUpdate(message.statisticValueChanges);
                    }
                    else if (message.__type == "urn:inin.com:alerts:alertNotificationMessage")
                    {
                        ProcessAlert(message);
                    }
                }
            }
        }

        string[] workgroupStatIds = new [] {"inin.workgroup:AgentsLoggedInAndActivated",
								"inin.workgroup:LongestAvailable",
								"inin.workgroup:NotAvailable",
								"inin.workgroup:NumberAvailableForACDInteractions",
								"inin.workgroup:InteractionsConnected",
								"inin.workgroup:PercentAvailable"};
						
        string[]  interactionStatIds = new [] {"inin.workgroup:AverageHoldTime",
						        "inin.workgroup:AverageTalkTime",
						        "inin.workgroup:AverageWaitTime",
						        "inin.workgroup:InteractionsAbandoned",
						        "inin.workgroup:InteractionsAnswered",
						        "inin.workgroup:InteractionsCompleted",
        };

        private void StartWorkgroupStatWatches()
        {
            if(SelectedInterval == null || SelectedWorkgroup == null)
            {
                return;
            }

            StopStatWatches();

            var workgroup = SelectedWorkgroup.value;
            var interval = SelectedInterval.value;


	        var statData = new List<StatisticIdentifierRequest>();
	
	        foreach (var statid in workgroupStatIds)
            {
		        var statKey = new StatisticIdentifierRequest{
                    statisticIdentifier = statid,
                    parameterValueItems = new [] { new StatisticParameter{
                                                                parameterTypeId = "ININ.People.WorkgroupStats:Workgroup",
                                                                value = workgroup
                                                        }
                    }
                };
               
		        statData.Add(statKey);
	        }
	
	        foreach (var statid in interactionStatIds)
            {
		          var statKey = new StatisticIdentifierRequest{
                    statisticIdentifier = statid,
                    parameterValueItems = new [] { new StatisticParameter{
                                                                parameterTypeId = "ININ.People.WorkgroupStats:Workgroup",
                                                                value = workgroup
                                                        },
                                                    new StatisticParameter{
                                                            parameterTypeId = "ININ.Queue:Interval",
                                                            value = interval
                                                    }
                    }
                };
               
		        statData.Add(statKey);
	        }

            SendRequest("PUT", _sessionId + "/messaging/subscriptions/statistics/statistic-values", new StatisticKeysRequest { statisticKeys = statData }, null,  null);
	
        }

        private void StopStatWatches(){
	        SendRequest("DELETE", _sessionId + "/messaging/subscriptions/statistics/statistic-values",null,null, null);
        }

        private void HandleStatUpdate(dynamic statChangeList)
        {
            foreach (var stat in statChangeList)
            {
                var value = "N/A";

                if (stat.statisticValue != null)
                {
                    value = stat.statisticValue.value;
                }

                var statVm = _statistics.FirstOrDefault(s => s.Key == stat.statisticKey.statisticIdentifier.ToString());
                if(statVm != null)
                {
                    statVm.Value = value;
                }
            }
        }

        private void AlertWorkgroupChanged(string workgroup){
	        if(workgroup != _lastAlertWorkgroup){
		        CleanupAlerts();

		        foreach(var alert in _workgroupAlerts.Keys.ToArray()){
			        var alertObject = _workgroupAlerts[alert];
			        ExecuteAlert(alertObject);
		        }
		        _lastAlertWorkgroup = workgroup;
	        }	
        }

        private void CleanupAlerts(){
            _statistics.ForEach(s => s.Reset());
        }

        private void ProcessAlert(dynamic message){

	        foreach (var alert in  message.alertNotificationList){
		        ExecuteAlert(alert);
	        }
        }

        private void ExecuteAlert(dynamic alert)
        {

            if(!_alertDefinitions.ContainsKey(alert.alertDefinitionId.ToString()))
            {
                return;
            }
            
	        var alertDefinition = _alertDefinitions[alert.alertDefinitionId.ToString()]; 
	        if(alertDefinition != null ){
		        if(IsAlertForCurrentWorkgroup(alertDefinition)){
			        if(alert.cleared == false){ 
				        ProcessFontAlertAction(alertDefinition, alert.alertRuleId);
			        }
			        else if(alertDefinition != null && alert.cleared == true){
				        var statId = alertDefinition.statisticKey.statisticIdentifier;
                        var statVm = _statistics.FirstOrDefault(s => s.Key == statId.ToString());

                        if (statVm != null && statVm.AlertId == alert.alertRuleId.ToString())
                        {
                            statVm.Reset();
                        }
			        }
		        }
		
		        SaveAlertWorkgroup(alert);
	        }
        }

        //used to persist the alert so that when we change workgroups we will have those alerts.  
        private void SaveAlertWorkgroup(dynamic alert)
        {
	        if(alert.cleared == true && _workgroupAlerts.ContainsKey(alert.alertRuleId.ToString())){
		        //remove from map
		        _workgroupAlerts.Remove(alert.alertRuleId);
	        }
	        else if(alert.cleared == false){
		        _workgroupAlerts[alert.alertRuleId.ToString()] = alert;
	        }
	
        }

        //take a font action from the message and apply it to the font label
        private void ProcessFontAlertAction(dynamic alertDefinition, dynamic ruleId){
	        var statId = alertDefinition.statisticKey.statisticIdentifier;
	        foreach(var rule in  alertDefinition.alertRules)
            {
		        if(rule.alertRuleId != ruleId){
			        continue;
		        }
		
		        foreach(var action in rule.alertActions)
                {
			        if(action.targetId.ToString() == "ININ.Supervisor.FontAlertAction"){
                        var background = action.namedValues["ININ.Supervisor.FontAlertAction.BackgroundColor"].ToString().Split(':');
                        var foreground = action.namedValues["ININ.Supervisor.FontAlertAction.TextColor"].ToString().Split(':');
				      
                        var statVm = _statistics.FirstOrDefault(s => s.Key == statId.ToString());

                        if (statVm != null)
                        {
                            _syncContext.Post((o) =>
                            {
                                var backgroundColor = new SolidColorBrush(Color.FromRgb(Byte.Parse(background[1]), Byte.Parse(background[2]), Byte.Parse(background[3])));
                                var foregroundColor = new SolidColorBrush(Color.FromRgb(Byte.Parse(foreground[1]), Byte.Parse(foreground[2]), Byte.Parse(foreground[3])));

                                statVm.BackgroundColor = backgroundColor;
                                statVm.ForegroundColor = foregroundColor;
                            }
                            , null);
                        }
			        }
		        }
	
	        }
        }


        //we only want to process an alert if it is for the currently selected workgroup
        private bool IsAlertForCurrentWorkgroup(dynamic alert){
            if (SelectedWorkgroup == null)
                return false;

	        var currentWorkgroup = SelectedWorkgroup.value;
	        var paramValues = alert.statisticKey.parameterValueItems;
	        foreach (var param in paramValues)
            {
		        if(param.parameterTypeId == "ININ.People.WorkgroupStats:Workgroup" && param.value != currentWorkgroup){
			        return false;
		        }
	        }
	
	        return true;
        }

        private void HandleAlertCatalogChange(dynamic message)
        {
	        var alertSets = new List<dynamic>();
            alertSets.AddRange(message.alertSetsAdded);
            alertSets.AddRange(message.alertSetsChanged);
            
	        foreach(var alertSet in alertSets)
	        {
                foreach (var definition in alertSet.alertDefinitions)
                {
			        if(HasVisualAlert(definition))
			        {
				        _alertDefinitions[definition.alertDefinitionId.ToString()] = definition;
			        }
		        }
	        }	
        }

        public bool DoesPropertyExist(object obj, string propertyName)
        {
            return obj.GetType().GetProperty(propertyName) != null;
        }

        //checks to see if there is a FontAlertAction
        private bool HasVisualAlert(dynamic alertDefinition){
	        if(alertDefinition == null )//|| !DoesPropertyExist(alertDefinition, "alertRules"))
            {
		        return false;
	        }
	        foreach (var rule in alertDefinition.alertRules)
            {
                foreach (var alert in rule.alertActions)
                {   
                    if(alert.targetId == "ININ.Supervisor.FontAlertAction")
				        return true;
		        }
	        }
	        return false;
        }

        #region Statistic Properties

        private StatisticViewModel _longestAvailable;
        public StatisticViewModel LongestAvailable
        {
            get
            {
                return _longestAvailable;
            }
            set
            {
                _longestAvailable = value;
                RaisePropertyChanged("LongestAvailable");
            }
        }

        private StatisticViewModel _agentsLoggedInAndActivated;
        public StatisticViewModel AgentsLoggedInAndActivated
        {
            get
            {
                return _agentsLoggedInAndActivated;
            }
            set
            {
                _agentsLoggedInAndActivated = value;
                RaisePropertyChanged("AgentsLoggedInAndActivated");
            }
        }
        
        private StatisticViewModel _notAvailable;
        public StatisticViewModel NotAvailable
        {
            get
            {
                return _notAvailable;
            }
            set
            {
                _notAvailable = value;
                RaisePropertyChanged("NotAvailable");
            }
        }

        private StatisticViewModel _numberAvailableForACDInteractions;
        public StatisticViewModel NumberAvailableForACDInteractions
        {
            get
            {
                return _numberAvailableForACDInteractions;
            }
            set
            {
                _numberAvailableForACDInteractions = value;
                RaisePropertyChanged("NumberAvailableForACDInteractions");
            }
        }

        private StatisticViewModel _percentAvailable;
        public StatisticViewModel PercentAvailable
        {
            get
            {
                return _percentAvailable;
            }
            set
            {
                _percentAvailable = value;
                RaisePropertyChanged("PercentAvailable");
            }
        }
        
        private StatisticViewModel _averageHoldTime;
        public StatisticViewModel AverageHoldTime
        {
            get
            {
                return _averageHoldTime;
            }
            set
            {
                _averageHoldTime = value;
                RaisePropertyChanged("AverageHoldTime");
            }
        }

        private StatisticViewModel _averageTalkTime;
        public StatisticViewModel AverageTalkTime
        {
            get
            {
                return _averageTalkTime;
            }
            set
            {
                _averageTalkTime = value;
                RaisePropertyChanged("AverageTalkTime");
            }
        }

        private StatisticViewModel _averageWaitTime;
        public StatisticViewModel AverageWaitTime
        {
            get
            {
                return _averageWaitTime;
            }
            set
            {
                _averageWaitTime = value;
                RaisePropertyChanged("AverageWaitTime");
            }
        }

        private StatisticViewModel _interactionsAbandoned;
        public StatisticViewModel InteractionsAbandoned
        {
            get
            {
                return _interactionsAbandoned;
            }
            set
            {
                _interactionsAbandoned = value;
                RaisePropertyChanged("InteractionsAbandoned");
            }
        }

        private StatisticViewModel _interactionsAnswered;
        public StatisticViewModel InteractionsAnswered
        {
            get
            {
                return _interactionsAnswered;
            }
            set
            {
                _interactionsAnswered = value;
                RaisePropertyChanged("InteractionsAnswered");
            }
        }

        private StatisticViewModel _interactionsCompleted;
        public StatisticViewModel InteractionsCompleted
        {
            get
            {
                return _interactionsCompleted;
            }
            set
            {
                _interactionsCompleted = value;
                RaisePropertyChanged("InteractionsCompleted");
            }
        }

        private StatisticViewModel _interactionsConnected;
        public StatisticViewModel InteractionsConnected
        {
            get
            {
                return _interactionsConnected;
            }
            set
            {
                _interactionsConnected = value;
                RaisePropertyChanged("InteractionsConnected");
            }
        }
        #endregion Statistic Properties
    }
}
