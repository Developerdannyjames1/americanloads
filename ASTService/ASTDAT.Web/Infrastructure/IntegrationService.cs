using ASTDAT.Data.Models;
using ASTDAT.Tools;
using DATService;
using DATService.ServiceReference1;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Entity.SqlServer;
using System.Data.SqlClient;
using System.Linq;
using System.Data.Entity;
using System.Web;
//using TruckStopService.TruckStopServiceReference;
using ASTDAT.Data;
using ASTDAT.Web.Models;
using System.Threading.Tasks;
using static ASTDAT.Web.Infrastructure.MercuryGate;
using System.Text.RegularExpressions;
using System.Text;
using TruckStopRestfullService.Models;

namespace ASTDAT.Web.Infrastructure
{
	public class IntegrationService
	{
		private static IntegrationService instance = null;

		public static IntegrationService Instance
		{
			get
			{
				lock (lockObject)
				{
					if (instance == null)
					{
						instance = new IntegrationService();
					}

					return instance;
				}
			}
		}

		private static object lockObject = new object();

		private int currentID = 0;
		public Session Session { get; set; }
		//public TruckStopUtils TruckStopUtils { get; set; }
		public TruckStopRestUtils TruckStopUtils { get; set; }

		public IntegrationService()
		{
			Session = new Session();
			Session.Login();
			TruckStopUtils = new TruckStopRestUtils();
		}

		public int LoadEmails(BidsResult<ASTDAT.Data.Models.LoadModel> mercuryGateGetLoads)
		{
			lock (lockObject)
			{
				List<EMessage> messages = new List<EMessage>();
				try
				{
					var mChecker = new MailChecker();

					messages = mChecker.GetMessages();

					mChecker.Dispose();
				}
				catch (Exception exc)
				{
					//EventLoger(ex.Message, EventLogType.MailError);
					Logger.Write("Exception.LoadEmails", exc);
				}

				if (mercuryGateGetLoads == null)
				{
					Logger.Write("LoadEmails mercuryGateGetLoads == null");
				}
				//return ParseEmails(messages, mercuryGateGetLoads);
				return ParseEmails(messages.Select(x => new Tuple<string, string>(x.Html, x.Headers.MessageId)).ToList(), mercuryGateGetLoads);
			}
		}

		//public List<LoadModel> ParseEmail(EMessage message)
		public List<LoadModel> ParseEmail(string html, string messageId)
		{
			using (var db = new DBContext())
			{
				//Add message to UploadLogs
				db.UploadLogs.Add(new UploadLogModel
				{
					Company = "Insight",
					EmailBody = html,
					MessageID = messageId,
					Uploaded = false,
					Begin = DateTime.Now,
				});
				db.SaveChanges();

				//Parse
				var msg = html;
				if (msg.IndexOf("<html>") != -1) msg = msg.Substring(msg.IndexOf("<html>"));
				if (msg.IndexOf("</html>") != -1) msg = msg.Substring(0, msg.IndexOf("</html>") + 7);

				//var rowList = new DataParser().ParseXLS_Insight(msg);
				var loads = new DataParser().ParseXLS_Insight(msg);

				//Add to Loads
				foreach (var load in loads)
				{
					if (String.IsNullOrEmpty(load.EquipmentType))
					{
						continue;
					}
					var refId = DateTime.Now.Ticks.ToString();
					refId = refId.Substring(refId.Length - 8);
					load.PostersReferenceId = refId;
					load.Count = 1;
					load.Ltl = false;
					load.IncludeAsset = true;
					load.Stops = 0;
					load.IncludeAsset = true;
					load.PostToExtendedNetwork = false;
					load.DimensionsWeightPounds = load.Weight;
					//OriginId = System.Convert.ToInt32(orgid),
					//DestinationId = System.Convert.ToInt32(destid),
					//CarrierAmount = row.Price,
					load.RateEateBasedOn = 0;
					//ClientName = row.Company,
					//ClientLoadNum = row.Load,
					load.EmailID = messageId;
					//AssetLength = row.AssetLength,
					//EquipmentType = eqType.ToString(),
					//Description = $"{row.Instructions}",
					//PickUpDate = row.PickUpStart,
					//DeliveryDate = row.DeliveryDate,
					load.CreateDate = DateTime.Now;
					load.CreatedBy = $"ParseEmails {load.ClientName}";
					load.CreateLoc = "AutoApp";
					load.IsLoadFull = true;
				}
				return loads;
			}
		}

