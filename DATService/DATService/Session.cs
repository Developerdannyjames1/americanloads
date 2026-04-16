using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.ServiceModel;
using DATService.ServiceReference1;
using System.Configuration;
using System.Diagnostics;
using System.Data;
using System.Data.SqlClient;
using Newtonsoft.Json;
using ASTDAT.Tools;
using ASTDAT.Data.Models;

namespace DATService
{
    public class Session
    {
        ApplicationHeader _applicationHeader;
        CorrelationHeader _correlationHeader;
        SessionHeader _sessionHeader;

        public ApplicationHeader ApplicationHeader { get { return _applicationHeader; } }

        public CorrelationHeader CorrelationHeader { get { return _correlationHeader; } }

        public SessionHeader SessionHeader
        {
            get
            {
                if (_sessionHeader == null)
                {
                    return null;
                }

                var sessionToken = (_sessionHeader.Item as SessionToken);
                return new SessionHeader
                {
                    Item = sessionToken,
                };
                //return _sessionHeader;
            }
        }

        TfmiFreightMatchingPortTypeClient _client;
        
        public LoginSuccessData LoginData { get { return loginData; } }

		public void ClearConnected(string reason)
		{
			Logger.Write($"ClearConnected {reason}");
			isConnected = false;
		}

		LoginSuccessData loginData = null;
        public bool IsConnected
        {
            get
            {
                return CheckLogin();
            }
        }

        bool isConnected = false;

        public CityAndState Origin { get; set; }
        public CityAndState Destination { get; set; }
        /*public List<PostAssetOperation> Loads
        {
            get { return loads; }
            set { loads = value; }
        }
        List<PostAssetOperation> loads = new List<PostAssetOperation>() { };*/

        public string Url { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string ConnectionString { get; set; }

        public Session()
        {
            Url = ConfigurationManager.AppSettings["DATServiceURL"];
            Username = ConfigurationManager.AppSettings["DATServiceUser"];
            Password = ConfigurationManager.AppSettings["DATServicePass"];
            ConnectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
        }

        private object lockObject = new object();

        public Session(string configPath)
        {
            Logger.Write($"Session.Constructor");

            if (!File.Exists(Path.Combine(configPath, "ASTService.cfg")))
            {
                throw new Exception("File does not exist: " + Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "ASTService.cfg"));
            }
            ExeConfigurationFileMap map = new ExeConfigurationFileMap()
            {
                ExeConfigFilename = Path.Combine(configPath, "ASTService.cfg")
            };
            var settings = ConfigurationManager.OpenMappedExeConfiguration(map, ConfigurationUserLevel.None);

            Url = settings.AppSettings.Settings["DATServiceURL"].Value;
            Username = settings.AppSettings.Settings["DATServiceUser"].Value;
            Password = settings.AppSettings.Settings["DATServicePass"].Value;
            ConnectionString = settings.ConnectionStrings.ConnectionStrings["DefaultConnection"].ConnectionString;

            Login();
        }

        public static DateTime? LastLogin = null;
        public static string LastLoginInfo = null;
        public static int LastToken = 0;
		public static bool LastLoginInvalidAuth = false;
		public static int LastLoginAttempts = 0;
		public static DateTime LastLoginByUserPass = DateTime.Now.Date.AddDays(-1);
		public static bool LockInternet = false;
		
		public static int LastLoginAttemptsByToken = 0;
		public static DateTime LastLoginByToken = DateTime.Now.Date.AddDays(-1);

