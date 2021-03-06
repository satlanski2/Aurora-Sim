/*
 * Copyright (c) Contributors, http://aurora-sim.org/
 * See CONTRIBUTORS.TXT for a full list of copyright holders.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *     * Redistributions of source code must retain the above copyright
 *       notice, this list of conditions and the following disclaimer.
 *     * Redistributions in binary form must reproduce the above copyright
 *       notice, this list of conditions and the following disclaimer in the
 *       documentation and/or other materials provided with the distribution.
 *     * Neither the name of the Aurora-Sim Project nor the
 *       names of its contributors may be used to endorse or promote products
 *       derived from this software without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE DEVELOPERS ``AS IS'' AND ANY
 * EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
 * WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
 * DISCLAIMED. IN NO EVENT SHALL THE CONTRIBUTORS BE LIABLE FOR ANY
 * DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
 * (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
 * ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
 * (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
 * SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;

using Nini.Config;

using Aurora.Framework;

using OpenMetaverse;
using OpenMetaverse.StructuredData;
using EventFlags = OpenMetaverse.DirectoryManager.EventFlags;

using OpenSim.Services.Interfaces;
using GridRegion = OpenSim.Services.Interfaces.GridRegion;

namespace Aurora.Services.DataService
{
    public class LocalDirectoryServiceConnector : IDirectoryServiceConnector
    {
        private IGenericData GD;
        private IRegistryCore m_registry;

        #region IDirectoryServiceConnector Members

        public void Initialize(IGenericData GenericData, IConfigSource source, IRegistryCore simBase, string defaultConnectionString)
        {
            GD = GenericData;
            m_registry = simBase;

            if (source.Configs[Name] != null)
                defaultConnectionString = source.Configs[Name].GetString("ConnectionString", defaultConnectionString);

            GD.ConnectToDatabase(defaultConnectionString, "Directory",
                                 source.Configs["AuroraConnectors"].GetBoolean("ValidateTables", true));

            DataManager.DataManager.RegisterPlugin(Name + "Local", this);

            if (source.Configs["AuroraConnectors"].GetString("DirectoryServiceConnector", "LocalConnector") ==
                "LocalConnector")
            {
                DataManager.DataManager.RegisterPlugin(this);
            }
        }

        public string Name
        {
            get { return "IDirectoryServiceConnector"; }
        }

        public void Dispose()
        {
        }

        #region Region

        /// <summary>
        ///   This also updates the parcel, not for just adding a new one
        /// </summary>
        /// <param name = "args"></param>
        /// <param name = "regionID"></param>
        /// <param name = "forSale"></param>
        /// <param name = "EstateID"></param>
        /// <param name = "showInSearch"></param>
        public void AddRegion(List<LandData> parcels)
        {
            if (parcels.Count == 0)
                return;

            ClearRegion(parcels[0].RegionID);
            List<object[]> insertValues = new List<object[]>();
#if (!ISWIN)
            foreach (LandData args in parcels)
            {
                List<object> Values = new List<object>
                                          {
                                              args.RegionID,
                                              args.GlobalID,
                                              args.LocalID,
                                              args.UserLocation.X,
                                              args.UserLocation.Y,
                                              args.UserLocation.Z,
                                              args.Name.MySqlEscape(50),
                                              args.Description.MySqlEscape(255),
                                              args.Flags,
                                              args.Dwell,
                                              args.InfoUUID,
                                              ((args.Flags & (uint) ParcelFlags.ForSale) == (uint) ParcelFlags.ForSale)
                                                  ? 1
                                                  : 0,
                                              args.SalePrice,
                                              args.AuctionID,
                                              args.Area,
                                              0,
                                              args.Maturity,
                                              args.OwnerID,
                                              args.GroupID,
                                              ((args.Flags & (uint) ParcelFlags.ShowDirectory) ==
                                               (uint) ParcelFlags.ShowDirectory)
                                                  ? 1
                                                  : 0,
                                              args.SnapshotID,
                                              OSDParser.SerializeLLSDXmlString(args.Bitmap),
                                              (int)args.Category
                                          };
                //InfoUUID is the missing 'real' Gridwide ParcelID

                insertValues.Add(Values.ToArray());
            }
#else
            foreach (List<object> Values in parcels.Select(args => new List<object>
                                                               {
                                                                   args.RegionID,
                                                                   args.GlobalID,
                                                                   args.LocalID,
                                                                   args.UserLocation.X,
                                                                   args.UserLocation.Y,
                                                                   args.UserLocation.Z,
                                                                   args.Name.MySqlEscape(50),
                                                                   args.Description.MySqlEscape(255),
                                                                   args.Flags,
                                                                   args.Dwell,
                                                                   args.InfoUUID,
                                                                   ((args.Flags & (uint) ParcelFlags.ForSale) == (uint) ParcelFlags.ForSale)
                                                                       ? 1
                                                                       : 0,
                                                                   args.SalePrice,
                                                                   args.AuctionID,
                                                                   args.Area,
                                                                   0,
                                                                   args.Maturity,
                                                                   args.OwnerID,
                                                                   args.GroupID,
                                                                   ((args.Flags & (uint) ParcelFlags.ShowDirectory) ==
                                                                    (uint) ParcelFlags.ShowDirectory)
                                                                       ? 1
                                                                       : 0,
                                                                   args.SnapshotID,
                                                                   OSDParser.SerializeLLSDXmlString(args.Bitmap),
                                                                   (int)args.Category
                                                               }))
            {
                //InfoUUID is the missing 'real' Gridwide ParcelID

                insertValues.Add(Values.ToArray());
            }
#endif
            GD.InsertMultiple("searchparcel", insertValues);
        }

        public void ClearRegion(UUID regionID)
        {
            GD.Delete("searchparcel", new string[1] {"RegionID"}, new object[1] {regionID});
        }

        #endregion

        #region Parcels

        private static List<LandData> Query2LandData(List<string> Query)
        {
            List<LandData> Lands = new List<LandData>();

            LandData LandData;

            for (int i = 0; i < Query.Count; i += 23)
            {
                LandData = new LandData();
                LandData.RegionID = UUID.Parse(Query[i]);
                LandData.GlobalID = UUID.Parse(Query[i + 1]);
                LandData.LocalID = int.Parse(Query[i + 2]);
                LandData.UserLocation = new Vector3(float.Parse(Query[i + 3]), float.Parse(Query[i + 4]),
                                                    float.Parse(Query[i + 5]));
                LandData.Name = Query[i + 6];
                LandData.Description = Query[i + 7];
                LandData.Flags = uint.Parse(Query[i + 8]);
                LandData.Dwell = int.Parse(Query[i + 9]);
                LandData.InfoUUID = UUID.Parse(Query[i + 10]);
                LandData.AuctionID = uint.Parse(Query[i + 13]);
                LandData.Area = int.Parse(Query[i + 14]);
                LandData.Maturity = int.Parse(Query[i + 16]);
                LandData.OwnerID = UUID.Parse(Query[i + 17]);
                LandData.GroupID = UUID.Parse(Query[i + 18]);
                LandData.SnapshotID = UUID.Parse(Query[i + 20]);
                try
                {
                    LandData.Bitmap = OSDParser.DeserializeLLSDXml(Query[i + 21]);
                }
                catch
                {
                }
                LandData.Category = (Query[i + 22] == string.Empty) ? ParcelCategory.None : (ParcelCategory)int.Parse(Query[i + 22]);

                Lands.Add(LandData);
            }
            return Lands;
        }

        /// <summary>
        ///   Gets a parcel from the search database by Info UUID (the true cross instance parcel ID)
        /// </summary>
        /// <param name = "ParcelID"></param>
        /// <returns></returns>
        public LandData GetParcelInfo(UUID InfoUUID)
        {
            //Split the InfoUUID so that we get the regions, we'll check for positions in a bit
            int RegionX, RegionY;
            uint X, Y;
            ulong RegionHandle;
            Util.ParseFakeParcelID(InfoUUID, out RegionHandle, out X, out Y);

            Util.UlongToInts(RegionHandle, out RegionX, out RegionY);

            GridRegion r = m_registry.RequestModuleInterface<IGridService>().GetRegionByPosition(UUID.Zero, RegionX,
                                                                                                 RegionY);
            if (r == null)
            {
//                m_log.Warn("[DirectoryService]: Could not find region for ParcelID: " + InfoUUID);
                return null;
            }
            //Get info about a specific parcel somewhere in the metaverse
            QueryFilter filter = new QueryFilter();
            filter.andFilters["RegionID"] = r.RegionID;
            List<string> Query = GD.Query(new string[] { "*" }, "searchparcel", filter, null, null, null);
            //Cant find it, return
            if (Query.Count == 0)
            {
                return null;
            }

            List<LandData> Lands = Query2LandData(Query);
            LandData LandData = null;

            bool[,] tempConvertMap = new bool[r.RegionSizeX/4,r.RegionSizeX/4];
            tempConvertMap.Initialize();
#if (!ISWIN)
            foreach (LandData land in Lands)
            {
                if (land.Bitmap != null)
                {
                    ConvertBytesToLandBitmap(ref tempConvertMap, land.Bitmap, r.RegionSizeX);
                    if (tempConvertMap[X/64, Y/64])
                    {
                        LandData = land;
                        break;
                    }
                }
            }
#else
            foreach (LandData land in Lands.Where(land => land.Bitmap != null))
            {
                ConvertBytesToLandBitmap(ref tempConvertMap, land.Bitmap, r.RegionSizeX);
                if (tempConvertMap[X/64, Y/64])
                {
                    LandData = land;
                    break;
                }
            }
#endif
            if (LandData == null && Lands.Count != 0)
                LandData = Lands[0];
            return LandData;
        }

        public LandData GetParcelInfo(UUID RegionID, UUID ScopeID, string ParcelName)
        {
            IRegionData regiondata = DataManager.DataManager.RequestPlugin<IRegionData>();
            if (regiondata != null)
            {
                GridRegion region = regiondata.Get(RegionID, ScopeID);
                if (region != null)
                {
                    UUID parcelInfoID = UUID.Zero;
                    QueryFilter filter = new QueryFilter();
                    filter.andFilters["Name"] = ParcelName;
                    filter.andFilters["RegionID"] = RegionID;

                    List<string> query = GD.Query(new string[1] { "InfoUUID" }, "searchparcel", filter, null, 0, 1);

                    if (query.Count >= 1 && UUID.TryParse(query[0], out parcelInfoID))
                    {
                        return GetParcelInfo(parcelInfoID);
                    }
                }
            }

            return null;
        }

        /// <summary>
        ///   Gets all parcels owned by the given user
        /// </summary>
        /// <param name = "OwnerID"></param>
        /// <returns></returns>
        public LandData[] GetParcelByOwner(UUID OwnerID)
        {
            //NOTE: this does check for group deeded land as well, so this can check for that as well
            QueryFilter filter = new QueryFilter();
            filter.andFilters["OwnerID"] = OwnerID;
            List<string> Query = GD.Query(new string[] { "*" }, "searchparcel", filter, null, null, null);

            return (Query.Count == 0) ? new LandData[0] { } : Query2LandData(Query).ToArray();
        }

        private static QueryFilter GetParcelsByRegionWhereClause(UUID RegionID, UUID scopeID, UUID owner, ParcelFlags flags, ParcelCategory category)
        {
            QueryFilter filter = new QueryFilter();
            filter.andFilters["RegionID"] = RegionID;

            if (owner != UUID.Zero)
            {
                filter.andFilters["OwnerID"] = owner;
            }

            if (flags != ParcelFlags.None)
            {
                filter.andBitfieldAndFilters["Flags"] = (uint)flags;
            }

            if (category != ParcelCategory.Any)
            {
                filter.andFilters["Category"] = (int)category;
            }

            return filter;
        }
        
        public List<LandData> GetParcelsByRegion(uint start, uint count, UUID RegionID, UUID scopeID, UUID owner, ParcelFlags flags, ParcelCategory category)
        {
            List<LandData> resp = new List<LandData>(0);
            if (count == 0)
            {
                return resp;
            }

            IRegionData regiondata = DataManager.DataManager.RequestPlugin<IRegionData>();
            if (regiondata != null)
            {
                GridRegion region = regiondata.Get(RegionID, scopeID);
                if (region != null)
                {
                    QueryFilter filter = GetParcelsByRegionWhereClause(RegionID, scopeID, owner, flags, category);
                    Dictionary<string, bool> sort = new Dictionary<string, bool>(1);
                    sort["OwnerID"] = false;
                    return Query2LandData(GD.Query(new string[1] { "*" }, "searchparcel", filter, sort, start, count));
                }
            }
            return resp;
        }

        public uint GetNumberOfParcelsByRegion(UUID RegionID, UUID scopeID, UUID owner, ParcelFlags flags, ParcelCategory category)
        {
            IRegionData regiondata = DataManager.DataManager.RequestPlugin<IRegionData>();
            if (regiondata != null)
            {
                GridRegion region = regiondata.Get(RegionID, scopeID);
                if (region != null)
                {
                    QueryFilter filter = GetParcelsByRegionWhereClause(RegionID, scopeID, owner, flags, category);
                    return uint.Parse(GD.Query(new string[1] { "COUNT(ParcelID)" }, "searchparcel", filter, null, null, null)[0]);
                }
            }
            return 0;
        }

        public List<LandData> GetParcelsWithNameByRegion(uint start, uint count, UUID RegionID, UUID ScopeID, string name)
        {
            List<LandData> resp = new List<LandData>(0);
            if (count == 0)
            {
                return resp;
            }

            IRegionData regiondata = DataManager.DataManager.RequestPlugin<IRegionData>();
            if (regiondata != null)
            {
                GridRegion region = regiondata.Get(RegionID, ScopeID);
                if (region != null)
                {
                    QueryFilter filter = new QueryFilter();
                    filter.andFilters["RegionID"] = RegionID;
                    filter.andFilters["Name"] = name;
                    
                    Dictionary<string, bool> sort = new Dictionary<string, bool>(1);
                    sort["OwnerID"] = false;

                    return Query2LandData(GD.Query(new string[1] { "*" }, "searchparcel", filter, sort, start, count));
                }
            }

            return resp;
        }

        public uint GetNumberOfParcelsWithNameByRegion(UUID RegionID, UUID ScopeID, string name)
        {
            IRegionData regiondata = DataManager.DataManager.RequestPlugin<IRegionData>();
            if (regiondata != null)
            {
                GridRegion region = regiondata.Get(RegionID, ScopeID);
                if (region != null)
                {
                    QueryFilter filter = new QueryFilter();
                    filter.andFilters["RegionID"] = RegionID;
                    filter.andFilters["Name"] = name;

                    return uint.Parse(GD.Query(new string[1] { "COUNT(ParcelID)" }, "searchparcel", filter, null, null, null)[0]);
                }
            }
            return 0;
        }

        /// <summary>
        ///   Searches for parcels around the grid
        /// </summary>
        /// <param name = "queryText"></param>
        /// <param name = "category"></param>
        /// <param name = "StartQuery"></param>
        /// <returns></returns>
        public DirPlacesReplyData[] FindLand(string queryText, string category, int StartQuery, uint Flags)
        {
            QueryFilter filter = new QueryFilter();
            Dictionary<string, bool> sort = new Dictionary<string, bool>();

            if (category != "-1")
            {
                filter.andFilters["Category"] = category;
            }

            //If they dwell sort flag is there, sort by dwell going down
            if ((Flags & (uint)DirectoryManager.DirFindFlags.DwellSort) == (uint)DirectoryManager.DirFindFlags.DwellSort)
            {
                sort["Dwell"] = false;
            }

            filter.orLikeFilters["Description"] = "%" + queryText + "%";
            filter.orLikeFilters["Name"] = "%" + queryText + "%";
            filter.andFilters["ShowInSearch"] = 1;

            List<string> retVal = GD.Query(new string[6]{
                "InfoUUID",
                "Name",
                "ForSale",
                "Auction",
                "Dwell",
                "Flags"
            }, "searchparcel", filter, sort, (uint)StartQuery, 50);

            if (retVal.Count == 0)
            {
                return new DirPlacesReplyData[0] { };
            }

            List<DirPlacesReplyData> Data = new List<DirPlacesReplyData>();

            for (int i = 0; i < retVal.Count; i += 6)
            {
                //Check to make sure we are sending the requested maturity levels
                if (!((int.Parse(retVal[i + 5]) & (int)ParcelFlags.MaturePublish) == (int)ParcelFlags.MaturePublish && ((Flags & (uint)DirectoryManager.DirFindFlags.IncludeMature)) == 0))
                {
                    Data.Add(new DirPlacesReplyData
                    {
                        parcelID = new UUID(retVal[i]),
                        name = retVal[i + 1],
                        forSale = int.Parse(retVal[i + 2]) == 1,
                        auction = retVal[i + 3] == "0", //Auction is stored as a 0 if there is no auction
                        dwell = float.Parse(retVal[i + 4])
                    });
                }
            }

            return Data.ToArray();
        }

        /// <summary>
        ///   Searches for parcels for sale around the grid
        /// </summary>
        /// <param name = "searchType">2 = Auction only, 8 = For Sale - Mainland, 16 = For Sale - Estate, 4294967295 = All</param>
        /// <param name = "price"></param>
        /// <param name = "area"></param>
        /// <param name = "StartQuery"></param>
        /// <returns></returns>
        public DirLandReplyData[] FindLandForSale(string searchType, uint price, uint area, int StartQuery, uint Flags)
        {

            QueryFilter filter = new QueryFilter();

            //They requested a sale price check
            if ((Flags & (uint)DirectoryManager.DirFindFlags.LimitByPrice) == (uint)DirectoryManager.DirFindFlags.LimitByPrice)
            {
                filter.andLessThanEqFilters["SalePrice"] = (int)price;
            }

            //They requested a 
            if ((Flags & (uint) DirectoryManager.DirFindFlags.LimitByArea) == (uint) DirectoryManager.DirFindFlags.LimitByArea)
            {
                filter.andGreaterThanEqFilters["Area"] = (int)area;
            }

            //Only parcels set for sale will be checked
            filter.andFilters["ForSale"] = "1";

            List<string> retVal = GD.Query(new string[]{
                "InfoUUID",
                "Name",
                "Auction",
                "SalePrice",
                "Area",
                "Flags"
            }, "searchparcel", filter, null, (uint)StartQuery, 50);

            //if there are none, return
            if (retVal.Count == 0)
            {
                return new DirLandReplyData[0] { };
            }

            List<DirLandReplyData> Data = new List<DirLandReplyData>();
            DirLandReplyData replyData;
            for (int i = 0; i < retVal.Count; i += 6)
            {
                replyData = new DirLandReplyData
                {
                    forSale = true,
                    parcelID = new UUID(retVal[i]),
                    name = retVal[i + 1],
                    auction = (retVal[i + 2] != "0")
                };
                //If its an auction and we didn't request to see auctions, skip to the next and continue
                if ((Flags & (uint)DirectoryManager.SearchTypeFlags.Auction) == (uint)DirectoryManager.SearchTypeFlags.Auction && !replyData.auction)
                {
                    continue;
                }

                replyData.salePrice = Convert.ToInt32(retVal[i + 3]);
                replyData.actualArea = Convert.ToInt32(retVal[i + 4]);

                //Check maturity levels depending on what flags the user has set
                //0 flag is an override so that we can get all lands for sale, regardless of maturity
                if (Flags == 0 || !((int.Parse(retVal[i + 5]) & (int)ParcelFlags.MaturePublish) == (int)ParcelFlags.MaturePublish && ((Flags & (uint)DirectoryManager.DirFindFlags.IncludeMature)) == 0))
                {
                    Data.Add(replyData);
                }
            }

            return Data.ToArray();
        }

        private void ConvertBytesToLandBitmap(ref bool[,] tempConvertMap, byte[] Bitmap, int sizeX)
        {
            try
            {
                byte tempByte = 0;
                int x = 0, y = 0, i = 0, bitNum = 0;
                int avg = (sizeX*sizeX/128);
                for (i = 0; i < avg; i++)
                {
                    tempByte = Bitmap[i];
                    for (bitNum = 0; bitNum < 8; bitNum++)
                    {
                        bool bit = Convert.ToBoolean(Convert.ToByte(tempByte >> bitNum) & 1);
                        tempConvertMap[x, y] = bit;
                        x++;
                        if (x > (sizeX/4) - 1)
                        {
                            x = 0;
                            y++;
                        }
                    }
                }
            }
            catch
            {
            }
        }

        #endregion

        #region Classifieds

        /// <summary>
        ///   Searches for classifieds
        /// </summary>
        /// <param name = "queryText"></param>
        /// <param name = "category"></param>
        /// <param name = "queryFlags"></param>
        /// <param name = "StartQuery"></param>
        /// <returns></returns>
        public DirClassifiedReplyData[] FindClassifieds(string queryText, string category, uint queryFlags, int StartQuery)
        {

            QueryFilter filter = new QueryFilter();

            if (int.Parse(category) != (int)DirectoryManager.ClassifiedCategories.Any) //Check the category
            {
                filter.andFilters["Category"] = category;
            }

            filter.andLikeFilters["Name"] = "%" + queryText + "%";

            List<string> retVal = GD.Query(new string[1] { "*" }, "userclassifieds", filter, null, (uint)StartQuery, 50);
            if (retVal.Count == 0)
            {
                return new DirClassifiedReplyData[0] { };
            }

            List<DirClassifiedReplyData> Data = new List<DirClassifiedReplyData>();
            DirClassifiedReplyData replyData;
            for (int i = 0; i < retVal.Count; i += 6)
            {
                //Pull the classified out of OSD
                Classified classified = new Classified();
                classified.FromOSD((OSDMap) OSDParser.DeserializeJson(retVal[i + 5]));

                replyData = new DirClassifiedReplyData
                {
                    classifiedFlags = classified.ClassifiedFlags,
                    classifiedID = classified.ClassifiedUUID,
                    creationDate = classified.CreationDate,
                    expirationDate = classified.ExpirationDate,
                    price = classified.PriceForListing,
                    name = classified.Name
                };
                //Check maturity levels
                if ((replyData.classifiedFlags & (uint)DirectoryManager.ClassifiedFlags.Mature) == (uint)DirectoryManager.ClassifiedFlags.Mature)
                {
                    if ((queryFlags & (uint)DirectoryManager.ClassifiedQueryFlags.Mature) == (uint)DirectoryManager.ClassifiedQueryFlags.Mature)
                    {
                        Data.Add(replyData);
                    }
                }
                else
                { //Its PG, add all
                    Data.Add(replyData);
                }
            }
            return Data.ToArray();
        }

        /// <summary>
        ///   Gets all classifieds in the given region
        /// </summary>
        /// <param name = "regionName"></param>
        /// <returns></returns>
        public Classified[] GetClassifiedsInRegion(string regionName)
        {
            QueryFilter filter = new QueryFilter();
            filter.andFilters["SimName"] = regionName;
            List<string> retVal = GD.Query(new string[] { "*" }, "userclassifieds", filter, null, null, null);

            if (retVal.Count == 0)
            {
                return new Classified[0] { };
            }

            List<Classified> Classifieds = new List<Classified>();
            Classified classified;
            for (int i = 0; i < retVal.Count; i += 6)
            {
                classified = new Classified();
                //Pull the classified out of OSD
                classified.FromOSD((OSDMap) OSDParser.DeserializeJson(retVal[i + 5]));
                Classifieds.Add(classified);
            }
            return Classifieds.ToArray();
        }

        #endregion

        #region Events

        /// <summary>
        ///   Searches for events with the given parameters
        /// </summary>
        /// <param name = "queryText"></param>
        /// <param name = "flags"></param>
        /// <param name = "StartQuery"></param>
        /// <returns></returns>
        public DirEventsReplyData[] FindEvents(string queryText, uint eventFlags, int StartQuery)
        {
            List<DirEventsReplyData> Data = new List<DirEventsReplyData>();

            QueryFilter filter = new QueryFilter();

            //|0| means search between some days
            if (queryText.Contains("|0|"))
            {
                string StringDay = queryText.Split('|')[0];
                if (StringDay == "u") //"u" means search for events that are going on today
                {
                    filter.andGreaterThanEqFilters["UNIX_TIMESTAMP(date)"] = Util.ToUnixTime(DateTime.Today);
                }
                else
                {
                    //Pull the day out then and search for that many days in the future/past
                    int Day = int.Parse(StringDay);
                    DateTime SearchedDay = DateTime.Today.AddDays(Day);
                    //We only look at one day at a time
                    DateTime NextDay = SearchedDay.AddDays(1);
                    filter.andGreaterThanEqFilters["UNIX_TIMESTAMP(date)"] = Util.ToUnixTime(SearchedDay);
                    filter.andLessThanEqFilters["UNIX_TIMESTAMP(date)"] = Util.ToUnixTime(NextDay);
                    filter.andLessThanEqFilters["flags"] = (int)eventFlags;
                }
            }
            else
            {
                filter.andLikeFilters["name"] = "%" + queryText + "%";
            }

            List<string> retVal = GD.Query(new string[]{
                "EID",
                "creator",
                "date",
                "maturity",
                "flags",
                "name"
            }, "asevents", filter, null, (uint)StartQuery, 50);

            if (retVal.Count > 0)
            {
                DirEventsReplyData replyData;
                for (int i = 0; i < retVal.Count; i += 6)
                {
                    replyData = new DirEventsReplyData
                    {
                        eventID = Convert.ToUInt32(retVal[i]),
                        ownerID = new UUID(retVal[i + 1]),
                        name = retVal[i + 5],
                    };
                    DateTime date = DateTime.Parse(retVal[i + 2].ToString());
                    replyData.date = date.ToString(new DateTimeFormatInfo());
                    replyData.unixTime = (uint)Util.ToUnixTime(date);
                    replyData.eventFlags = Convert.ToUInt32(retVal[i + 4]);

                    //Check the maturity levels
                    uint maturity = Convert.ToUInt32(retVal[i + 3]);
                    if(
                            (maturity == 0 && (eventFlags & (uint)DirectoryManager.EventFlags.PG) == (uint)DirectoryManager.EventFlags.PG) ||
                            (maturity == 1 && (eventFlags & (uint)DirectoryManager.EventFlags.Mature) == (uint)DirectoryManager.EventFlags.Mature) ||
                            (maturity == 2 && (eventFlags & (uint)DirectoryManager.EventFlags.Adult) == (uint)DirectoryManager.EventFlags.Adult)
                    )
                    {
                        Data.Add(replyData);
                    }
                }
            }

            return Data.ToArray();
        }

        /// <summary>
        ///   Retrives all events in the given region by their maturity level
        /// </summary>
        /// <param name = "regionName"></param>
        /// <param name = "maturity">Uses DirectoryManager.EventFlags to determine the maturity requested</param>
        /// <returns></returns>
        public DirEventsReplyData[] FindAllEventsInRegion(string regionName, int maturity)
        {
            List<DirEventsReplyData> Data = new List<DirEventsReplyData>();

            IRegionData regiondata = Aurora.DataManager.DataManager.RequestPlugin<IRegionData>();
            if (regiondata != null)
            {
                List<GridRegion> regions = regiondata.Get(regionName, UUID.Zero);
                if (regions.Count >= 1)
                {
                    Dictionary<string, object> whereClause = new Dictionary<string, object>();
                    whereClause["region"] = regions[0].RegionID.ToString();
                    whereClause["maturity"] = maturity;

                    List<string> retVal = GD.Query(new string[]{
                        "EID",
                        "creator",
                        "date",
                        "maturity",
                        "flags",
                        "name"
                    }, "asevents", new QueryFilter
                    {
                        andFilters = whereClause
                    }, null, null, null);

                    if (retVal.Count > 0)
                    {
                        DirEventsReplyData replyData;
                        for (int i = 0; i < retVal.Count; i += 6)
                        {
                            replyData = new DirEventsReplyData
                            {
                                eventID = Convert.ToUInt32(retVal[i]),
                                ownerID = new UUID(retVal[i + 1]),
                                name = retVal[i + 5],
                            };
                            DateTime date = DateTime.Parse(retVal[i + 2].ToString());
                            replyData.date = date.ToString(new DateTimeFormatInfo());
                            replyData.unixTime = (uint)Util.ToUnixTime(date);
                            replyData.eventFlags = Convert.ToUInt32(retVal[i + 4]);

                            Data.Add(replyData);
                        }
                    }
                }
            }

            return Data.ToArray();
        }
        
        private static List<EventData> Query2EventData(List<string> RetVal){
            List<EventData> Events = new List<EventData>();
            IRegionData regiondata = Aurora.DataManager.DataManager.RequestPlugin<IRegionData>();
            if (RetVal.Count % 15 != 0 || regiondata == null)
            {
                return Events;
            }

            GridRegion region;
            EventData data;
            
            for (int i = 0; i < RetVal.Count; i += 15)
            {
                data = new EventData();

                region = regiondata.Get(UUID.Parse(RetVal[2].ToString()), UUID.Zero);
                if (region == null)
                {
                    continue;
                }
                data.simName = region.RegionName;

                data.eventID = Convert.ToUInt32(RetVal[i]);
                data.creator = RetVal[i + 1];

                //Parse the time out for the viewer
                DateTime date = DateTime.Parse(RetVal[i + 4].ToString());
                data.date = date.ToString(new DateTimeFormatInfo());
                data.dateUTC = (uint)Util.ToUnixTime(date);

                data.cover = data.amount = Convert.ToUInt32(RetVal[i + 5]);
                data.maturity = Convert.ToInt32(RetVal[i + 6]);
                data.eventFlags = Convert.ToUInt32(RetVal[i + 7]);
                data.duration = Convert.ToUInt32(RetVal[i + 8]);

                data.globalPos = new Vector3(
                        region.RegionLocX + float.Parse(RetVal[i + 9]),
                        region.RegionLocY + float.Parse(RetVal[i + 10]),
                        region.RegionLocZ + float.Parse(RetVal[i + 11])
                );

                data.name = RetVal[i + 12];
                data.description = RetVal[i + 13];
                data.category = RetVal[i + 14];

                Events.Add(data);
            }

            return Events;
        }

        /// <summary>
        ///   Gets more info about the event by the events unique event ID
        /// </summary>
        /// <param name = "EventID"></param>
        /// <returns></returns>
        public EventData GetEventInfo(uint EventID)
        {
            QueryFilter filter = new QueryFilter();
            filter.andFilters["EID"] = EventID;
            List<string> RetVal = GD.Query(new string[] { "*" }, "asevents", filter, null, null, null);
            return (RetVal.Count == 0) ? null : Query2EventData(RetVal)[0];
        }

        public EventData CreateEvent(UUID creator, UUID regionID, UUID parcelID, DateTime date, uint cover, EventFlags maturity, uint flags, uint duration, Vector3 localPos, string name, string description, string category)
        {
            IRegionData regiondata = Aurora.DataManager.DataManager.RequestPlugin<IRegionData>();
            IParcelServiceConnector parceldata = Aurora.DataManager.DataManager.RequestPlugin<IParcelServiceConnector>();
            if(regiondata == null || parceldata == null){
                return null;
            }

            GridRegion region = regiondata.Get(regionID, UUID.Zero);
            if(region == null){
                return null;
            }
            if (parcelID != UUID.Zero)
            {
                LandData parcel = parceldata.GetLandData(region.RegionID, parcelID);
                if (parcel == null)
                {
                    return null;
                }
            }


            EventData eventData = new EventData();
            eventData.eventID = GetMaxEventID() + 1;
            eventData.creator = creator.ToString();
            eventData.simName = region.RegionName;
            eventData.date = date.ToString(new DateTimeFormatInfo());
            eventData.dateUTC = (uint)Util.ToUnixTime(date);
            eventData.cover = eventData.amount = cover;
            eventData.maturity = (int)maturity;
            eventData.eventFlags = flags | (uint)maturity;
            eventData.duration = duration;
            eventData.globalPos = new Vector3(
                region.RegionLocX + localPos.X,
                region.RegionLocY + localPos.Y,
                region.RegionLocZ + localPos.Z
            );
            eventData.name = name;
            eventData.description = description;
            eventData.category = category;

            GD.Insert("asevents", new string[]{
                "EID",
                "creator",
                "region",
                "parcel", 
                "date", 
                "cover", 
                "maturity", 
                "flags", 
                "duration", 
                "localPosX", 
                "localPosY", 
                "localPosZ", 
                "name",
                "description",
                "category"
            }, new object[]{
                eventData.eventID,
                creator.ToString(),
                regionID.ToString(),
                parcelID.ToString(),
                date.ToString("s"),
                eventData.cover,
                (uint)maturity,
                flags,
                duration,
                localPos.X,
                localPos.Y,
                localPos.Z,
                name,
                description,
                category
            });

            return eventData;
        }

        public List<EventData> GetEvents(uint start, uint count, Dictionary<string, bool> sort, Dictionary<string, object> filter)
        {
            return (count == 0) ? new List<EventData>(0) : Query2EventData(GD.Query(new string[]{ "*" }, "asevents", new QueryFilter{
                andFilters = filter
            }, sort, start, count ));
        }

        public uint GetNumberOfEvents(Dictionary<string, object> filter)
        {
            return uint.Parse(GD.Query(new string[1]{
                "COUNT(EID)"
            }, "asevents", new QueryFilter{
                andFilters = filter
            }, null, null, null)[0]);
        }

        public uint GetMaxEventID()
        {
            if (GetNumberOfEvents(new Dictionary<string, object>(0)) == 0)
            {
                return 0;
            }
            else
            {
                return uint.Parse(GD.Query(new string[1] { "MAX(EID)" }, "asevents", null, null, null, null)[0]);
            }
        }

        #endregion

        #endregion
    }
}