		//public int ParseEmails(List<EMessage> messages, BidsResult<ASTDAT.Data.Models.LoadModel> mercuryGateGetLoads)
		public int ParseEmails(List<Tuple<string, string>> messages, BidsResult<ASTDAT.Data.Models.LoadModel> mercuryGateGetLoads)
		{
			var countLoads = 0;
			currentID = 0;

			if (messages.Count > 0)
			{
				try
				{
					var alreadyAdded = new List<LoadModel>();
					foreach (var item in messages)
					{
						//var loads = ParseEmail(item);
						//var loads = ParseEmail(item.Html, item.Headers.MessageId);
						var loads = ParseEmail(item.Item1, item.Item2);
						foreach (var newload in loads)
						{
							if (String.IsNullOrEmpty(newload.EquipmentType))
							{
								//Logger.Write($"ParseEmails {newload.ClientLoadNum} EquipmentType is empty - skipped");
								continue;
							}
							if (newload.Origin == null || newload.Destination == null) //new load must have Origin and Destination
							{
								//Logger.Write($"ParseEmails {newload.ClientLoadNum} Origin or Destination is empty - skipped");
								continue;
							}

							Logger.Write($"ParseEmails start import {newload.ClientLoadNum}");

							#region Duplicates in DB
							//Delete exists when already exists load with same ClientLoadNum
							//2019-06-06 it must delete first, then post again
							try
							{
								using (var db = new DBContext())
								{
									//RULE #1 delete by same ClientLoadNum
									//It MUST be the same client "United Rentals"
									//not current id + current ClientLoadNum and current ClientName
									var oldloads = db.Loads.Where(x => x.Id != newload.Id && x.ClientLoadNum == newload.ClientLoadNum && x.ClientName == newload.ClientName).ToList();
									foreach (var oldload in oldloads)
									{
										IntegrationService.Instance.DeleteFromSQLAndBoards(oldload, $"ParseEmails delete duplicate same ClientLoadNum");
									}

									//RULE #2 delete by same Origin and same Destination and Same PickUp Date
									//Origin and same Destination and Same PickUp Date
									//Search loads in db
									oldloads = db.Loads.Where(x =>
										x.Id != newload.Id //not current load
										&& (!x.DateDatDeleted.HasValue) //not deleted from DAT
										&& (!x.DateTSDeleted.HasValue) //not deleted from TS
										&& x.ClientName == newload.ClientName //same client
										&& x.PickUpDate.HasValue //has PickUpDate
										&& x.Origin.Id == newload.Origin.Id //same Origin
										&& x.Destination.Id == newload.Destination.Id //same Destination
										).ToList();

									foreach (var oldload in oldloads)
									{
										if (newload.PickUpDate.HasValue)
										{
											if (oldload.PickUpDate.Value.Date == newload.PickUpDate.Value.Date)
											{
												IntegrationService.Instance.DeleteFromSQLAndBoards(oldload, $"ParseEmails delete duplicate same Origin and same Destination and Same PickUp Date");
											}
										}
									}
								}
							}
							catch (Exception exc)
							{
								Logger.Write($"EXCEPTION Delete exists by ClientLoadNum", exc);
								continue;
							}
							#endregion Duplicates in DB

							#region Description
							//Get from MercuryGate Dimensions + Item + "HazMat required"
							//Van NO STEP DECKS PPE REQUIRED Deliver 5-17 by 1200. Weight: 46,000 Dimensions: 48.0 x 8.0 X 8.0 ft  Item: (33)40'KD SHEETS
							newload.Description = $"{newload.Description}\r\nWeight: {newload.Weight}";
							if (mercuryGateGetLoads != null && mercuryGateGetLoads.Loads != null)
							{
								var mg = mercuryGateGetLoads.Loads.FirstOrDefault(x => x.LoadClientId == newload.ClientLoadNum);
								if (mg != null)
								{
									newload.LengthWidthHeight = mg.LengthWidthHeight;
									//NOTE: If there is no Description in the link, then do NOT put the label "Item:"
									//Same with the other ones, like the Len, Wid, & Hght
									if (!String.IsNullOrEmpty(mg.LengthWidthHeight))
									{
										newload.Description += $"\r\nDimensions: {mg.LengthWidthHeight}\r\n";
									}
									if (!String.IsNullOrEmpty(mg.Description))
									{
										newload.Description += $"\r\nItem: { mg.Description}\r\n";
									}
									if ($"{mg.HazMat}".ToLower() == "yes")
									{
										newload.Description += "\r\nHazMat required\r\n";
									}
								}
								else
								{
									Logger.Write($"ParseEmails mercuryGateGetLoads not found {newload.Id} {newload.ClientLoadNum}");
								}
							}
							newload.Description = newload.Description.Replace("\r\n\r\n", "\r\n");
							newload.Description = newload.Description.Replace("\r\r", "\r");
							newload.Description = newload.Description.Replace("\n\n", "\n");
							if (newload.Description.Length > 2 && newload.Description[newload.Description.Length - 1] == '\n' && newload.Description[newload.Description.Length - 2] == '\r')
							{
								newload.Description = newload.Description.Substring(0, newload.Description.Length - 2);
							}

							newload.UserNotes = "";
							var lines = newload.Description.Split('\n');
							var result = "";
							foreach (var line in lines)
							{
								if (string.IsNullOrEmpty(line))
									continue;

								//phone
								var regex = new Regex(@"\(?([0-9]{3})\)?.?([0-9]{3}).?([0-9]{4})");
								if (regex.IsMatch(line))
								{
									newload.UserNotes += $"Eliminated: \"{line.Replace("\r", "")}\"\r\n";
									continue;
								}

								//email
								regex = new Regex(@"\w+([-+.]\w+)*@\w+([-.]\w+)*\.\w+([-.]\w+)*", RegexOptions.IgnoreCase);
								if (regex.IsMatch(line))
								{
									newload.UserNotes += $"Eliminated: \"{line.Replace("\r", "")}\"\r\n";
									continue;
								}

								result += line.Replace("\r", "") + "\r\n";
							}

							newload.Description = result;

							newload.Description = newload.Description.Replace("\r\n\r\n", "\r\n");
							newload.Description = newload.Description.Replace("\r\r", "\r");
							newload.Description = newload.Description.Replace("\n\n", "\n");
							if (newload.Description.Length > 2 && newload.Description[newload.Description.Length - 1] == '\n' && newload.Description[newload.Description.Length - 2] == '\r')
							{
								newload.Description = newload.Description.Substring(0, newload.Description.Length - 2);
							}
							newload.UserNotes = newload.UserNotes.Replace("\r\n\r\n", "\r\n");
							newload.UserNotes = newload.UserNotes.Replace("\r\r", "\r");
							newload.UserNotes = newload.UserNotes.Replace("\n\n", "\n");
							if (newload.UserNotes.Length > 2 && newload.UserNotes[newload.UserNotes.Length - 1] == '\n' && newload.UserNotes[newload.UserNotes.Length - 2] == '\r')
							{
								newload.UserNotes = newload.UserNotes.Substring(0, newload.UserNotes.Length - 2);
							}

							#endregion Description

							#region Duplicates in current emails
							try
							{
								//look to duplicates in current emails list
								//by ClientLoadNum
								if (alreadyAdded.Any(x => x.ClientName == newload.ClientName && x.ClientLoadNum == newload.ClientLoadNum))
								{
									Logger.Write($"{newload.ClientLoadNum} is duplicate, already added in current ParseEmails (by ClientLoadNum)");
									continue;
								}
								//by Origin
								var duplicate = alreadyAdded.FirstOrDefault(x =>
									x.ClientName == newload.ClientName //same client
									&& x.PickUpDate.HasValue //has PickUpDate
									&& x.OriginId == newload.Origin.Id //same Origin
									&& x.DestinationId == newload.Destination.Id //same Destination
									);
								if (duplicate != null && newload.PickUpDate.HasValue)
								{
									if (duplicate.PickUpDate.Value.Date == newload.PickUpDate.Value.Date)
									{
										Logger.Write($"{newload.ClientLoadNum} is duplicate, already added in current ParseEmails (by Origin/Destination/PickUpDate)");
										continue;
									}
								}
							}
							catch (Exception exc)
							{
								Logger.Write($"EXCEPTION Duplicates in current emails", exc);
								continue;
							}
							#endregion Duplicates in current emails

							countLoads++;
							alreadyAdded.Add(newload);

							//Add to DB
							IntegrationService.Instance.AddLoadToDB(newload, null, true);
							//Upload
							var errors = IntegrationService.Instance.DoUploadLoad(newload, "ParseEmails.Add");
						}
					}
				}
				catch (Exception exc)
				{
					Logger.Write($"ParseEmails", exc);
				}
			}

			return countLoads;
		}