		public void Login()
        {
            try
            {
                if (isConnected)
                {
                    return;
                }

                if (string.IsNullOrWhiteSpace(Url) || string.IsNullOrWhiteSpace(Password) || string.IsNullOrWhiteSpace(Username))
                {
                    Logger.Write("DAT login: can not log in since the necessary data is not specified");
                    lastExceptions.Add(new Tuple<DateTime, Exception>(DateTime.Now, new Exception("Can not log in since the necessary data is not specified")));
                    return;
                }
                lock (lockObject)
                {
                    Logger.Write($"DAT login start {this.GetHashCode()}");

                    // build client to TFMI service 
                    if (_client == null)
                    {
                        var remoteAddress = new EndpointAddress(Url);
                        var binding = new BasicHttpBinding(BasicHttpSecurityMode.None) { MaxReceivedMessageSize = 2 << 20 };
                        _client = new TfmiFreightMatchingPortTypeClient(binding, remoteAddress);
                        //-----
                        //ALLOW INTERCEPT ALL REQUESTS AND RESPONCES
                        //look in MyMessageInspector C:\Sources\AST-DAT\DATService\DATService\Inspector.cs
                        //var requestInterceptor = new InspectorBehavior();
                        //_client.Endpoint.EndpointBehaviors.Add(requestInterceptor);
                        //-----
                        if (Url.ToLower().StartsWith("https"))
                        {
                            var binding2 = new BasicHttpsBinding(BasicHttpsSecurityMode.Transport) { MaxReceivedMessageSize = 2 << 20 };
                            _client = new TfmiFreightMatchingPortTypeClient(binding2, remoteAddress);
                        }
                    }

                    using (var db = new DBContext())
                    {
                        var token = db.DATLogins
							.Where(x => x.Expiration > DateTime.Now && x.TokenPrimary != null && x.TokenSecondary != null)
							.OrderByDescending(x => x.DateTime)
							.FirstOrDefault();

                        if (token != null && (token.Id != LastToken || LastLoginAttemptsByToken <= 3))
                        {
							if (token.Id == LastToken)
							{
								if ((DateTime.Now - LastLoginByToken).TotalMinutes < 2)
								{
									Logger.Write($"Can not login by token, last attempt less then 2 min. last {LastLoginByToken} now {DateTime.Now}");
									return;
								}
								else
								{
									LastLoginAttemptsByToken++;
									Logger.Write($"DAT last login by token IS NOT VALID. Attempt {LastLoginAttemptsByToken}");
								}
							}
							LastLoginByToken = DateTime.Now;

							loginData = new LoginSuccessData
                            {
                                expiration = token.Expiration.Value,
                                token = new SessionToken
                                {
                                    expiration = token.Expiration.Value,
                                    primary = token.TokenPrimary.Take(32).ToArray(),
                                    secondary = token.TokenSecondary.Take(32).ToArray(),
                                },
                            };

							LastToken = token.Id;
							if (!LockInternet)
							{
								_correlationHeader = new CorrelationHeader();
								_sessionHeader = BuildSessionHeader(LoginData);
								isConnected = true;
								LastLogin = DateTime.Now;
								LastLoginInfo = $"By token ({token.DateTime} / {token.Expiration})";

								Logger.Write($"DAT login by token {Encoding.Default.GetString(token.TokenPrimary)} {Encoding.Default.GetString(token.TokenSecondary)} {token.DateTime} {token.Expiration}");
							}
                            return;
                        }
                        else if (token != null && token.Id == LastToken)
                        {
                            Logger.Write($"DAT last login by token IS NOT VALID");
                        }
                    }

                    LastLogin = null;
                    LastLoginInfo = "";
                    using (var db = new DBContext())
                    {
                        //2 in days
                        DateTime startOfDay = DateTime.Now.Date;
                        DateTime endOfDay = startOfDay.AddDays(1);
                        if (db.DATLogins.Where(x => x.DateTime >= startOfDay && x.DateTime < endOfDay).Count() >= 2)
                        {
                            var dt = db.DATLogins.OrderByDescending(x => x.DateTime).Take(2).ToList();
                            LastLogin = dt[0].DateTime;
                            LastLoginInfo = $"More then 2 logins in day. ({dt[0].DateTime.ToString()}) and ({dt[1].DateTime.ToString()})";
                            Logger.Write(LastLoginInfo);
                            //return;
                        }
                        //less 12h
                        var dt12h = DateTime.Now.AddHours(-12);
                        if (db.DATLogins.Any(x => x.DateTime >= dt12h))
                        {
                            LastLogin = db.DATLogins.OrderByDescending(x => x.DateTime).FirstOrDefault().DateTime;
                            LastLoginInfo = "There is already a login in less than 12H";
                            Logger.Write(LastLoginInfo);
                            //return;
                        }

						LastLoginAttempts++;
						if (LastLoginAttempts > 3)
						{
							if ((DateTime.Now - LastLoginByUserPass).TotalMinutes < 5)
							{
								Logger.Write($"Can not login, last attempt less then 5 min. last {LastLoginByUserPass} now {DateTime.Now}");
								return;
							}
						}
					}

					LastLoginByUserPass = DateTime.Now;
					LastLogin = DateTime.Now;
                    Logger.Write($"DAT login by user/pass {this.GetHashCode()}");
                    if (_sessionHeader != null)
                    {
                        var token = (_sessionHeader.Item as SessionToken);
                        Logger.Write($"DAT login {LastLogin} |{(token != null ? System.Text.Encoding.Default.GetString(token.primary) : "")}|{(token != null ? System.Text.Encoding.Default.GetString(token.secondary) : "")}");
                    }
                    else
                    {
                        Logger.Write($"DAT login {LastLogin}");
                    }

                    // build request
                    var loginRequest = new LoginRequest
                    {
                        loginOperation = new LoginOperation { loginId = Username, password = Password, thirdPartyId = "SampleClient.NET" }
                    };

                    // build various headers required by the service method
                    var applicationHeader = new ApplicationHeader
                    {
                        application = "AST service",
                        applicationVersion = "1.0"
                    };
                    var correlationHeader = new CorrelationHeader();
                    var sessionHeader = new SessionHeader
                    {
                        Item = new SessionToken { primary = new byte[] { }, secondary = new byte[] { } }
                    };

                    // invoke the service
                    WarningHeader warningHeader;
                    LoginResponse loginResponse;
                    _client.Login(applicationHeader,
                                 ref correlationHeader,
                                 ref sessionHeader,
                                 loginRequest,
                                 out warningHeader,
                                 out loginResponse);

                    var data = loginResponse.loginResult.Item as LoginSuccessData;
                    if (data == null || LockInternet)
                    {
                        var serviceError = loginResponse.loginResult.Item as ServiceError;
                        isConnected = false;
						LastLoginInvalidAuth = false;
						Logger.WriteError($"DAT Login failed {Url}");
                        if (serviceError != null)
                        {
                            Logger.WriteError($"{serviceError.faultCode}");
                            Logger.WriteError($"{serviceError.message}");
                            Logger.WriteError($"{serviceError.detailedMessage}");
                        }
						//throw new Exception(serviceError.detailedMessage);
						return;
                    }
                    else
                    {
                        using (var db = new DBContext())
                        {
                            db.DATLogins.Add(new ASTDAT.Data.DATLoginModel
                            {
                                DateTime = DateTime.Now,
                                Message = "Success login",
                                TokenPrimary = data.token.primary,
                                TokenSecondary = data.token.secondary,
                                Expiration = data.expiration,
                            });
                            db.SaveChanges();
                        }
                    }
                    _applicationHeader = applicationHeader;
                    _correlationHeader = correlationHeader;
                    _sessionHeader = BuildSessionHeader(data);
                    loginData = data;
                    isConnected = true;
					LastLoginAttempts = 0;
					LastLoginAttemptsByToken = 0;

					_client.ChannelFactory.Closed += (s, e) => 
					{ 
						isConnected = false;
						LastLoginInvalidAuth = false;
						Logger.Write("ChannelFactory.Closed"); 
					};
                    _client.ChannelFactory.Faulted += (s, e) => 
					{ 
						isConnected = false;
						LastLoginInvalidAuth = false;
						Logger.Write("ChannelFactory.Faulted"); 
					};
                }
            }
            catch(Exception exc)
            {
                Logger.Write("Exception DAT Login", exc);
                lastExceptions.Add(new Tuple<DateTime, Exception>(DateTime.Now, exc));
            }
        }

