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

using System.Collections.Generic;
using Aurora.Framework;
using Aurora.Services.DataService;
using Nini.Config;
using OpenMetaverse;

namespace Aurora.Modules
{
    public class IWCProfileConnector : IProfileConnector
    {
        protected LocalProfileConnector m_localService;

        private IRegistryCore m_registry;
        protected RemoteProfileConnector m_remoteService;

        #region IProfileConnector Members

        public void Initialize(IGenericData unneeded, IConfigSource source, IRegistryCore simBase,
                               string defaultConnectionString)
        {
            if (source.Configs["AuroraConnectors"].GetString("ProfileConnector", "LocalConnector") == "IWCConnector")
            {
                m_localService = new LocalProfileConnector();
                m_localService.Initialize(unneeded, source, simBase, defaultConnectionString);
                m_remoteService = new RemoteProfileConnector();
                m_remoteService.Initialize(unneeded, source, simBase, defaultConnectionString);
                m_registry = simBase;
                DataManager.DataManager.RegisterPlugin(this);
            }
        }

        public string Name
        {
            get { return "IProfileConnector"; }
        }

        public IUserProfileInfo GetUserProfile(UUID agentID)
        {
            IUserProfileInfo profile = m_localService.GetUserProfile(agentID);
            if (profile == null)
                profile = m_remoteService.GetUserProfile(agentID);
            return profile;
        }

        public bool UpdateUserProfile(IUserProfileInfo Profile)
        {
            bool success = m_localService.UpdateUserProfile(Profile);
            if (!success)
                success = m_remoteService.UpdateUserProfile(Profile);
            return success;
        }

        public void CreateNewProfile(UUID UUID)
        {
            m_localService.CreateNewProfile(UUID);
        }

        public bool AddClassified(Classified classified)
        {
            bool success = m_localService.AddClassified(classified);
            if (!success)
                success = m_remoteService.AddClassified(classified);
            return success;
        }

        public Classified GetClassified(UUID queryClassifiedID)
        {
            Classified Classified = m_localService.GetClassified(queryClassifiedID);
            if (Classified == null)
                Classified = m_remoteService.GetClassified(queryClassifiedID);
            return Classified;
        }

        public List<Classified> GetClassifieds(UUID ownerID)
        {
            List<Classified> Classifieds = m_localService.GetClassifieds(ownerID);
            if (Classifieds == null)
                Classifieds = m_remoteService.GetClassifieds(ownerID);
            return Classifieds;
        }

        public void RemoveClassified(UUID queryClassifiedID)
        {
            m_localService.RemoveClassified(queryClassifiedID);
            m_remoteService.RemoveClassified(queryClassifiedID);
        }

        public bool AddPick(ProfilePickInfo pick)
        {
            bool success = m_localService.AddPick(pick);
            if (!success)
                success = m_remoteService.AddPick(pick);
            return success;
        }

        public ProfilePickInfo GetPick(UUID queryPickID)
        {
            ProfilePickInfo pick = m_localService.GetPick(queryPickID);
            if (pick == null)
                pick = m_remoteService.GetPick(queryPickID);
            return pick;
        }

        public List<ProfilePickInfo> GetPicks(UUID ownerID)
        {
            List<ProfilePickInfo> picks = m_localService.GetPicks(ownerID);
            if (picks == null)
                picks = m_remoteService.GetPicks(ownerID);
            return picks;
        }

        public void RemovePick(UUID queryPickID)
        {
            m_localService.RemovePick(queryPickID);
            m_remoteService.RemovePick(queryPickID);
        }

        #endregion

        public void Dispose()
        {
        }
    }
}