		public LookupAssetSuccessData GetDATLoads()
		{
			return Session.GetLoads();
		}

		public TsResponse GetTSLoads()
		{
			return TruckStopUtils.GetLoads();
		}

		private DateTime CalcNextDate(DateTime nextDate, bool AllowUntilSat, bool AllowUntilSun)
		{
			var newDate = nextDate;
			if (newDate.DayOfWeek == DayOfWeek.Saturday && AllowUntilSat != true) //not  allow Saturday
			{
				newDate = newDate.AddDays(1).Date; //Move to Sunday
			}
			if (newDate.DayOfWeek == DayOfWeek.Sunday && AllowUntilSun != true) //not allow Sunday
			{
				newDate = newDate.AddDays(1).Date; //Move to Monday
			}

			return newDate;
		}

		public void UntilDateProcessing(bool test = false)
		{
			Logger.Write($"Started", fileName: "UntilDateProcessing.txt");

			using (var db = new DBContext())
			{
				//now 04/10
				var tomorrow = DateTime.Now.AddDays(1).Date; // 05/10

				var loads = db.Loads.Where(x =>
					x.UntilDate.HasValue //has UntilDate
					&& x.UntilDate.Value >= tomorrow // UntilDate >= then tomorrow 05/10

					&& x.PickUpDate.HasValue

					&& !x.DateDatDeleted.HasValue //in DAT
					&& !x.DateTSDeleted.HasValue //in TS
					).ToList();

				var nextDate = tomorrow; // 05/10
				if (nextDate.IsUSAHoliday()) // Is USA Holiday +1 day
				{
					nextDate = nextDate.AddDays(1);
				}

				foreach (var load in loads)
				{
					try
					{
						var newDate = nextDate; //new date is next date

						newDate = CalcNextDate(newDate, load.AllowUntilSat == true, load.AllowUntilSun == true);

						if (load.PickUpDate.Value.Date >= newDate.Date) //current pickupdate more then new date date 05/10 = 05/10 or 06/10 > 05/10
						{
							continue;
						}
						if (load.UntilDate.Value.Date < newDate.Date) //new date less then until date 05/10 = 05/10, 06/10 < 05/10
						{
							continue;
						}

						Logger.Write($"Load roll over: {load.Id}, PickUpDate: {load.PickUpDate}, new PickUpDate: {newDate}, Until Date: {load.UntilDate.Value}", fileName: "UntilDateProcessing.txt");

						//Change PickUpDate
						load.PickUpDate = newDate;

						if (test)
						{
							continue;
						}

						load.AddComment($"Roll over load: {load.Id}, PickUpDate: {load.PickUpDate}, new PickUpDate: {newDate}, Until Date: {load.UntilDate.Value}");
						db.SaveChanges();

						//Delete from DAT
						var resp = this.Session.DeleteAssetsByIds(new[] { load.AssetId });
						if (resp != null && resp.deleteAssetResult != null && resp.deleteAssetResult.Item != null && resp.deleteAssetResult.Item is ServiceError)
						{
							var error = (resp.deleteAssetResult.Item as ServiceError);
							Logger.Write($"Delete from DAT UntilDateProcessing ERROR {error.faultCode}|{error.message}|{error.detailedMessage}", fileName: "UntilDateProcessing.txt");
							load.AddComment($"NOT Deleted from DAT by UntilDateProcessing {error.faultCode}|{error.message}|{error.detailedMessage} [UntilDateProcessing]");
						}
						else
						{
							//Post to DAT
							var result = this.Session.UploadDAT(new List<PostAssetOperation> { load.ToDATLoad() });
							if (result == null || result.Response == null || result.Response.postAssetResults == null || result.Response.postAssetResults.Length != 1)
							{
								Logger.Write($"No response from DAT", fileName: "UntilDateProcessing.txt");
							}
							else if (result.Response.postAssetResults[0].Item == null || result.Response.postAssetResults[0].Item is ServiceError)
							{
								var error = (result.Response.postAssetResults[0].Item as ServiceError);
								Logger.Write($"DAT error: {error.message} ({error.detailedMessage})", fileName: "UntilDateProcessing.txt");
							}
							else
							{
								var success = (result.Response.postAssetResults[0].Item as PostAssetSuccessData);
								Logger.Write($"Posted to DAT new id: {success.assetId}", fileName: "UntilDateProcessing.txt");
								load.AddComment($"Uploaded to DAT, Id:{success.assetId}, Source:UntilDateProcessing");
							}
							//Update in TS
							this.TruckStopUtils.UploadToTruckStop(load.ToTSLoad(), source: "UntilDateProcessing", logfile: "UntilDateProcessing.txt"); //Add by UntilDate
						}
					}
					catch (Exception exc)
					{
						Logger.Write($"Exception UntilDate LoadId: {load.Id}", exc, fileName: "UntilDateProcessing.txt");
					}
				}
			}
		}