        public bool CheckLogin()
        {
			if (LoginData == null || LoginData.expiration <= DateTime.Now)
			{
				if (LoginData == null)
				{
					Logger.Write($"LoginData == null ({LoginData == null})");
				}
				else
				{
					Logger.Write($"LoginData.expiration <= DateTime.Now ({LoginData.expiration} <= DateTime.Now)");
				}
				loginData = null;
                isConnected = false;
				LastLoginInvalidAuth = false;
                return false;
            }
            return isConnected;
        }

        public bool SetOrigin(string City, string State, string County = null)
        {
            Origin = GetOriginDestination(City, State, County);
            return (Origin != null);
        }

        public bool SetDestination(string City, string State, string County = null)
        {
            Destination = GetOriginDestination(City, State, County);
            return (Destination != null);
        }

        CityAndState GetOriginDestination(string City, string State, string County)
        {
            StateProvince state ;
            if (!Enum.TryParse(State, out state))
            {
                return null;
            }

            return new CityAndState
            {
                city = City,
                county = County,
                stateProvince = state
            };
        }

        public PostAssetOperation CreatePostAssetOperation(RowModel row, string messageID, out EquipmentType eqType, string refId)
        {
            if (messageID == null)
            {
                messageID = "";
            }
            if (String.IsNullOrEmpty(refId))
            {
                refId = DateTime.Now.Ticks.ToString();
                refId = refId.Substring(refId.Length - 8);
            }
            //EquipmentType eqType;
            if (!Enum.TryParse(row.AssetType, out eqType))
            {
                eqType = EquipmentType.Van;
            }

            var shipment = new Shipment
            {
                destination = new Place { Item = GetOriginDestination(row.DestinationCity, row.DestinationState, null) },
                equipmentType = eqType,
                origin = new Place { Item = GetOriginDestination(row.OriginCity, row.OriginState, null) },
                //rate =
                //                   new ShipmentRate
                //                   {
                //                       baseRateDollars = 1700,
                //                       rateBasedOn = RateBasedOnType.Flat,
                //                       rateMiles = 951,
                //                       rateMilesSpecified = true
                //                   },
                //truckStops =
                //                   new TruckStops
                //                   {
                //                       enhancements = new[] { TruckStopVideoEnhancement.Flash, TruckStopVideoEnhancement.Highlight },
                //                       Item = new ClosestTruckStops(),
                //                       posterDisplayName = "12345"
                //                   }
            };
            if (row.Price > 0)
            {
                shipment.rate = new ShipmentRate
                {
                    baseRateDollars = (float)row.Price,
                    rateBasedOn = RateBasedOnType.Flat,
                };

            }

			var pickUpdate = row.PickUpStart.HasValue ? row.PickUpStart.Value : DateTime.Now.Date;
			if (pickUpdate.Hour == 0 && pickUpdate.Minute == 0 && pickUpdate.Second == 0)
			{
				//pickUpdate = pickUpdate.Date.AddDays(1).AddSeconds(-1);
			}

			var postAssetOperation = new PostAssetOperation
            {
                availability =
                        new Availability
                        {
							earliest = pickUpdate,
							earliestSpecified = true,
                            latest = pickUpdate.AddHours(24),
                            latestSpecified = true
                        },
                comments = new[] { row.Instructions },
                count = 1,
                countSpecified = true,
                includeAsset = true,
                includeAssetSpecified = true,
                Item = shipment,
                //ltl = true,
                //ltlSpecified = true,
                postersReferenceId = refId,
                stops = 0,
                stopsSpecified = true
            };
            if (row.Weight > 0 || row.AssetLength > 0)
            {
                postAssetOperation.dimensions =
                    new Dimensions
                    {
                        //                                 heightInches = 48,
                        //                                 heightInchesSpecified = true,
                        //                                 lengthFeet = 30,
                        //                                 lengthFeetSpecified = true,
                        //                                 volumeCubicFeet = 0,
                        //                                 volumeCubicFeetSpecified = false,
                        weightPounds = row.Weight,
                        weightPoundsSpecified = row.Weight > 0,
                        lengthFeet = row.AssetLength,
                        lengthFeetSpecified = row.AssetLength > 0
                    };
            }

            return postAssetOperation;
        }

