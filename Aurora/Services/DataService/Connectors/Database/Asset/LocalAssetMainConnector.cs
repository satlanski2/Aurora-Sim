﻿using System;
using System.Data;
using System.Reflection;
using Aurora.Framework;
using Nini.Config;
using OpenMetaverse;
using OpenSim.Services.Interfaces;

namespace Aurora.Services.DataService.Connectors.Database.Asset
{
    public class LocalAssetMainConnector : IAssetDataPlugin
    {
        private IGenericData m_Gd;

        #region Implementation of IAuroraDataPlugin

        public string Name
        {
            get { return "IAssetDataPlugin"; }
        }

        public void Initialize(IGenericData genericData, IConfigSource source, IRegistryCore simBase,
                               string defaultConnectionString)
        {
            if (source.Configs["AuroraConnectors"].GetString("AssetConnector", "LocalConnector") != "LocalConnector")
                return;
            m_Gd = genericData;

            if (source.Configs[Name] != null)
                defaultConnectionString = source.Configs[Name].GetString("ConnectionString", defaultConnectionString);

            genericData.ConnectToDatabase(defaultConnectionString, "Asset",
                                          source.Configs["AuroraConnectors"].GetBoolean("ValidateTables", true));
            DataManager.DataManager.RegisterPlugin(this);
        }

        #endregion

        #region Implementation of IAssetDataPlugin

        public AssetBase GetAsset(UUID uuid)
        {
            return GetAsset(uuid, true);
        }

        public AssetBase GetMeta(UUID uuid)
        {
            IDataReader dr = null;
            try
            {
                dr = m_Gd.QueryData("where id = '" + uuid + "' LIMIT 1", "assets",
                                    "id, name, description, assetType, local, temporary, asset_flags, creatorID");
                while (dr.Read())
                {
                    return LoadAssetFromDataRead(dr);
                }
                MainConsole.Instance.Warn("[LocalAssetDatabase] GetMeta(" + uuid + ") - Asset " + uuid + " was not found.");
            }
            catch (Exception e)
            {
                MainConsole.Instance.Error("[LocalAssetDatabase]: Failed to fetch asset " + uuid + ", " + e);
            }
            finally
            {
                if (dr != null) dr.Close();
            }
            return null;
        }

        public UUID Store(AssetBase asset)
        {
            StoreAsset(asset);
            return asset.ID;
        }

        public bool StoreAsset(AssetBase asset)
        {
            try
            {
                if (asset.Name.Length > 64)
                    asset.Name = asset.Name.Substring(0, 64);
                if (asset.Description.Length > 128)
                    asset.Description = asset.Description.Substring(0, 128);
                if (ExistsAsset(asset.ID))
                {
                    AssetBase oldAsset = GetAsset(asset.ID);
                    if (oldAsset == null || (oldAsset.Flags & AssetFlags.Rewritable) == AssetFlags.Rewritable)
                    {
                        MainConsole.Instance.Debug("[LocalAssetDatabase]: Asset already exists in the db, overwriting - " + asset.ID);
                        Delete(asset.ID, true);
                        InsertAsset(asset, asset.ID);
                    }
                    else
                    {
                        MainConsole.Instance.Warn("[LocalAssetDatabase]: Asset already exists in the db, fixing ID... - " + asset.ID);
                        InsertAsset(asset, UUID.Random());
                    }
                }
                else
                {
                    InsertAsset(asset, asset.ID);
                }
            }
            catch (Exception e)
            {
                MainConsole.Instance.ErrorFormat("[LocalAssetDatabase]: Failure creating asset {0} with name \"{1}\". Error: {2}",
                                  asset.ID, asset.Name, e);
            }
            return true;
        }

        public void UpdateContent(UUID id, byte[] asset, out UUID newID)
        {
            newID = UUID.Zero;

            AssetBase oldAsset = GetAsset(id);
            if (oldAsset == null)
                return;

            if ((oldAsset.Flags & AssetFlags.Rewritable) == AssetFlags.Rewritable)
            {
                try
                {
                    m_Gd.Update("assets", new object[] { asset }, new[] { "data" }, new[] { "id" }, new object[] { id });
                }
                catch (Exception e)
                {
                    MainConsole.Instance.Error("[LocalAssetDatabase] UpdateContent(" + id + ") - Errored, " + e);
                }
            }
            else
            {
                newID = UUID.Random();
                oldAsset.Data = asset;
                InsertAsset(oldAsset, newID);
            }
        }

        private void InsertAsset(AssetBase asset, UUID assetID)
        {
            int now = (int)Utils.DateTimeToUnixTime(DateTime.UtcNow);
            m_Gd.Insert("assets",
                        new[]
                                    {
                                        "id", "name", "description", "assetType", "local", "temporary", "create_time",
                                        "access_time", "asset_flags", "creatorID", "data"
                                    },
                        new object[]
                                    {
                                        assetID, asset.Name.MySqlEscape(64), asset.Description.MySqlEscape(64),
                                        (sbyte) asset.TypeAsset, (asset.Flags & AssetFlags.Local) == AssetFlags.Local,
                                        (asset.Flags & AssetFlags.Temperary) == AssetFlags.Temperary, now, now,
                                        (int) asset.Flags, asset.CreatorID, asset.Data
                                    });
        }