		public OriginDestinationModel AddOriginDestination(DBContext db, string str)
		{
			var s = str.Split(' ');
			var city = String.Join(" ", s.Take(s.Length - 1));
			var code = $"{s[s.Length - 1]}  ".Substring(0, 2);
			var state = db.States.FirstOrDefault(x => x.Code == code);
			if (state == null)
			{
				state = new StateModel
				{
					Code = code.ToUpper(),
					Name = code.ToUpper(),
				};
				db.States.Add(state);
				db.SaveChanges();
			}
			var od = db.OriginDestinations.FirstOrDefault(x => x.City == city && x.State.Code == state.Code);
			if (od == null)
			{
				od = new OriginDestinationModel
				{
					City = city,
					StateId = state.Id,
				};
				db.OriginDestinations.Add(od);
				db.SaveChanges();
			}
			return od;
		}

		public OriginDestinationModel AddOriginDestination(DBContext db, string city, string state, string country = null)
		{
			db = db ?? new DBContext();

			country = country == "USA" ? null : country;
			country = country == "United States" ? null : country;
			var od = db.OriginDestinations.FirstOrDefault(x => x.City == city && x.State.Code == state && x.Country == country);
			if (od == null)
			{
				var stateDb = db.States.FirstOrDefault(x => x.Code == state && x.Country == country);
				if (stateDb == null)
				{
					stateDb = new StateModel
					{
						Code = state,
						Name = state,
						Country = country,
					};
					db.States.Add(stateDb);
					db.SaveChanges();
				}

				od = new OriginDestinationModel
				{
					City = city,
					Country = country,
					StateId = stateDb.Id,
				};
				db.OriginDestinations.Add(od);
				db.SaveChanges();
			}
			return od;
		}

		public List<ResultInfo> DoUploadLoad(LoadModel model, string source)
		{
			var results = new List<ResultInfo>();
			using (var db = new DBContext())
			{
				var load = db.Loads
					.Include(x => x.Destination)
					.Include(x => x.Origin)
					.Include(x => x.LoadType)
					.FirstOrDefault(x => x.Id == model.Id);

				PostRequest resultDAT = null;
				if (load.AssetId == null) //Upload to DAT
				{
					load.PostersReferenceId = load.Id.ToString();
					var postAssetOperation = load.ToDATLoad();
					db.SaveChanges();

					resultDAT = IntegrationService.Instance.Session.UploadDAT(new List<PostAssetOperation> { postAssetOperation });
					if (resultDAT == null || resultDAT.Response == null || resultDAT.Response.postAssetResults == null || resultDAT.Response.postAssetResults.Length == 0)
					{
						load.AddComment($"DoUploadLoad upload to DAT failed. Source:{source}");
						db.SaveChanges();
					}
					else
					{
						foreach (var postAssetResult in resultDAT.Response.postAssetResults)
						{
							var postAssetSuccessData = postAssetResult.Item as PostAssetSuccessData;
							if (postAssetSuccessData == null)
							{
								var serviceError = postAssetResult.Item as ServiceError;
								if (serviceError == null)
								{
									load.AddComment($"DoUploadLoad upload to DAT failed. Source:{source}");
								}
								else
								{
									load.AddComment($"DoUploadLoad upload to DAT failed, message:{serviceError.message}, detailedMessage:{serviceError.detailedMessage}. Source:{source}");
								}
							}
							else
							{
								load.AddComment($"Uploaded to DAT, AssetId:{postAssetSuccessData.assetId} Source:{source}");
							}
						}
					}
				}

				TsResponse resultTS = null;
				if (load.TsLoadId == null) //Upload to TS
				{
					resultTS = IntegrationService.Instance.TruckStopUtils.UploadToTruckStop(load.ToTSLoad(), source: source); //Add load
					if (resultTS != null && resultTS.StatusSet != null && resultTS.StatusSet.Count > 0)
					{
						load.AddComment($"Upload to TS has errors {String.Join(",", resultTS.StatusSet.Select(x => $"ErrorMessage:{x.Message}"))}");
					}
					if (resultTS == null)
					{
						load.AddComment("Upload to TS failed see Log.txt");
					}
				}

				//results.AddRange(resultDAT.ToResults(load.ClientLoadNum));
				results.AddRange(resultTS.ToResults(load.ClientLoadNum));
			}

			return results;
		}