        public PostRequest UploadDAT(List<PostAssetOperation> items = null, bool removeOld = false, int attemps = 2)
        {
            lock (lockObject)
            {
                try
                {
                    if (!IsConnected && attemps > 0)
                    {
                        Login();
                    }
                    if (!IsConnected)
                    {
                        Logger.WriteError("UploadDAT.!IsConnected");
                        return null;
                    }
                    if (removeOld)
                    {
                        //DeleteAllAssets();
                    }

                    var json = "";
                    try
                    {
                        //json = JsonConvert.SerializeObject(items == null ? loads.ToArray() : items.ToArray());
                        json = JsonConvert.SerializeObject(items.ToArray());
                    }
                    catch (Exception exc)
                    {
                        json = $"EXCEPTION Session.UploadDAT start JSON {exc.Message} {(exc.InnerException == null ? "" : exc.InnerException.Message)} {exc.StackTrace}";
                    }
                    Logger.Write($"Session.UploadDAT start JSON, json:{json}");

                    //var postRequest = new PostRequest() { Request = new PostAssetRequest { postAssetOperations = items == null ? loads.ToArray() : items.ToArray() } };
                    var postRequest = new PostRequest()
                    {
                        Request = new PostAssetRequest
                        {
                            postAssetOperations = items.ToArray()
                        }
                    };
                    foreach(var item in postRequest.Request.postAssetOperations)
                    {
                        var comment = $"{String.Join(" ", item.comments ?? new string[0])}";
                        item.comments = new string[1] { comment.Substring(0, Math.Min(comment.Length, 70)) };
                    }
                    var resultRequest = Post(postRequest);
                    if (resultRequest.Response == null)
                    {
                        json = "";
                        try
                        {
                            json = JsonConvert.SerializeObject(postRequest.Request.postAssetOperations);
                        }
                        catch(Exception exc)
                        {
                            json = $"resultRequest.Response == null JSON {exc.Message} {(exc.InnerException == null ? "" : exc.InnerException.Message)} {exc.StackTrace}";
                        }
                        Logger.Write($"Session.UploadDAT resultRequest.Response == null, json:{json}");
                    }
                    if (resultRequest.Response != null)
                    {
                        var authFail = resultRequest.Response.postAssetResults.FirstOrDefault(x => x.Item is ServiceError);
                        if (authFail != null)
                        {
                            Logger.Write("Session.UploadDAT authFail != null");
                            var error = (authFail.Item as ServiceError);
                            if (error.faultCode == ServiceReference1.FaultCode.InvalidAuthentication)
                            {
                                isConnected = false;
								LastLoginInvalidAuth = true;
								Logger.Write("UploadDAT InvalidAuthentication");
                                return UploadDAT(items, removeOld, --attemps);
                            }
                        }

                        foreach (PostAssetResult postAssetResult in resultRequest.Response.postAssetResults)
                        {
                            var postAssetSuccessData = postAssetResult.Item as PostAssetSuccessData;
                            if (postAssetSuccessData == null)
                            {
                                var serviceError = postAssetResult.Item as ServiceError;
                                //retVal.Error = serviceError;
                                //serviceError.Display();
                                Logger.WriteError($"serviceError.detailedMessage:{serviceError?.detailedMessage}, code:{serviceError?.code}, faultCode:{serviceError?.faultCode}, message:{serviceError?.message}, name:{serviceError?.name}");
                            }
                            else
                            {
                                //update data in SQL table
                                var conn = new SqlConnection(ConnectionString);

                                conn.Open();
                                if (conn.State == ConnectionState.Open)
                                {
                                    using (SqlCommand comm = new SqlCommand("", conn))
                                    {
                                        comm.CommandText = "set dateformat mdy";
                                        try
                                        {
                                            Logger.Write($"postersReferenceId: {postAssetSuccessData.asset.postersReferenceId}, AssetiId: {postAssetSuccessData.assetId}");
                                            comm.CommandText = $"update [Loads] set [AssetId] = '{postAssetSuccessData.assetId}'," +
                                                $"[DateLoaded]='{new DateTimeOffset(postAssetSuccessData.asset.status.created.date).LocalDateTime.ToString("MM/dd/yyyy HH:mm:ss")}'," +
                                                $"[DateDatLoaded]='{new DateTimeOffset(postAssetSuccessData.asset.status.created.date).LocalDateTime.ToString("MM/dd/yyyy HH:mm:ss")}'," +
                                                $"[AvailabilityEarliest]='{new DateTimeOffset(postAssetSuccessData.asset.availability.earliest).LocalDateTime.ToString("MM/dd/yyyy HH:mm:ss")}'," +
                                                $"[AvailabilityLatest] = '{new DateTimeOffset(postAssetSuccessData.asset.availability.latest).LocalDateTime.ToString("MM/dd/yyyy HH:mm:ss")}', " +
                                                $"[DateDatDeleted] = null " + 
                                                $" where [PostersReferenceId]='{postAssetSuccessData.asset.postersReferenceId}'";
                                            comm.ExecuteNonQuery();
                                            var sh = postAssetSuccessData.asset.Item as Shipment;
                                            if (sh != null)
                                            {
                                                var ll = sh.origin.Item as NamedLatLon;
                                                if (ll != null)
                                                {
                                                    comm.CommandText = $"update [OriginDestination] set [Latitude] = {ll.latitude.ToString(new CultureInfo("en-US"))}, [Longitude] = {ll.longitude.ToString(new CultureInfo("en-US"))} " +
                                                        $"where [Id] in (select [OriginId] from [Loads] where [PostersReferenceId]='{postAssetSuccessData.asset.postersReferenceId}')";
                                                    comm.ExecuteNonQuery();
                                                }
                                                ll = sh.destination.Item as NamedLatLon;
                                                if (ll != null)
                                                {
                                                    comm.CommandText = $"update [OriginDestination] set [Latitude] = {ll.latitude.ToString(new CultureInfo("en-US"))}, [Longitude] = {ll.longitude.ToString(new CultureInfo("en-US"))} " +
                                                        $"where [Id] in (select [DestinationId] from [Loads] where [PostersReferenceId]='{postAssetSuccessData.asset.postersReferenceId}')";
                                                    comm.ExecuteNonQuery();
                                                }
                                            }
                                        }
                                        catch (Exception exc)
                                        {
                                            Logger.Write($"SQL", exc);
                                            //isConnected = false;
                                        }

                                        //retVal.Response   = postAssetSuccessData;
                                        //retVal.Warnings = postAssetSuccessData.assetId;
                                    }
                                    conn.Close();
                                }
                            }
                        }
                    }

                    //ClearLoads();
                    return postRequest;
                }
                catch(Exception exc)
                {
                    Logger.Write($"Session.UploadDAT", exc);
                    //isConnected = false;
                }
            }
            return null;
        }

