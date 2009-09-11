/*
 * Copyright (c) Contributors, http://opensimulator.org/
 * See CONTRIBUTORS.TXT for a full list of copyright holders.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *     * Redistributions of source code must retain the above copyright
 *       notice, this list of conditions and the following disclaimer.
 *     * Redistributions in binary form must reproduce the above copyright
 *       notice, this list of conditions and the following disclaimer in the
 *       documentation and/or other materials provided with the distribution.
 *     * Neither the name of the OpenSimulator Project nor the
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

namespace OpenSim.Framework
{
    public class AuthorizationRequest
    {
        private string m_userID;
        private string m_firstname;
        private string m_surname;
        private string m_email;
        private string m_regionName;
        private string m_regionID;

        public AuthorizationRequest()
        {
        }

        public AuthorizationRequest(string ID, string RegionID)
        {
            m_userID = ID;
            m_regionID = RegionID;
        }
        
        public AuthorizationRequest(string ID,string FirstName, string SurName, string Email, string RegionName, string RegionID)
        {
            m_userID = ID;
            m_firstname = FirstName;
            m_surname = SurName;
            m_email = Email;
            m_regionName = RegionName;
            m_regionID = RegionID;
        }
        
        public string ID
        {
            get { return m_userID; }
            set { m_userID = value; }
        }
        
        public string FirstName
        {
            get { return m_firstname; }
            set { m_firstname = value; }
        }
        
        public string SurName
        {
            get { return m_surname; }
            set { m_surname = value; }
        }
        
        public string Email
        {
            get { return m_email; }
            set { m_email = value; }
        }
        
        public string RegionName
        {
            get { return m_regionName; }
            set { m_regionName = value; }
        }
                        
        public string RegionID
        {
            get { return m_regionID; }
            set { m_regionID = value; }
        }
        
        
        
    }
}