		/*
        NEW LOGIC:
        When the Load is "marked" as deleted, save to LoadHistory table IF it has either (not both): Customer or Carrier $
        It is has BOTH, better yet: Save
        If it doesn't have Customer or Carrier $, then dont' save.
        Please check to see if that Load ID is already in LoadHistory, if so, DELETE the old one.
        NOTE: There are several apps that will Mark for deletion
        PUT THIS LOGIC IN ALL OF THEM.
        */
		public void DeleteFromSQLAndBoards(LoadModel load, string comment)
		{
			try
			{
				//DAT
				if (load.CanDeleteFromDAT()) //CAN DELETE FROM DAT
				{
					var session = IntegrationService.Instance.Session;
					var resp = session.DeleteAssetsByIds(new string[] { load.AssetId }); //DeleteFromSQLAndBoards
					if (resp != null)
					{
						var resultError = (resp.deleteAssetResult.Item as ServiceError); //ERROR
						var resultSuccess = (resp.deleteAssetResult.Item as DeleteAssetSuccessData); //SUCCESS

						if (resultError != null) //ERROR
						{
							load.AddComment($"DeleteFromSQLAndBoards DAT [resultError] [{resultError.message}] [{resultError.detailedMessage}] [{comment}]");
							if (resultError.faultCode == FaultCode.EntityNotFound) //Not found - mark as deleted
							{
								load.DateDatDeleted = DateTime.Now;
								load.AddComment($"DeleteFromSQLAndBoards DAT [Delete] [EntityNotFound1] [{comment}]");
							}
						}
						else if (resultSuccess != null) //SUCCESS
						{
							if (resultSuccess.warnings != null && resultSuccess.warnings.Length > 0)
							{
								load.AddComment($"DeleteFromSQLAndBoards DAT [resultSuccess] [{String.Join(",", resultSuccess.warnings.Select(x => x.message))}] [{comment}]");
							}
							load.DateDatDeleted = DateTime.Now;
							load.AddComment($"DeleteFromSQLAndBoards Deleted from DAT No Warnings [{comment}]");
						}
						else //NOT ERROR AND NOT SUCCESS
						{
							load.AddComment($"DeleteFromSQLAndBoards DAT [NO resultError] [NO resultSuccess] [{comment}]");
						}
					}
					else
					{
						load.AddComment($"DeleteFromSQLAndBoards DAT DeleteAssetsByIds resp == null [{comment}]");
						if (System.Configuration.ConfigurationManager.AppSettings["DeleteWithoutLoadboards"] == "true")
						{
							load.DateDatDeleted = DateTime.Now;
							load.AddComment($"DeleteFromSQLAndBoards DAT (mark !DeleteWithoutLoadboards) {comment}");
						}
					}
				}
				//else if (load.UploadedToDAT() && !load.DeletedFromDAT())
				else if (!load.DateDatDeleted.HasValue)
				{
					load.DateDatDeleted = DateTime.Now;
					load.AddComment($"DeleteFromSQLAndBoards DAT (mark) {comment}");
				}

				//TS
				if (load.CanDeletFromTS())
				{
					var truckStopUtils = IntegrationService.Instance.TruckStopUtils;

					var respTS = truckStopUtils.DeleteByIds(new string[] { load.ClientLoadNum == null ? load.Id.ToString() : load.ClientLoadNum });
					Logger.Write($"DeleteFromSQLAndBoards TS DeleteByIds ID:{load.Id} resp==null: { respTS == null }");
					Logger.Write($"DeleteFromSQLAndBoards TS (resp.LoadNumbers == null)={respTS?.Data == null}");

					if (respTS == null )
					{

						load.AddComment($"DeleteFromSQLAndBoards [respTS == null] [{comment}]");
						if (System.Configuration.ConfigurationManager.AppSettings["DeleteWithoutLoadboards"] == "true")
						{
							load.DateTSDeleted = DateTime.Now;
							load.AddComment($"DeleteFromSQLAndBoards TS (mark !DeleteWithoutLoadboards) {comment}");
						}
					}
					else if (respTS.StatusSet != null && respTS.StatusSet.Count > 0) //ERRORS
					{
						Logger.Write($"DeleteFromSQLAndBoards TS Errors:{String.Join(",", respTS.StatusSet.Select(x => x.Message))}");
						if (respTS.StatusSet.Any(x => x.Message.ToLower().Contains("Load does not exist".ToLower())) ||
                            respTS.StatusSet.Any(x => x.Message.ToLower().Contains("Load was already deleted".ToLower()))||
                            respTS.StatusSet.Any(x => x.Message.ToLower().Contains("Not Found".ToLower()))) //Not found
						{
							load.DateTSDeleted = DateTime.Now;
							load.AddComment($"DeleteFromSQLAndBoards TS [Delete] [{comment}]");
						}
					}
					else if (respTS.Data is List<string> loadNumbers && loadNumbers.Count > 0) //SUCCESS
					{
						Logger.Write($"DeleteFromSQLAndBoards TS (resp.LoadNumbers)={loadNumbers.Count}");
						load.DateTSDeleted = DateTime.Now;
						load.AddComment($"DeleteFromSQLAndBoards TS [Delete] [{comment}]");
					}
                    //else if (respTS.Errors == null || respTS.Errors.Length == 0) //SUCCESS
                    //{
                    //    Logger.Write("DeleteFromSQLAndBoards (no resp.LoadNumbers, no resp.Errors)");
                    //    load.DateTSDeleted = DateTime.Now;
                    //    load.AddComment($"DeleteFromSQLAndBoards [Delete] [{comment}]");
                    //}
                    else
					{
						Logger.Write($"DeleteFromSQLAndBoards O respTS.Errors==null {respTS.StatusSet == null}");
						Logger.Write($"DeleteFromSQLAndBoards O respTS.Errors==null {respTS.Data == null}");
						if (respTS.StatusSet != null)
						{
							Logger.Write($"DeleteFromSQLAndBoards O respTS.Errors.Length {respTS.StatusSet.Count}");
						}
						if (respTS.Data is List<string> loadNmbrs && loadNmbrs.Count > 0)
						{
							Logger.Write($"DeleteFromSQLAndBoards O respTS.LoadNumbers.Length {loadNmbrs.Count}");
						}
						if (System.Configuration.ConfigurationManager.AppSettings["DeleteWithoutLoadboards"] == "true")
						{
							load.DateTSDeleted = DateTime.Now;
							load.AddComment($"DeleteFromSQLAndBoards TS (mark !DeleteWithoutLoadboards) {comment}");
						}
					}
				}
				//else if (load.UploadedToTS() && !load.DeletedFromTS())
				else if (!load.DateTSDeleted.HasValue)
				{
					load.DateTSDeleted = DateTime.Now;
					load.AddComment($"DeleteFromSQLAndBoards TS (mark) {comment}");
				}

				/*if (!load.UploadedToDAT() && load.DeletedFromTS()) //
                {
                    load.DateDatDeleted = DateTime.Now;
                }
                if (!load.UploadedToTS() && load.DeletedFromDAT()) //
                {
                    load.DateTSDeleted = DateTime.Now;
                }*/

				//LoadHistory
				LoadHistoryDelete(load);
			}
			catch (Exception exc)
			{
				Logger.Write($"EXCEPTION DeleteFromSQLAndBoards ", exc);
			}
		}