        public LookupAssetSuccessData GetLoads(int attemps = 2)
        {
            lock (lockObject)
            {
                Logger.WriteError($"GetLoads start");
                try
                {
                    if (!IsConnected && attemps > 0)
                    {
                        Login();
                    }
                    if (!IsConnected)
                    {
                        Logger.WriteError($"GetLoads.!IsConnected");
                        return null;
                    }

                    var lookupAssetRequest = new LookupAssetRequest();
                    lookupAssetRequest.lookupAssetOperation = new LookupAssetOperation
                    {
                        Item = new QueryAllMyAssets
                        {

                        },
                    };

                    WarningHeader warningHeader;
                    LookupAssetResponse lookupAssetResponse;
                    SessionHeader sessionHeader = SessionHeader;

                    var r = _client.LookupAsset(_applicationHeader,
                        ref _correlationHeader,
                        ref sessionHeader,
                        lookupAssetRequest,
                        out warningHeader,
                        out lookupAssetResponse
                        );

                    if (lookupAssetResponse.lookupAssetResult.Item is ServiceError)
                    {
                        var error = (lookupAssetResponse.lookupAssetResult.Item as ServiceError);
                        if (error.faultCode == ServiceReference1.FaultCode.InvalidAuthentication)
                        {
                            isConnected = false;
							LastLoginInvalidAuth = true;
							Logger.Write("GetLoads InvalidAuthentication");
							return GetLoads(--attemps);
                        }
                    }

                    var result = (lookupAssetResponse.lookupAssetResult.Item as LookupAssetSuccessData);

                    Logger.WriteError($"GetLoads assets:{(result.assets != null ? result.assets.Length : 0)}, warnings:{(result.warnings != null ? result.warnings.Length : 0)}");

                    return result;

                }
                catch(Exception exc)
                {
                    //isConnected = false;
                    Logger.Write($"GetLoads.Exception", exc);
                    lastExceptions.Add(new Tuple<DateTime, Exception>(DateTime.Now, exc));
                }
            }

            return null;
        }
 