        public bool ExistsAsset(UUID uuid)
        {
            try
            {
                QueryFilter filter = new QueryFilter();
                filter.andFilters["id"] = uuid;
                return m_Gd.Query(new string[] { "id" }, "assets", filter, null, null, null).Count > 0;
            }
            catch (Exception e)
            {
                MainConsole.Instance.ErrorFormat("[LocalAssetDatabase]: Failure fetching asset {0}" + Environment.NewLine + e, uuid);
            }
            return false;
        }

        public bool Delete(UUID id)
        {
            return Delete(id, false);
        }

        private AssetBase GetAsset(UUID uuid, bool displaywarning)
        {
            IDataReader dr = null;
            try
            {
                dr = m_Gd.QueryData("where id = '" + uuid + "'", "assets",
                                    "id, name, description, assetType, local, temporary, asset_flags, creatorID, data");
                while (dr != null && dr.Read())
                {
                    return LoadAssetFromDataRead(dr);
                }
                if (displaywarning)
                    MainConsole.Instance.Warn("[LocalAssetDatabase] GetAsset(" + uuid + ") - Asset " + uuid + " was not found.");
            }
            catch (Exception e)
            {
                MainConsole.Instance.Error("[LocalAssetDatabase]: Failed to fetch asset " + uuid + ", " + e);
            }
            finally
            {
                if (dr != null)
                    dr.Close();
            }
            return null;
        }

        public Byte[] GetData(UUID uuid)
        {
            IDataReader dr = null;
            try
            {
                dr = m_Gd.QueryData("where id = '" + uuid + "' LIMIT 1", "assets", "data");
                if (dr != null)
                    return (byte[]) dr["data"];
                MainConsole.Instance.Warn("[LocalAssetDatabase] GetData(" + uuid + ") - Asset " + uuid + " was not found.");
            }
            catch (Exception e)
            {
                MainConsole.Instance.Error("[LocalAssetDatabase]: Failed to fetch asset " + uuid + ", " + e);
            }
            finally
            {
                if (dr != null) dr.Close();
            }
            return null;
        }

        private bool Delete(UUID id, bool ignoreFlags)
        {
            try
            {
                if (!ignoreFlags)
                {
                    AssetBase asset = GetAsset(id, false);
                    if (asset == null)
                        return false;
                    if ((int) (asset.Flags & AssetFlags.Maptile) != 0 || //Depriated, use Deletable instead
                        (int) (asset.Flags & AssetFlags.Deletable) != 0)
                        ignoreFlags = true;
                }
                if (ignoreFlags)
                    m_Gd.Delete("assets", "id = '" + id + "'");
            }
            catch (Exception e)
            {
                MainConsole.Instance.Error("[LocalAssetDatabase] Error while deleting asset " + e);
            }
            return true;
        }

        private static AssetBase LoadAssetFromDataRead(IDataRecord dr)
        {
            AssetBase asset = new AssetBase(dr["id"].ToString())
                                  {
                                      Name = dr["name"].ToString(),
                                      Description = dr["description"].ToString()
                                  };
            string Flags = dr["asset_flags"].ToString();
            if (Flags != "")
                asset.Flags = (AssetFlags) int.Parse(Flags);
            string type = dr["assetType"].ToString();
            asset.TypeAsset = (AssetType) int.Parse(type);
            UUID creator;

            if (UUID.TryParse(dr["creatorID"].ToString(), out creator))
                asset.CreatorID = creator;
            try
            {
                object d = dr["data"];
                if ((d != null) && (d.ToString() != ""))
                {
                    asset.Data = (Byte[]) d;
                    asset.MetaOnly = false;
                }
                else
                {
                    asset.MetaOnly = true;
                    asset.Data = new byte[0];
                }
            }
            catch (Exception ex)
            {
                asset.MetaOnly = true;
                asset.Data = new byte[0];
                MainConsole.Instance.Error("[LocalAssetDatabase]: Failed to cast data for " + asset.ID + ", " + ex);
            }

            if (dr["local"].ToString().Equals("1") ||
                dr["local"].ToString().Equals("true", StringComparison.InvariantCultureIgnoreCase))
                asset.Flags |= AssetFlags.Local;
            string temp = dr["temporary"].ToString();
            if (temp != "")
            {
                bool tempbool = false;
                int tempint = 0;
                if (bool.TryParse(temp, out tempbool))
                {
                    if (tempbool)
                        asset.Flags |= AssetFlags.Temperary;
                }
                else if (int.TryParse(temp, out tempint))
                {
                    if (tempint == 1)
                        asset.Flags |= AssetFlags.Temperary;
                }
            }
            return asset;
        }

        #endregion
    }
}