		public int LoadHistoryDelete(LoadModel load)
		{
			if (load.CarrierAmount == 0 || !load.CustomerAmount.HasValue || load.CustomerAmount == 0)
			{
				//return 1;
			}
			if (load.AssetId != null && !load.DateDatDeleted.HasValue) //Not deleted in DAT
			{
				return 2;
			}
			//if (load.TrackStopId.HasValue && !load.DateTSDeleted.HasValue) //Not deleted in TS
			if (load.TsLoadId.HasValue && !load.DateTSDeleted.HasValue) //Not deleted in TS
			{
				return 3;
			}

			using (var db = new DBContext())
			{
				var loadHistory = db.LoadHistory.FirstOrDefault(x => x.ProNumber == load.Id);
				if (loadHistory == null)
				{
					loadHistory = new LoadHistoryModel();
					db.LoadHistory.Add(loadHistory);
				}

				loadHistory.ProNumber = load.Id;
				loadHistory.ReadyDate = load.PickUpDate;

				loadHistory.PUCity = load.Origin.City;
				loadHistory.PUState = load.Origin.State.Code;

				loadHistory.ConsCity = load.Destination.City;
				loadHistory.ConsState = load.Destination.State.Code;

				loadHistory.LHPay = load.CarrierAmount;
				loadHistory.TotalPay = load.CustomerAmount;
				loadHistory.HistoryNote = load.Comments;

				db.SaveChanges();

				return 0;
			}
		}

		public decimal GetRateMate(LoadModel load)
		{
			if (load == null || load.Destination == null || load.Origin == null)
			{
				return 0;
			}

			//LoadHistory
			using (var db = new DBContext())
			{
				var loadHistories = db.LoadHistory
					.Where(x =>
						x.TotalPay.HasValue
						&& x.PUCity == load.Origin.City
						&& x.PUState == load.Origin.State.Code
						&& x.ConsCity == load.Destination.City
						&& x.ConsState == load.Destination.State.Code)
					.OrderByDescending(x => x.TotalPay.Value)
					.ToList();

				if (loadHistories.Count == 0)
				{
					return 0;
				}

				return loadHistories[0].TotalPay.Value;
			}
		}		
        