        List<Tuple<DateTime, Exception>> lastExceptions = new List<Tuple<DateTime, Exception>>();
        public List<Tuple<DateTime, Exception>> LastExceptions
        {
            get
            {
                var r = lastExceptions.ToList();
                lastExceptions.Clear();
                return r;
            }
        }

        public PostRequest Post(PostRequest PostRequest)
        {
            PostRequest retVal = PostRequest;
            CorrelationHeader correlationHeader = _correlationHeader;
            SessionHeader sessionHeader = SessionHeader;

            WarningHeader warningHeader;
            PostAssetResponse postAssetResponse;
            try
            {
                _client.PostAsset(_applicationHeader,
                    ref correlationHeader,
                    ref sessionHeader,
                    PostRequest.Request,
                    out warningHeader,
                    out postAssetResponse);
                retVal.Response = postAssetResponse;
            }
            catch (Exception exc)
            {
                if (PostRequest != null && PostRequest.Request != null && PostRequest.Request.postAssetOperations != null)
                {
                    Logger.Write($"EXCEPTION Session.Post postersReferenceId:{String.Join(",", PostRequest.Request.postAssetOperations.Select(x => x.postersReferenceId).ToList())}");
                }                

                Logger.Write($"EXCEPTION Session.Post Message:{exc.Message}, InnerException.Message:{(exc.InnerException == null ? "" : exc.InnerException.Message)}, ST:{exc.StackTrace}");
                lastExceptions.Add(new Tuple<DateTime, Exception>(DateTime.Now, exc));
                retVal.Response = null;
            }
            return retVal;
        }

        private SessionHeader BuildSessionHeader(LoginSuccessData data)
        {
            return new SessionHeader
            {
                
                Item = new SessionToken
                {
                    expiration = data.expiration,
                    primary = data.token.primary,
                    secondary = data.token.secondary,
                    expirationSpecified = true
                }
            };
        }

