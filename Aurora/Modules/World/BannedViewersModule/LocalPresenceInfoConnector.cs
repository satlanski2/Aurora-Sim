/*
 * Copyright 2011 Matthew Beardmore
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
using System.Reflection;
using System.Text;
using Nini.Config;
using OpenMetaverse;
using Aurora.DataManager;
using Aurora.Framework;

namespace Aurora.Modules.Ban
{
    public class LocalPresenceInfoConnector : IPresenceInfo, IAuroraDataPlugin
	{
        private IGenericData GD = null;
        private string DatabaseToAuthTable = "auth";

        public void Initialize(IGenericData GenericData, IConfigSource source, IRegistryCore registry, string DefaultConnectionString)
        {
            if (source.Configs["AuroraConnectors"].GetString("PresenceInfoConnector", "LocalConnector") == "LocalConnector")
            {
                GD = GenericData;

                if (source.Configs[Name] != null)
                {
                    DefaultConnectionString = source.Configs[Name].GetString("ConnectionString", DefaultConnectionString);
                    DatabaseToAuthTable = source.Configs[Name].GetString("DatabasePathToAuthTable", DatabaseToAuthTable);
                }
                GD.ConnectToDatabase(DefaultConnectionString, "PresenceInfo", true);
                DataManager.DataManager.RegisterPlugin(this);
            }
        }

        public string Name
        {
            get { return "IPresenceInfo"; }
        }

        public void Dispose()
        {
        }

        public PresenceInfo GetPresenceInfo(UUID agentID)
		{
            PresenceInfo agent = new PresenceInfo();
            Dictionary<string, object> where = new Dictionary<string, object>(1);
            where["AgentID"] = agentID;
            List<string> query = GD.Query(new string[] { "*" }, "baninfo", new QueryFilter
            {
                andFilters = where
            }, null, null, null);

            if (query.Count == 0) //Couldn't find it, return null then.
            {
                return null;
            }

            agent.AgentID = agentID;
            if (query[1] != "")
            {
                agent.Flags = (PresenceInfo.PresenceInfoFlags)Enum.Parse(typeof(PresenceInfo.PresenceInfoFlags), query[1]);
            }
            agent.KnownAlts = Util.ConvertToList(query[2]);
            agent.KnownID0s = Util.ConvertToList(query[3]);
            agent.KnownIPs = Util.ConvertToList(query[4]);
            agent.KnownMacs = Util.ConvertToList(query[5]);
            agent.KnownViewers = Util.ConvertToList(query[6]);
            agent.LastKnownID0 = query[7];
            agent.LastKnownIP = query[8];
            agent.LastKnownMac = query[9];
            agent.LastKnownViewer = query[10];
            agent.Platform = query[11];
            
			return agent;
		}

        public void UpdatePresenceInfo(PresenceInfo agent)
		{
			List<object> SetValues = new List<object>();
            List<string> SetRows = new List<string>();
            SetRows.Add("AgentID"/*"AgentID"*/);
            SetRows.Add("Flags"/*"Flags"*/);
            SetRows.Add("KnownAlts"/*"KnownAlts"*/);
            SetRows.Add("KnownID0s"/*"KnownID0s"*/);
            SetRows.Add("KnownIPs"/*"KnownIPs"*/);
            SetRows.Add("KnownMacs"/*"KnownMacs"*/);
            SetRows.Add("KnownViewers"/*"KnownViewers"*/);
            SetRows.Add("LastKnownID0"/*"LastKnownID0"*/);
            SetRows.Add("LastKnownIP"/*"LastKnownIP"*/);
            SetRows.Add("LastKnownMac"/*"LastKnownMac"*/);
            SetRows.Add("LastKnownViewer"/*"LastKnownViewer"*/);
            SetRows.Add("Platform"/*"Platform"*/);
            SetValues.Add(agent.AgentID);
            SetValues.Add(agent.Flags);
            SetValues.Add(Util.ConvertToString(agent.KnownAlts));
            SetValues.Add(Util.ConvertToString(agent.KnownID0s));
            SetValues.Add(Util.ConvertToString(agent.KnownIPs));
            SetValues.Add(Util.ConvertToString(agent.KnownMacs));
            SetValues.Add(Util.ConvertToString(agent.KnownViewers));
            SetValues.Add(agent.LastKnownID0);
            SetValues.Add(agent.LastKnownIP);
            SetValues.Add(agent.LastKnownMac);
            SetValues.Add(agent.LastKnownViewer);
            SetValues.Add(agent.Platform);
            GD.Replace("baninfo", SetRows.ToArray(), SetValues.ToArray());
        }

        public void Check(List<string> viewers, bool includeList)
        {
            List<string> query = GD.Query(new string[] { "AgentID" }, "baninfo", new QueryFilter(), null, null, null);
            foreach (string ID in query)
            {
                //Check all
                Check(GetPresenceInfo(UUID.Parse(ID)), viewers, includeList);
            }
        }

        public void Check (PresenceInfo info, List<string> viewers, bool includeList)
        {
            //
            //Check passwords
            //Check IPs, Mac's, etc
            //

            bool needsUpdated = false;

            #region Check Password

            Dictionary<string, object> where = new Dictionary<string, object>(1);
            where["UUID"] = info.AgentID;

            List<string> query = GD.Query(new string[] { "passwordHash" }, DatabaseToAuthTable, new QueryFilter
            {
                andFilters = where
            }, null, null, null);

            if (query.Count != 0)
            {
                where.Remove("UUID");
                where["passwordHash"] = query[0];
                query = GD.Query(new string[] { "UUID" }, DatabaseToAuthTable, new QueryFilter
                {
                    andFilters = where
                }, null, null, null);

                foreach (string ID in query)
                {
                    PresenceInfo suspectedInfo = GetPresenceInfo(UUID.Parse(ID));
                    if (suspectedInfo.AgentID == info.AgentID)
                    {
                        continue;
                    }

                    CoralateLists (info, suspectedInfo);

                    needsUpdated = true;
                }
            }

            #endregion

            #region Check ID0, IP, Mac, etc

            //Only check suspected and known offenders in this scan
            // 2 == Flags

            QueryFilter filter = new QueryFilter();
            filter.orMultiFilters["Flags"] = new List<object>(5);
            filter.orMultiFilters["Flags"].Add("SuspectedAltAccountOfKnown");
            filter.orMultiFilters["Flags"].Add("Known");
            filter.orMultiFilters["Flags"].Add("SuspectedAltAccountOfSuspected");
            filter.orMultiFilters["Flags"].Add("Banned");
            filter.orMultiFilters["Flags"].Add("Suspected");

            query = GD.Query(new string[1] { "AgentID" }, "baninfo", filter, null, null, null);

            foreach (string ID in query)
            {
                PresenceInfo suspectedInfo = GetPresenceInfo(UUID.Parse(ID));
                if (suspectedInfo.AgentID == info.AgentID)
                    continue;
                foreach (string ID0 in suspectedInfo.KnownID0s)
                {
                    if (info.KnownID0s.Contains(ID0))
                    {
                        CoralateLists (info, suspectedInfo);
                        needsUpdated = true;
                    }
                }
                foreach (string IP in suspectedInfo.KnownIPs)
                {
                    if (info.KnownIPs.Contains(IP.Split(':')[0]))
                    {
                        CoralateLists (info, suspectedInfo);
                        needsUpdated = true;
                    }
                }
                foreach (string Mac in suspectedInfo.KnownMacs)
                {
                    if (info.KnownMacs.Contains(Mac))
                    {
                        CoralateLists (info, suspectedInfo);
                        needsUpdated = true;
                    }
                }
            }

            foreach (string viewer in info.KnownViewers)
            {
                if (IsViewerBanned(viewer, includeList, viewers))
                {
                    if ((info.Flags & PresenceInfo.PresenceInfoFlags.Clean) == PresenceInfo.PresenceInfoFlags.Clean)
                    {
                        //Update them to suspected for their viewer
                        AddFlag (ref info, PresenceInfo.PresenceInfoFlags.Suspected);
                        //And update them later
                        needsUpdated = true;
                    }
                    else if ((info.Flags & PresenceInfo.PresenceInfoFlags.Suspected) == PresenceInfo.PresenceInfoFlags.Suspected)
                    {
                        //Suspected, we don't really want to move them higher than this...
                    }
                    else if ((info.Flags & PresenceInfo.PresenceInfoFlags.Known) == PresenceInfo.PresenceInfoFlags.Known)
                    {
                        //Known, can't update anymore
                    }
                }
            }
            if (DoGC(info) & !needsUpdated)//Clean up all info
                needsUpdated = true;

            #endregion

            //Now update ours
            if (needsUpdated)
                UpdatePresenceInfo(info);
        }

        public bool IsViewerBanned(string name, bool include, List<string> list)
        {
            if (include)
            {
                if (!list.Contains(name))
                    return true;
            }
            else
            {
                if (list.Contains(name))
                    return true;
            }
            return false;
        }

        private bool DoGC(PresenceInfo info)
        {
            bool update = false;
            List<string> newIPs = new List<string>();
            foreach (string ip in info.KnownIPs)
            {
                string[] split;
                string newIP = ip;
                if ((split = ip.Split(':')).Length > 1)
                {
                    //Remove the port if it exists and force an update
                    newIP = split[0];
                    update = true;
                }
                if (!newIPs.Contains(newIP))
                    newIPs.Add(newIP);
            }
            if (info.KnownIPs.Count != newIPs.Count)
                update = true;
            info.KnownIPs = newIPs;

            return update;
        }

        private void CoralateLists (PresenceInfo info, PresenceInfo suspectedInfo)
        {
            bool addedFlag = false;
            PresenceInfo.PresenceInfoFlags Flag = 0;

            if ((suspectedInfo.Flags & PresenceInfo.PresenceInfoFlags.Clean) == PresenceInfo.PresenceInfoFlags.Clean &&
                    (info.Flags & PresenceInfo.PresenceInfoFlags.Clean) == PresenceInfo.PresenceInfoFlags.Clean)
            {
                //They are both clean, do nothing
            }
            else if ((suspectedInfo.Flags & PresenceInfo.PresenceInfoFlags.Suspected) == PresenceInfo.PresenceInfoFlags.Suspected ||
                (info.Flags & PresenceInfo.PresenceInfoFlags.Suspected) == PresenceInfo.PresenceInfoFlags.Suspected)
            {
                //Suspected, update them both
                addedFlag = true;
                AddFlag (ref info, PresenceInfo.PresenceInfoFlags.Suspected);
                AddFlag (ref suspectedInfo, PresenceInfo.PresenceInfoFlags.Suspected);
            }
            else if ((suspectedInfo.Flags & PresenceInfo.PresenceInfoFlags.Known) == PresenceInfo.PresenceInfoFlags.Known ||
                (info.Flags & PresenceInfo.PresenceInfoFlags.Known) == PresenceInfo.PresenceInfoFlags.Known)
            {
                //Known, update them both
                addedFlag = true;
                AddFlag (ref info, PresenceInfo.PresenceInfoFlags.Known);
                AddFlag (ref suspectedInfo, PresenceInfo.PresenceInfoFlags.Known);
            }

            //Add the alt account flag
            AddFlag (ref info, PresenceInfo.PresenceInfoFlags.SuspectedAltAccount);
            AddFlag (ref suspectedInfo, PresenceInfo.PresenceInfoFlags.SuspectedAltAccount);

            if (suspectedInfo.Flags == PresenceInfo.PresenceInfoFlags.Suspected ||
                suspectedInfo.Flags == PresenceInfo.PresenceInfoFlags.SuspectedAltAccountOfSuspected ||
                info.Flags == PresenceInfo.PresenceInfoFlags.Suspected ||
                info.Flags == PresenceInfo.PresenceInfoFlags.SuspectedAltAccountOfSuspected)
            {
                //They might be an alt, but the other is clean, so don't bother them too much
                AddFlag (ref info, PresenceInfo.PresenceInfoFlags.SuspectedAltAccountOfSuspected);
                AddFlag (ref suspectedInfo, PresenceInfo.PresenceInfoFlags.SuspectedAltAccountOfSuspected);
            }
            else if (suspectedInfo.Flags == PresenceInfo.PresenceInfoFlags.Known ||
                suspectedInfo.Flags == PresenceInfo.PresenceInfoFlags.SuspectedAltAccountOfKnown ||
                info.Flags == PresenceInfo.PresenceInfoFlags.Known ||
                info.Flags == PresenceInfo.PresenceInfoFlags.SuspectedAltAccountOfKnown)
            {
                //Flag 'em
                AddFlag (ref info, PresenceInfo.PresenceInfoFlags.SuspectedAltAccountOfKnown);
                AddFlag (ref suspectedInfo, PresenceInfo.PresenceInfoFlags.SuspectedAltAccountOfKnown);
            }

            //Add each user to the list of alts, then add the lists of both together
            info.KnownAlts.Add (suspectedInfo.AgentID.ToString ());
            suspectedInfo.KnownAlts.Add (info.AgentID.ToString ());

            //Add the lists together
            List<string> alts = new List<string> ();
            foreach (string alt in info.KnownAlts)
            {
                if (!alts.Contains (alt))
                    alts.Add (alt);
            }
            foreach (string alt in suspectedInfo.KnownAlts)
            {
                if (!alts.Contains (alt))
                    alts.Add (alt);
            }

            //If we have added a flag, we need to update ALL alts as well
            if (addedFlag && alts.Count != 0)
            {
                foreach (string alt in alts)
                {
                    PresenceInfo altInfo = GetPresenceInfo (UUID.Parse (alt));
                    if (altInfo != null)
                    {
                        //Give them the flag as well
                        AddFlag (ref altInfo, Flag);

                        //Add the alt account flag
                        AddFlag (ref altInfo, PresenceInfo.PresenceInfoFlags.SuspectedAltAccount);

                        //Also give them the flags for alts
                        if (suspectedInfo.Flags == PresenceInfo.PresenceInfoFlags.Suspected ||
                            suspectedInfo.Flags == PresenceInfo.PresenceInfoFlags.SuspectedAltAccountOfSuspected ||
                            info.Flags == PresenceInfo.PresenceInfoFlags.Suspected ||
                            info.Flags == PresenceInfo.PresenceInfoFlags.SuspectedAltAccountOfSuspected)
                        {
                            //They might be an alt, but the other is clean, so don't bother them too much
                            AddFlag (ref altInfo, PresenceInfo.PresenceInfoFlags.SuspectedAltAccountOfSuspected);
                        }
                        else if (suspectedInfo.Flags == PresenceInfo.PresenceInfoFlags.Known ||
                            suspectedInfo.Flags == PresenceInfo.PresenceInfoFlags.SuspectedAltAccountOfKnown ||
                            info.Flags == PresenceInfo.PresenceInfoFlags.Known ||
                            info.Flags == PresenceInfo.PresenceInfoFlags.SuspectedAltAccountOfKnown)
                        {
                            //Flag 'em
                            AddFlag (ref altInfo, PresenceInfo.PresenceInfoFlags.SuspectedAltAccountOfKnown);
                        }

                        //And update them in the db
                        UpdatePresenceInfo (suspectedInfo);
                    }
                }
            }

            //Replace both lists now that they are merged
            info.KnownAlts = alts;
            suspectedInfo.KnownAlts = alts;

            //Update them, as we changed their info, we get updated below
            UpdatePresenceInfo (suspectedInfo);
        }

        private void AddFlag (ref PresenceInfo info, PresenceInfo.PresenceInfoFlags presenceInfoFlags)
        {
            if (presenceInfoFlags == 0)
                return;
            info.Flags &= PresenceInfo.PresenceInfoFlags.Clean; //Remove clean
            if (presenceInfoFlags == PresenceInfo.PresenceInfoFlags.Known)
                info.Flags &= PresenceInfo.PresenceInfoFlags.Clean; //Remove suspected as well
            info.Flags |= presenceInfoFlags; //Add the flag
        }
    }
}