        public int GetDatLoadsCount()
		{
            var datLoads = IntegrationService.Instance.Session.GetLoads(); //DATPostersReferenceIdUpdate
            if (datLoads != null && datLoads.assets != null)
            {
                return datLoads.assets.Length;
            }

            return 0;
        }

        public LoadModel AddLoadToDB(LoadModel model, ApplicationUser user, bool useClientLoadNum = false)
		{
			using (var db = new DBContext())
			{
				if (db.Loads.Any(x => x.Id == model.Id))
				{
					return model;
				}

				model.PickUpDate = (new DateTimeOffset(model.PickUpDate.Value)).LocalDateTime;
				if (model.DeliveryDate.HasValue)
				{
					model.DeliveryDate = (new DateTimeOffset(model.DeliveryDate.Value)).LocalDateTime;
				}
				//Create Destination
				if (model.Destination.Id == 0)
				{
					model.Destination = IntegrationService.Instance.AddOriginDestination(db, model.Destination.City);
				}
				//Create Origin
				if (model.Origin.Id == 0)
				{
					model.Origin = IntegrationService.Instance.AddOriginDestination(db, model.Origin.City);
				}
				//Create Company
				if (!db.Companies.Any(x => x.Name == model.ClientName))
				{
					db.Companies.Add(new Data.CompanyModel { Name = model.ClientName });
				}
				db.SaveChanges();

				//Two phrases to eliminate
				//QUOTE IS VALID FOR ____ DAYS
				//CREATED BY ______
				var descrOrig = $"{model.Description}";
				var descr = descrOrig.ToUpper();

				var idx1 = descr.IndexOf("QUOTE IS VALID FOR ");
				var idx2 = descr.IndexOf(" DAYS");
				if (idx1 != -1 && idx2 != -1 && idx2 - 19 - idx1 < 4)
				{
					descrOrig = descrOrig.Remove(idx1, idx2 - idx1 + 6);
					descr = descr.Remove(idx1, idx2 - idx1 + 6);
				}
				var idx3 = descr.IndexOf("CREATED BY ");
				var idx4 = -1;
				if (idx3 != -1)
				{
					idx4 = Math.Min(descr.IndexOf("\r", idx3), idx4 = descr.IndexOf(".", idx3));
				}
				if (idx3 != -1 && idx4 != -1)
				{
					descrOrig = descrOrig.Remove(idx3, idx4 - idx3);
				}
				model.Description = descrOrig;

				model.DestinationId = model.Destination.Id;
				model.Destination = null;
				model.OriginId = model.Origin.Id;
				model.Origin = null;
				model.EquipmentType = "Van";
				model.LoadTypeId = model.LoadType != null ? model.LoadType.Id : model.LoadTypeId;
				model.LoadType = null;
				if (model.LoadTypeId.HasValue)
				{
					model.EquipmentType = db.LoadTypes.FirstOrDefault(x => x.Id == model.LoadTypeId)?.Name;
				}
				model.UntilDate = model.UntilDate;
				model.AssetLength = model.AssetLength;
				model.Description = model.Description;
				model.IsLoadFull = model.IsLoadFull;
				model.DeliveryDate = model.DeliveryDate;
				model.PickUpDate = model.PickUpDate;
				model.CarrierAmount = model.CarrierAmount;

				model.CreateDate = DateTime.Now;
				if (user != null)
				{
					model.CreatedBy = user?.UserName;
					model.CreateLoc = user?.Location;
					model.UpdatedBy = user?.UserName;
					model.UpdateLoc = user?.Location;
				}
				model.UpdateDate = DateTime.Now;

				db.Loads.Add(model);
				db.SaveChanges();

				model.Description = $"Load #{model.Id}{descrOrig}";
				db.SaveChanges();

				//TODO  ClientLoadNum???
				if (!useClientLoadNum)
				{
					model.ClientLoadNum = $"{model.Id}";
					db.SaveChanges();
				}
				db.Entry(model).State = EntityState.Detached;

				return model;
			}
		}

		public async Task<BidsResult<ASTDAT.Data.Models.LoadModel>> MercuryGateGetLoads()
		{
			var userName = System.Configuration.ConfigurationManager.AppSettings["MercuryGateUserName"] ?? "dispatch@americanspecialized.org";
			var password = System.Configuration.ConfigurationManager.AppSettings["MercuryGatePassword"] ?? "Trucking1";

			var mg = new MercuryGate();
			var model = await mg.GetBids<ASTDAT.Data.Models.LoadModel>(userName, password);

			return model;
		}

		public void DeleteOlder30()
		{
			try
			{
				var db = new DBContext();
				var dt = DateTime.Now.AddDays(-30).Date;
				var loads = db.Loads.Where(x => x.CreateDate < dt || x.DateDatLoaded < dt).ToList();
				foreach (var load in loads)
				{
					if (!String.IsNullOrEmpty(load.AssetId) && !load.DateDatDeleted.HasValue) //not deleted from DAT
					{
						continue;
					}
					if (load.TrackStopId.HasValue && !load.DateTSDeleted.HasValue) //not deleted from TS
					{
						continue;
					}

					Logger.Write($"DeleteOlder30 {load.Id} {load.CreateDate}", fileName: "DeleteLoads.txt");

					db.LoadComments.RemoveRange(db.LoadComments.Where(x => x.LoadId == load.Id).ToList());
					db.Loads.Remove(load); //DeleteOlder30
					db.SaveChanges();

				}
			}
			catch (Exception exc)
			{
				Logger.Write($"DeleteOlder30", exc);
			}
		}