        public void DeleteAllAssets()
        {
            var deleteAssetOperation = new DeleteAssetOperation { Item = new DeleteAllMyAssets() };
            var deleteAssetRequest = new DeleteAssetRequest { deleteAssetOperation = deleteAssetOperation };

            CorrelationHeader correlationHeader = _correlationHeader;
            SessionHeader sessionHeader = SessionHeader;

            WarningHeader warningHeader;
            DeleteAssetResponse deleteAssetResponse;
            _client.DeleteAsset(_applicationHeader,
                ref correlationHeader,
                ref sessionHeader,
                deleteAssetRequest,
                out warningHeader,
                out deleteAssetResponse);
        }

        public DeleteAssetResponse DeleteAssetsByIds(string[] assetIds, int attempts = 2)
        {
            lock (lockObject)
            {
                try
                {
                    if (!IsConnected && attempts > 0)
                    {
                        Login();
                    }
                    if (!IsConnected)
                    {
                        Logger.WriteError($"GetLoads.!IsConnected");
                        return null;
                    }

                    var deleteAssetOperation = new DeleteAssetOperation { Item = new DeleteAssetsByAssetIds { assetIds = assetIds } };
                    var deleteAssetRequest = new DeleteAssetRequest { deleteAssetOperation = deleteAssetOperation };

                    /* pass a local variable as a "ref" parameter, rather than passing the field itself, so 
                     * the service can't modify what the field refers to */
                    CorrelationHeader correlationHeader = _correlationHeader;
                    SessionHeader sessionHeader = SessionHeader;

                    WarningHeader warningHeader;
                    DeleteAssetResponse deleteAssetResponse;
                    _client.DeleteAsset(_applicationHeader,
                        ref correlationHeader,
                        ref sessionHeader,
                        deleteAssetRequest,
                        out warningHeader,
                        out deleteAssetResponse);

                    if (deleteAssetResponse != null && deleteAssetResponse.deleteAssetResult != null && deleteAssetResponse.deleteAssetResult.Item is ServiceError)
                    {
                        var error = (deleteAssetResponse.deleteAssetResult.Item as ServiceError);
                        if (error.faultCode == ServiceReference1.FaultCode.InvalidAuthentication)
                        {
                            isConnected = false;
							LastLoginInvalidAuth = true;
							Logger.Write("DeleteAssetsByIds InvalidAuthentication");
							return DeleteAssetsByIds(assetIds, --attempts);
                        }
                        Logger.Write($"DeleteAssetsByIds Error {error.faultCode}|{error.message}|{error.detailedMessage}");
                    }

                    return deleteAssetResponse;
                }
                catch(Exception exc)
                {
                    Logger.Write("EXCEPTION DeleteAssetsByIds", exc);
                }
            }

            return null;
        }

        /*public bool BuildLoad(DateTime when)
        {
            string refId = when.Millisecond.ToString();

            var shipment = new Shipment
            {
                destination = new Place { Item = Destination },
                equipmentType = EquipmentType.Flatbed,
                origin = new Place { Item = Origin },
                //rate =
                //                   new ShipmentRate
                //                   {
                //                       baseRateDollars = 1700,
                //                       rateBasedOn = RateBasedOnType.Flat,
                //                       rateMiles = 951,
                //                       rateMilesSpecified = true
                //                   },
                //truckStops =
                //                   new TruckStops
                //                   {
                //                       enhancements = new[] { TruckStopVideoEnhancement.Flash, TruckStopVideoEnhancement.Highlight },
                //                       Item = new ClosestTruckStops(),
                //                       posterDisplayName = "12345"
                //                   }
            };

            var postAssetOperation = new PostAssetOperation
            {
                availability =
                                             new Availability
                                             {
                                                 earliest = when,
                                                 earliestSpecified = true,
                                                 latest = when.Date.AddDays(5),
                                                 latestSpecified = true
                                             },
                comments = new[] { "Call Now!" },
                count = 1,
                countSpecified = true,
                dimensions =
                    new Dimensions
                    {
                        //                                 heightInches = 48,
                        //                                 heightInchesSpecified = true,
                        //                                 lengthFeet = 30,
                        //                                 lengthFeetSpecified = true,
                        //                                 volumeCubicFeet = 0,
                        //                                 volumeCubicFeetSpecified = false,
                        weightPounds = 45000,
                        weightPoundsSpecified = true
                    },
                includeAsset = true,
                includeAssetSpecified = true,
                Item = shipment,
                //ltl = true,
                //ltlSpecified = true,
                postersReferenceId = refId,
                stops = 0,
                stopsSpecified = true
            };
            var reqloads = new PostAssetRequest { postAssetOperations = loads.ToArray() };
            return true;
        }*/