		private bool DoDATPostersReferenceIdUpdate(LoadModel load, DBContext db)
		{
			//delete from DAT
			var resultDelete = IntegrationService.Instance.Session.DeleteAssetsByIds(new string[] { load.AssetId }); //DATPostersReferenceIdUpdate
			if (resultDelete == null || resultDelete.deleteAssetResult == null || resultDelete.deleteAssetResult.Item is ServiceError)
			{
				load.AddComment($"DeleteAssetsByIds in DAT failed. Source:DATPostersReferenceIdUpdate");
				return false;
			}

			//post to DAT
			var resultPost = IntegrationService.Instance.Session.UploadDAT(new List<PostAssetOperation> { load.ToDATLoad() }); //DATPostersReferenceIdUpdate

			if (resultPost == null || resultPost.Response == null || resultPost.Response.postAssetResults == null || resultPost.Response.postAssetResults.Length == 0)
			{
				load.AddComment($"DoUploadLoad upload to DAT failed. Source:DATPostersReferenceIdUpdate");
				db.SaveChanges();
				return false;
			}
			else
			{
				foreach (var postAssetResult in resultPost.Response.postAssetResults)
				{
					var postAssetSuccessData = postAssetResult.Item as PostAssetSuccessData;
					if (postAssetSuccessData == null)
					{
						var serviceError = postAssetResult.Item as ServiceError;
						if (serviceError == null)
						{
							load.AddComment($"DoUploadLoad upload to DAT failed. Source:DATPostersReferenceIdUpdate");
						}
						else
						{
							load.AddComment($"DoUploadLoad upload to DAT failed, message:{serviceError.message}, detailedMessage:{serviceError.detailedMessage}. Source:DATPostersReferenceIdUpdate");
						}
					}
					else
					{
						load.AddComment($"Uploaded to DAT, AssetId:{postAssetSuccessData.assetId} Source:DATPostersReferenceIdUpdate");
						return true;
					}
				}
			}

			return false;
		}

		public List<Load> DATPostersReferenceIdUpdate()
		{
			Logger.Write("Start", fileName: "DATPostersReferenceIdUpdate.txt");

			var result = new List<Load>();

			using (var db = new DBContext())
			{
				var loads = db.Loads
					.Where(x => !x.DateDatDeleted.HasValue && x.AssetId != null && x.PostersReferenceId != null) //not delete, posted, has PostersReferenceId
					.ToList();

				foreach (var load in loads.Where(x => x.PostersReferenceId.Length == 7 || x.PostersReferenceId.Length == 8))
				{
					Logger.Write($"Start {load.Id} / {load.PostersReferenceId}", fileName: "DATPostersReferenceIdUpdate.txt");
					var repeatCounter = 4;

					try
					{
						var newId = "";
						if (load.PostersReferenceId.Length == 7)
						{
							newId = "8" + load.PostersReferenceId.Substring(2) + "aa";
						}
						else if (load.PostersReferenceId.Length == 8)
						{
							var s1 = load.PostersReferenceId[6];
							var s2 = load.PostersReferenceId[7];
							int i1 = Encoding.ASCII.GetBytes("" + s1)[0];
							int i2 = Encoding.ASCII.GetBytes("" + s2)[0];
							newId = load.PostersReferenceId.Substring(0, 6);
							if (i1 == 122 && i2 == 122) //zz - stop
							{
								newId = "9" + load.PostersReferenceId.Substring(1, 5) + "aa";
								if (load.PostersReferenceId[0] == '9')
								{
									newId = "";
								}
							}
							else if (i2 == 122) //.z
							{
								newId += Encoding.ASCII.GetString(new byte[] { Convert.ToByte(i1 + 1) });
								newId += "a";
							}
							else
							{
								newId += s1;
								newId += Encoding.ASCII.GetString(new byte[] { Convert.ToByte(i2 + 1) });
							}
						}

						if (String.IsNullOrEmpty(newId))
						{
							load.AddComment($"Can not generate new Ref Id {load.PostersReferenceId}");
						}
						else
						{
							Logger.Write($"{load.Id} / {load.PostersReferenceId} / {newId}", fileName: "DATPostersReferenceIdUpdate.txt");

							load.AddComment($"Ref Id '{load.PostersReferenceId}' changed to '{newId}'");
							load.PostersReferenceId = newId; //Set new PostersReferenceId
							db.SaveChanges();

							while (true)
							{
								var success = DoDATPostersReferenceIdUpdate(load, db);
								if (!success)
								{
									repeatCounter--;
									if (repeatCounter == 0)
									{
										break;
									}
									Logger.Write($"{load.Id} repeat ({repeatCounter})", fileName: "DATPostersReferenceIdUpdate.txt");
									System.Threading.Thread.Sleep(TimeSpan.FromMinutes(2));
									//System.Threading.Thread.Sleep(TimeSpan.FromSeconds(5));
								}
								if (success)
								{
									break;
								}
							}
						}
					}
					catch(Exception exc)
					{
						Logger.Write("DATPostersReferenceIdUpdate", exc, "DATPostersReferenceIdUpdate.txt");
					}
				}
			}

			return result;
		}
	}
}