        //public UpdateAssetResponse UpdateAssets(string assetId, int attemps = 2, ShipmentRate shipmentRate = null, Dimensions dimensions = null)
		public class UpdateAssetsResult
		{
			public UpdateAssetResponse UpdateAssetResponse { get; set; }
			public bool IsException { get; set; }
			public string Message { get; set; }
		}

		public UpdateAssetsResult UpdateAssets(UpdateAssetOperation updateAssetOperation, int attemps = 2)
        {
            lock (lockObject)
            {
				try
				{
					if (!IsConnected && attemps > 0)
					{
						Login();
					}
					if (!IsConnected)
					{
						return null;
					}

					/*var updateAssetOperation = new UpdateAssetOperation
                    {
                        Item1 = new ShipmentUpdate
                        {
                            comments = new string[] { "" },
                            rate = shipmentRate,
                            dimensions = dimensions,                            
                        },
                        ItemElementName = ItemChoiceType.assetId,
                        Item = assetId,                        
                    };*/
					var updateAssetRequest = new UpdateAssetRequest
					{
						updateAssetOperation = updateAssetOperation,
					};

					/* pass a local variable as a "ref" parameter, rather than passing the field itself, so 
                     * the service can't modify what the field refers to */
					CorrelationHeader correlationHeader = _correlationHeader;
					SessionHeader sessionHeader = SessionHeader;

					WarningHeader warningHeader;
					UpdateAssetResponse updateAssetResponse;
					_client.UpdateAsset(_applicationHeader,
						ref correlationHeader,
						ref sessionHeader,
						updateAssetRequest,
						out warningHeader,
						out updateAssetResponse);

					if (updateAssetResponse != null && updateAssetResponse.updateAssetResult != null && updateAssetResponse.updateAssetResult.Item is ServiceError)
					{
						var error = (updateAssetResponse.updateAssetResult.Item as ServiceError);
						if (error.faultCode == ServiceReference1.FaultCode.InvalidAuthentication)
						{
							isConnected = false;
							LastLoginInvalidAuth = true;
							//return UpdateAssets(assetId, --attemps, shipmentRate, dimensions);
							Logger.Write("UpdateAssets InvalidAuthentication");
							return UpdateAssets(updateAssetOperation, --attemps);
						}
					}

					return new UpdateAssetsResult
					{
						UpdateAssetResponse = updateAssetResponse,
					};
                }
                catch(Exception exc)
                {
					//isConnected = false;
					return new UpdateAssetsResult
					{
						IsException = true,
						Message = exc.Message,
					};
				}
            }

            return null;
        }
    }

    public class PostRequest
    {
        public PostAssetRequest Request { get; set; }
        public PostAssetResponse Response { get; set; }
    }

    public class RowModel
    {
        public string Load { get; set; }
        public int Asset { get; set; }
        public int AssetLength { get; set; }
        public string AssetType { get; set; }
        public string AssetSubType { get; set; }
        public string MoveType { get; set; }
        public decimal Price { get; set; }
        public string AssetNumber { get; set; }
        public string Origin { get; set; }
        public string OriginCity { get; set; }
        public string OriginState { get; set; }
        public string OriginZip { get; set; }
        public string Destination { get; set; }
        public string DestinationCity { get; set; }
        public string DestinationState { get; set; }
        public string DestinationZip { get; set; }
        public DateTime? PickUpStart { get; set; }
        public DateTime? PickUpEnd { get; set; }
        public DateTime? DeliveryDate { get; set; }
        public string Instructions { get; set; }
        public string MoverNotes { get; set; }
        public int Weight { get; set; }
        public string Company { get; set; }
        public string LengthWidthHeight { get; set; }

        /*public RowModel ToDATRow()
        {
            return new RowModel()
            {
                Load = this.Load,
                Asset = this.Asset, //Not used
                AssetLength = this.AssetLength, //
                AssetNumber = this.AssetNumber,
                AssetSubType = this.AssetSubType,
                AssetType = this.AssetType,
                MoverNotes = this.MoverNotes,
                MoveType = this.MoveType,
                Origin = this.Origin,
                OriginCity = this.OriginCity,
                OriginState = this.OriginState,
                OriginZip = this.OriginZip,
                Destination = this.Destination,
                DestinationCity = this.DestinationCity,
                DestinationState = this.DestinationState,
                DestinationZip = this.DestinationZip,
                PickUpStart = this.PickUpStart,
                PickUpEnd = this.PickUpEnd,
                DeliveryDate = this.DeliveryDate,
                Instructions = this.Instructions,
                Price = this.Price,
                Weight = this.Weight,
                Company = this.Company,
                LengthWidthHeight = this.LengthWidthHeight,
            };
        }*/
    }
}
