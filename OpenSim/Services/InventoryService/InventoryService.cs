/*
 * Copyright (c) Contributors, http://aurora-sim.org/, http://opensimulator.org/
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
using System.Linq;
using System.Reflection;
using Aurora.DataManager;
using Aurora.Simulation.Base;
using Nini.Config;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using Aurora.Framework;
using OpenSim.Services.Interfaces;

namespace OpenSim.Services.InventoryService
{
    public class InventoryService : IInventoryService, IService
    {
        protected bool m_AllowDelete = true;

        protected IAssetService m_AssetService;
        protected IInventoryData m_Database;
        protected ILibraryService m_LibraryService;
        protected IUserAccountService m_UserAccountService;

        public virtual string Name
        {
            get { return GetType().Name; }
        }

        #region IInventoryService Members

        public virtual bool CreateUserRootFolder(UUID principalID)
        {
            bool result = false;

            InventoryFolderBase rootFolder = GetRootFolder(principalID);

            if (rootFolder == null)
            {
                List<InventoryFolderBase> rootFolders = GetInventorySkeleton(principalID);
                if (rootFolders.Count == 0)
                    rootFolder = CreateFolder(principalID, UUID.Zero, (int) AssetType.RootFolder, "My Inventory");
                else
                {
                    rootFolder = new InventoryFolderBase
                                     {
                                         Name = "My Inventory",
                                         Type = (short) AssetType.RootFolder,
                                         Version = 1,
                                         ID = rootFolders[0].ParentID,
                                         Owner = principalID,
                                         ParentID = UUID.Zero
                                     };


                    m_Database.StoreFolder(rootFolder);
                }
                result = true;
            }
            return result;
        }

        public virtual bool CreateUserInventory(UUID principalID, bool createDefaultItems)
        {
            // This is braindeaad. We can't ever communicate that we fixed
            // an existing inventory. Well, just return root folder status,
            // but check sanity anyway.
            //
            bool result = false;

            InventoryFolderBase rootFolder = GetRootFolder(principalID);

            if (rootFolder == null)
            {
                rootFolder = CreateFolder(principalID, UUID.Zero, (int) AssetType.RootFolder, "My Inventory");
                result = true;
            }

            InventoryFolderBase[] sysFolders = GetSystemFolders(principalID);

            if (!Array.Exists(sysFolders, delegate(InventoryFolderBase f)
                                              {
                                                  if (f.Type == (short) AssetType.Animation) return true;
                                                  return false;
                                              }))
                CreateFolder(principalID, rootFolder.ID, (int) AssetType.Animation, "Animations");
            if (!Array.Exists(sysFolders, delegate(InventoryFolderBase f)
                                              {
                                                  if (f.Type == (short) AssetType.Bodypart) return true;
                                                  return false;
                                              }))
                CreateFolder(principalID, rootFolder.ID, (int) AssetType.Bodypart, "Body Parts");
            if (!Array.Exists(sysFolders, delegate(InventoryFolderBase f)
                                              {
                                                  if (f.Type == (short) AssetType.CallingCard) return true;
                                                  return false;
                                              }))
                CreateFolder(principalID, rootFolder.ID, (int) AssetType.CallingCard, "Calling Cards");
            if (!Array.Exists(sysFolders, delegate(InventoryFolderBase f)
                                              {
                                                  if (f.Type == (short) AssetType.Clothing) return true;
                                                  return false;
                                              }))
                CreateFolder(principalID, rootFolder.ID, (int) AssetType.Clothing, "Clothing");
            if (!Array.Exists(sysFolders, delegate(InventoryFolderBase f)
                                              {
                                                  if (f.Type == (short) AssetType.Gesture) return true;
                                                  return false;
                                              }))
                CreateFolder(principalID, rootFolder.ID, (int) AssetType.Gesture, "Gestures");
            if (!Array.Exists(sysFolders, delegate(InventoryFolderBase f)
                                              {
                                                  if (f.Type == (short) AssetType.Landmark) return true;
                                                  return false;
                                              }))
                CreateFolder(principalID, rootFolder.ID, (int) AssetType.Landmark, "Landmarks");
            if (!Array.Exists(sysFolders, delegate(InventoryFolderBase f)
                                              {
                                                  if (f.Type == (short) AssetType.LostAndFoundFolder) return true;
                                                  return false;
                                              }))
                CreateFolder(principalID, rootFolder.ID, (int) AssetType.LostAndFoundFolder, "Lost And Found");
            if (!Array.Exists(sysFolders, delegate(InventoryFolderBase f)
                                              {
                                                  if (f.Type == (short) AssetType.Notecard) return true;
                                                  return false;
                                              }))
                CreateFolder(principalID, rootFolder.ID, (int) AssetType.Notecard, "Notecards");
            if (!Array.Exists(sysFolders, delegate(InventoryFolderBase f)
                                              {
                                                  if (f.Type == (short) AssetType.Object) return true;
                                                  return false;
                                              }))
                CreateFolder(principalID, rootFolder.ID, (int) AssetType.Object, "Objects");
            if (!Array.Exists(sysFolders, delegate(InventoryFolderBase f)
                                              {
                                                  if (f.Type == (short) AssetType.SnapshotFolder) return true;
                                                  return false;
                                              }))
                CreateFolder(principalID, rootFolder.ID, (int) AssetType.SnapshotFolder, "Photo Album");
            if (!Array.Exists(sysFolders, delegate(InventoryFolderBase f)
                                              {
                                                  if (f.Type == (short) AssetType.LSLText) return true;
                                                  return false;
                                              }))
                CreateFolder(principalID, rootFolder.ID, (int) AssetType.LSLText, "Scripts");
            if (!Array.Exists(sysFolders, delegate(InventoryFolderBase f)
                                              {
                                                  if (f.Type == (short) AssetType.Sound) return true;
                                                  return false;
                                              }))
                CreateFolder(principalID, rootFolder.ID, (int) AssetType.Sound, "Sounds");
            if (!Array.Exists(sysFolders, delegate(InventoryFolderBase f)
                                              {
                                                  if (f.Type == (short) AssetType.Texture) return true;
                                                  return false;
                                              }))
                CreateFolder(principalID, rootFolder.ID, (int) AssetType.Texture, "Textures");
            if (!Array.Exists(sysFolders, delegate(InventoryFolderBase f)
                                              {
                                                  if (f.Type == (short) AssetType.TrashFolder) return true;
                                                  return false;
                                              }))
                CreateFolder(principalID, rootFolder.ID, (int) AssetType.TrashFolder, "Trash");

            if (!Array.Exists(sysFolders, delegate(InventoryFolderBase f)
                                              {
                                                  if (f.Type == (short) AssetType.Mesh) return true;
                                                  return false;
                                              }))
                CreateFolder(principalID, rootFolder.ID, (int) AssetType.Mesh, "Mesh");

            if (createDefaultItems && m_LibraryService != null)
            {
                InventoryFolderBase bodypartFolder = GetFolderForType(principalID, InventoryType.Unknown,
                                                                      AssetType.Bodypart);
                InventoryFolderBase clothingFolder = GetFolderForType(principalID, InventoryType.Unknown,
                                                                      AssetType.Clothing);

                // Default items
                InventoryItemBase defaultShape = new InventoryItemBase
                                                     {
                                                         Name = "Default shape",
                                                         Description = "Default shape description",
                                                         AssetType = (int) AssetType.Bodypart,
                                                         InvType = (int) InventoryType.Wearable,
                                                         Flags = (uint) WearableType.Shape,
                                                         ID = AvatarWearable.DEFAULT_BODY_ITEM
                                                     };
                //Give a new copy to every person
                AssetBase asset = m_AssetService.Get(AvatarWearable.DEFAULT_BODY_ASSET.ToString());
                if (asset != null)
                {
                    asset.ID = UUID.Random();
                    asset.ID = m_AssetService.Store(asset);
                    defaultShape.AssetID = asset.ID;
                    defaultShape.Folder = bodypartFolder.ID;
                    defaultShape.CreatorId = UUID.Zero.ToString();
                    AddItem(defaultShape, false);
                }

                InventoryItemBase defaultSkin = new InventoryItemBase
                                                    {
                                                        Name = "Default skin",
                                                        Description = "Default skin description",
                                                        AssetType = (int) AssetType.Bodypart,
                                                        InvType = (int) InventoryType.Wearable,
                                                        Flags = (uint) WearableType.Skin,
                                                        ID = AvatarWearable.DEFAULT_SKIN_ITEM
                                                    };
                //Give a new copy to every person
                asset = m_AssetService.Get(AvatarWearable.DEFAULT_SKIN_ASSET.ToString());
                if (asset != null)
                {
                    asset.ID = UUID.Random();
                    asset.ID = m_AssetService.Store(asset);
                    defaultSkin.AssetID = asset.ID;
                    defaultSkin.Folder = bodypartFolder.ID;
                    defaultSkin.CreatorId = m_LibraryService.LibraryOwner.ToString();
                    defaultSkin.Owner = principalID;
                    defaultSkin.BasePermissions = (uint) PermissionMask.All;
                    defaultSkin.CurrentPermissions = (uint) PermissionMask.All;
                    defaultSkin.EveryOnePermissions = (uint) PermissionMask.None;
                    defaultSkin.NextPermissions = (uint) PermissionMask.All;
                    AddItem(defaultSkin, false);
                }

                InventoryItemBase defaultHair = new InventoryItemBase
                                                    {
                                                        Name = "Default hair",
                                                        Description = "Default hair description",
                                                        AssetType = (int) AssetType.Bodypart,
                                                        InvType = (int) InventoryType.Wearable,
                                                        Flags = (uint) WearableType.Hair,
                                                        ID = AvatarWearable.DEFAULT_HAIR_ITEM
                                                    };
                //Give a new copy to every person
                asset = m_AssetService.Get(AvatarWearable.DEFAULT_HAIR_ASSET.ToString());
                if (asset != null)
                {
                    asset.ID = UUID.Random();
                    asset.ID = m_AssetService.Store(asset);
                    defaultHair.AssetID = asset.ID;
                    defaultHair.Folder = bodypartFolder.ID;
                    defaultHair.CreatorId = m_LibraryService.LibraryOwner.ToString();
                    defaultHair.Owner = principalID;
                    defaultHair.BasePermissions = (uint) PermissionMask.All;
                    defaultHair.CurrentPermissions = (uint) PermissionMask.All;
                    defaultHair.EveryOnePermissions = (uint) PermissionMask.None;
                    defaultHair.NextPermissions = (uint) PermissionMask.All;
                    AddItem(defaultHair, false);
                }

                InventoryItemBase defaultEyes = new InventoryItemBase
                                                    {
                                                        Name = "Default eyes",
                                                        Description = "Default eyes description",
                                                        AssetType = (int) AssetType.Bodypart,
                                                        InvType = (int) InventoryType.Wearable,
                                                        Flags = (uint) WearableType.Eyes,
                                                        ID = AvatarWearable.DEFAULT_EYES_ITEM
                                                    };
                //Give a new copy to every person
                asset = m_AssetService.Get(AvatarWearable.DEFAULT_EYES_ASSET.ToString());
                if (asset != null)
                {
                    asset.ID = UUID.Random();
                    asset.ID = m_AssetService.Store(asset);
                    defaultEyes.AssetID = asset.ID;
                    defaultEyes.Folder = bodypartFolder.ID;
                    defaultEyes.CreatorId = m_LibraryService.LibraryOwner.ToString();
                    defaultEyes.Owner = principalID;
                    defaultEyes.BasePermissions = (uint) PermissionMask.All;
                    defaultEyes.CurrentPermissions = (uint) PermissionMask.All;
                    defaultEyes.EveryOnePermissions = (uint) PermissionMask.None;
                    defaultEyes.NextPermissions = (uint) PermissionMask.All;
                    AddItem(defaultEyes, false);
                }

                InventoryItemBase defaultShirt = new InventoryItemBase
                                                     {
                                                         Name = "Default shirt",
                                                         Description = "Default shirt description",
                                                         AssetType = (int) AssetType.Clothing,
                                                         InvType = (int) InventoryType.Wearable,
                                                         Flags = (uint) WearableType.Shirt,
                                                         ID = AvatarWearable.DEFAULT_SHIRT_ITEM
                                                     };
                //Give a new copy to every person
                asset = m_AssetService.Get(AvatarWearable.DEFAULT_SHIRT_ASSET.ToString());
                if (asset != null)
                {
                    asset.ID = UUID.Random();
                    asset.ID = m_AssetService.Store(asset);
                    defaultShirt.AssetID = asset.ID;
                    defaultShirt.Folder = clothingFolder.ID;
                    defaultShirt.CreatorId = m_LibraryService.LibraryOwner.ToString();
                    defaultShirt.Owner = principalID;
                    defaultShirt.BasePermissions = (uint) PermissionMask.All;
                    defaultShirt.CurrentPermissions = (uint) PermissionMask.All;
                    defaultShirt.EveryOnePermissions = (uint) PermissionMask.None;
                    defaultShirt.NextPermissions = (uint) PermissionMask.All;
                    AddItem(defaultShirt, false);
                }

                InventoryItemBase defaultPants = new InventoryItemBase
                                                     {
                                                         Name = "Default pants",
                                                         Description = "Default pants description",
                                                         AssetType = (int) AssetType.Clothing,
                                                         InvType = (int) InventoryType.Wearable,
                                                         Flags = (uint) WearableType.Pants,
                                                         ID = AvatarWearable.DEFAULT_PANTS_ITEM
                                                     };
                //Give a new copy to every person
                asset = m_AssetService.Get(AvatarWearable.DEFAULT_PANTS_ASSET.ToString());
                if (asset != null)
                {
                    asset.ID = UUID.Random();
                    asset.ID = m_AssetService.Store(asset);
                    defaultPants.AssetID = asset.ID;
                    defaultPants.Folder = clothingFolder.ID;
                    defaultPants.CreatorId = m_LibraryService.LibraryOwner.ToString();
                    defaultPants.Owner = principalID;
                    defaultPants.BasePermissions = (uint) PermissionMask.All;
                    defaultPants.CurrentPermissions = (uint) PermissionMask.All;
                    defaultPants.EveryOnePermissions = (uint) PermissionMask.None;
                    defaultPants.NextPermissions = (uint) PermissionMask.All;
                    AddItem(defaultPants, false);
                }
            }

            return result;
        }

        public virtual List<InventoryFolderBase> GetInventorySkeleton(UUID principalID)
        {
            List<InventoryFolderBase> allFolders = m_Database.GetFolders(
                new[] {"agentID"},
                new[] {principalID.ToString()});

            if (allFolders.Count == 0)
                return null;

            return allFolders;
        }

        public virtual List<InventoryFolderBase> GetRootFolders(UUID principalID)
        {
            return m_Database.GetFolders(
                new[] {"agentID", "parentFolderID"},
                new[] {principalID.ToString(), UUID.Zero.ToString()});
        }

        public virtual InventoryFolderBase GetRootFolder(UUID principalID)
        {
            List<InventoryFolderBase> folders = m_Database.GetFolders(
                new[] {"agentID", "parentFolderID"},
                new[] {principalID.ToString(), UUID.Zero.ToString()});

            if (folders.Count == 0)
                return null;

            InventoryFolderBase root = null;
#if (!ISWIN)
            foreach (InventoryFolderBase folder in folders)
            {
                if (folder.Name == "My Inventory") root = folder;
            }
#else
            foreach (InventoryFolderBase folder in folders.Where(folder => folder.Name == "My Inventory"))
                root = folder;
#endif
            if (folders == null) // oops
                root = folders[0];

            return root;
        }

        public virtual InventoryFolderBase GetFolderForType(UUID principalID, InventoryType invType, AssetType type)
        {
//            MainConsole.Instance.DebugFormat("[XINVENTORY SERVICE]: Getting folder type {0} for user {1}", type, principalID);
            if (invType == InventoryType.Snapshot)
                type = AssetType.SnapshotFolder;
                    //Fix for snapshots, as they get the texture asset type, but need to get checked as snapshotfolder types

            List<InventoryFolderBase> folders = m_Database.GetFolders(
                new[] {"agentID", "type"},
                new[] {principalID.ToString(), ((int) type).ToString()});

            if (folders.Count == 0)
            {
//                MainConsole.Instance.WarnFormat("[XINVENTORY SERVICE]: Found no folder for type {0} for user {1}", type, principalID);
                return null;
            }

//            MainConsole.Instance.DebugFormat(
//                "[XINVENTORY SERVICE]: Found folder {0} {1} for type {2} for user {3}", 
//                folders[0].folderName, folders[0].folderID, type, principalID);

            return folders[0];
        }

        public virtual InventoryCollection GetFolderContent(UUID principalID, UUID folderID)
        {
            // This method doesn't receive a valud principal id from the
            // connector. So we disregard the principal and look
            // by ID.
            //
            MainConsole.Instance.DebugFormat("[XINVENTORY SERVICE]: Fetch contents for folder {0}", folderID.ToString());
            InventoryCollection inventory = new InventoryCollection
                                                {
                                                    UserID = principalID,
                                                    Folders = m_Database.GetFolders(
                                                        new[] {"parentFolderID"},
                                                        new[] {folderID.ToString()}),
                                                    Items = m_Database.GetItems(
                                                        new[] {"parentFolderID"},
                                                        new[] {folderID.ToString()})
                                                };



            return inventory;
        }

        public virtual List<InventoryItemBase> GetFolderItems(UUID principalID, UUID folderID)
        {
            //            MainConsole.Instance.DebugFormat("[XINVENTORY]: Fetch items for folder {0}", folderID);

            // Since we probably don't get a valid principal here, either ...
            //
            return m_Database.GetItems(
                new[] {"parentFolderID", "avatarID"},
                new[] {folderID.ToString(), principalID.ToString()});
        }

        public virtual OSDArray GetLLSDFolderItems(UUID principalID, UUID folderID)
        {
            //            MainConsole.Instance.DebugFormat("[XINVENTORY]: Fetch items for folder {0}", folderID);

            // Since we probably don't get a valid principal here, either ...
            //
            return m_Database.GetLLSDItems(
                new[] {"parentFolderID", "avatarID"},
                new[] {folderID.ToString(), principalID.ToString()});
        }

        public virtual List<InventoryFolderBase> GetFolderFolders(UUID principalID, UUID folderID)
        {
            //            MainConsole.Instance.DebugFormat("[XINVENTORY]: Fetch folders for folder {0}", folderID);

            // Since we probably don't get a valid principal here, either ...
            //
            List<InventoryFolderBase> invItems = m_Database.GetFolders(
                new[] {"parentFolderID"},
                new[] {folderID.ToString()});

            return invItems;
        }

        public virtual bool AddFolder(InventoryFolderBase folder)
        {
            InventoryFolderBase check = GetFolder(folder);
            if (check != null)
                return false;

            return m_Database.StoreFolder(folder);
        }

        public virtual bool UpdateFolder(InventoryFolderBase folder)
        {
            if (!m_AllowDelete) //Initial item MUST be created as a link folder
                if (folder.Type == (sbyte) AssetType.LinkFolder)
                    return false;

            InventoryFolderBase check = GetFolder(folder);
            if (check == null)
                return AddFolder(folder);

            if (check.Type != -1 || folder.Type != -1)
            {
                if (folder.Version > check.Version)
                    return false;
                check.Version = folder.Version;
                check.Type = folder.Type;
                check.Version++;
                return m_Database.StoreFolder(check);
            }

            if (folder.Version < check.Version)
                folder.Version = check.Version;
            folder.ID = check.ID;

            folder.Version++;
            return m_Database.StoreFolder(folder);
        }

        public virtual bool MoveFolder(InventoryFolderBase folder)
        {
            List<InventoryFolderBase> x = m_Database.GetFolders(
                new[] {"folderID"},
                new[] {folder.ID.ToString()});

            if (x.Count == 0)
                return false;

            x[0].ParentID = folder.ParentID;

            return m_Database.StoreFolder(x[0]);
        }

        // We don't check the principal's ID here
        //
        public virtual bool DeleteFolders(UUID principalID, List<UUID> folderIDs)
        {
            if (!m_AllowDelete)
            {
                foreach (UUID id in folderIDs)
                {
                    if (!ParentIsLinkFolder(id))
                        continue;
                    InventoryFolderBase f = new InventoryFolderBase {ID = id};
                    PurgeFolder(f);
                    m_Database.DeleteFolders("folderID", id.ToString(), true);
                }
                return true;
            }

            // Ignore principal ID, it's bogus at connector level
            //
            foreach (UUID id in folderIDs)
            {
                if (!ParentIsTrash(id))
                    continue;
                InventoryFolderBase f = new InventoryFolderBase {ID = id};
                PurgeFolder(f);
                m_Database.DeleteFolders("folderID", id.ToString(), true);
            }

            return true;
        }

        public virtual bool PurgeFolder(InventoryFolderBase folder)
        {
            if (!m_AllowDelete && !ParentIsLinkFolder(folder.ID))
                return false;

            if (!ParentIsTrash(folder.ID))
                return false;

            List<InventoryFolderBase> subFolders = m_Database.GetFolders(
                new[] {"parentFolderID"},
                new[] {folder.ID.ToString()});

            foreach (InventoryFolderBase x in subFolders)
            {
                PurgeFolder(x);
                m_Database.DeleteFolders("folderID", x.ID.ToString(), true);
            }

            m_Database.DeleteItems("parentFolderID", folder.ID.ToString());

            return true;
        }

        public virtual bool ForcePurgeFolder(InventoryFolderBase folder)
        {
            List<InventoryFolderBase> subFolders = m_Database.GetFolders(
                new[] {"parentFolderID"},
                new[] {folder.ID.ToString()});

            foreach (InventoryFolderBase x in subFolders)
            {
                ForcePurgeFolder(x);
                m_Database.DeleteFolders("folderID", x.ID.ToString(), false);
            }

            m_Database.DeleteItems("parentFolderID", folder.ID.ToString());
            m_Database.DeleteFolders("folderID", folder.ID.ToString(), false);

            return true;
        }

        public virtual bool AddItem(InventoryItemBase item)
        {
            return AddItem(item, true);
        }

        public virtual bool AddItem(InventoryItemBase item, bool doParentFolderCheck)
        {
//            MainConsole.Instance.DebugFormat(
//                "[XINVENTORY SERVICE]: Adding item {0} to folder {1} for {2}", item.ID, item.Folder, item.Owner);

            if (doParentFolderCheck)
            {
                InventoryFolderBase folder = GetFolder(new InventoryFolderBase(item.Folder));

                if (folder == null || folder.Owner != item.Owner)
                    return false;
            }
            m_Database.IncrementFolder(item.Folder);
            return m_Database.StoreItem(item);
        }

        public virtual bool UpdateItem(InventoryItemBase item)
        {
            if (!m_AllowDelete) //Initial item MUST be created as a link or link folder
                if (item.AssetType == (sbyte) AssetType.Link || item.AssetType == (sbyte) AssetType.LinkFolder)
                    return false;
            m_Database.IncrementFolder(item.Folder);
            return m_Database.StoreItem(item);
        }

        public virtual bool MoveItems(UUID principalID, List<InventoryItemBase> items)
        {
            // Principal is b0rked. *sigh*
            //
            foreach (InventoryItemBase i in items)
            {
                m_Database.IncrementFolder(i.Folder); //Increment the new folder
                m_Database.IncrementFolderByItem(i.ID);
                    //And the old folder too (have to use this one because we don't know the old folder)
                m_Database.MoveItem(i.ID.ToString(), i.Folder.ToString());
            }

            return true;
        }

        public virtual bool DeleteItems(UUID principalID, List<UUID> itemIDs)
        {
            if (!m_AllowDelete)
            {
                foreach (UUID id in itemIDs)
                {
                    InventoryItemBase item = new InventoryItemBase(id);
                    item = GetItem(item);
                    m_Database.IncrementFolder(item.Folder);
                    if (!ParentIsLinkFolder(item.Folder))
                        continue;
                    m_Database.DeleteItems("inventoryID", id.ToString());
                }
                return true;
            }

            // Just use the ID... *facepalms*
            //
            foreach (UUID id in itemIDs)
            {
                m_Database.IncrementFolderByItem(id);
                m_Database.DeleteItems("inventoryID", id.ToString());
            }

            return true;
        }

        public virtual InventoryItemBase GetItem(InventoryItemBase item)
        {
            List<InventoryItemBase> items = m_Database.GetItems(
                new[] {"inventoryID"},
                new[] {item.ID.ToString()});

            foreach (InventoryItemBase xitem in items)
            {
                UUID nn;
                if (!UUID.TryParse(xitem.CreatorId, out nn))
                {
                    try
                    {
                        if (xitem.CreatorId != string.Empty)
                        {
                            string FullName = xitem.CreatorId.Remove(0, 7);
                            string[] FirstLast = FullName.Split(' ');
                            UserAccount account = m_UserAccountService.GetUserAccount(UUID.Zero, FirstLast[0],
                                                                                      FirstLast[1]);
                            if (account == null)
                            {
                                xitem.CreatorId = UUID.Zero.ToString();
                                m_Database.StoreItem(xitem);
                            }
                            else
                            {
                                xitem.CreatorId = account.PrincipalID.ToString();
                                m_Database.StoreItem(xitem);
                            }
                        }
                        else
                        {
                            xitem.CreatorId = UUID.Zero.ToString();
                            m_Database.StoreItem(xitem);
                        }
                    }
                    catch
                    {
                        xitem.CreatorId = UUID.Zero.ToString();
                    }
                }
            }

            if (items.Count == 0)
                return null;

            return items[0];
        }

        public virtual OSDArray GetItem(UUID itemID)
        {
            return m_Database.GetLLSDItems(
                new string[1] {"inventoryID"},
                new string[1] {itemID.ToString()});
        }

        public virtual InventoryFolderBase GetFolder(InventoryFolderBase folder)
        {
            List<InventoryFolderBase> folders = m_Database.GetFolders(
                new[] {"folderID"},
                new[] {folder.ID.ToString()});

            if (folders.Count == 0)
                return null;

            return folders[0];
        }

        public virtual List<InventoryItemBase> GetActiveGestures(UUID principalID)
        {
            return new List<InventoryItemBase>(m_Database.GetActiveGestures(principalID));
        }

        #endregion

        #region IService Members

        public virtual void Initialize(IConfigSource config, IRegistryCore registry)
        {
            IConfig handlerConfig = config.Configs["Handlers"];
            if (handlerConfig.GetString("InventoryHandler", "") != Name)
                return;

            IConfig invConfig = config.Configs["InventoryService"];
            if (invConfig != null)
                m_AllowDelete = invConfig.GetBoolean("AllowDelete", true);

            if (MainConsole.Instance != null)
                MainConsole.Instance.Commands.AddCommand("fix inventory", "fix inventory",
                                                         "If the user's inventory has been corrupted, this function will attempt to fix it",
                                                         FixInventory);
            registry.RegisterModuleInterface<IInventoryService>(this);
        }

        public virtual void Start(IConfigSource config, IRegistryCore registry)
        {
            m_Database = DataManager.RequestPlugin<IInventoryData>();
            m_UserAccountService = registry.RequestModuleInterface<IUserAccountService>();
            m_LibraryService = registry.RequestModuleInterface<ILibraryService>();
            m_AssetService = registry.RequestModuleInterface<IAssetService>();
        }

        public virtual void FinishedStartup()
        {
        }

        #endregion

        public virtual void FixInventory(string[] cmd)
        {
            string userName = MainConsole.Instance.Prompt("Name of user");
            UserAccount account = m_UserAccountService.GetUserAccount(UUID.Zero, userName);
            if (account == null)
            {
                MainConsole.Instance.Warn("Could not find user");
                return;
            }
            InventoryFolderBase rootFolder = GetRootFolder(account.PrincipalID);

            //Fix having a default root folder
            if (rootFolder == null)
            {
                MainConsole.Instance.Warn("Fixing default root folder...");
                List<InventoryFolderBase> skel = GetInventorySkeleton(account.PrincipalID);
                if (skel.Count == 0)
                {
                    CreateUserInventory(account.PrincipalID, false);
                    rootFolder = GetRootFolder(account.PrincipalID);
                }
                else
                {
                    rootFolder = new InventoryFolderBase
                                     {
                                         Name = "My Inventory",
                                         Type = (short) AssetType.RootFolder,
                                         Version = 1,
                                         ID = skel[0].ParentID,
                                         Owner = account.PrincipalID,
                                         ParentID = UUID.Zero
                                     };

                }
            }
            //Check against multiple root folders
            List<InventoryFolderBase> rootFolders = GetRootFolders(account.PrincipalID);
            List<UUID> badFolders = new List<UUID>();
            if (rootFolders.Count != 1)
            {
                //No duplicate folders!
#if (!ISWIN)
                foreach (InventoryFolderBase f in rootFolders)
                {
                    if (!badFolders.Contains(f.ID) && f.ID != rootFolder.ID)
                    {
                        MainConsole.Instance.Warn("Removing duplicate root folder " + f.Name);
                        badFolders.Add(f.ID);
                    }
                }
#else
                foreach (InventoryFolderBase f in rootFolders.Where(f => !badFolders.Contains(f.ID) && f.ID != rootFolder.ID))
                {
                    MainConsole.Instance.Warn("Removing duplicate root folder " + f.Name);
                    badFolders.Add(f.ID);
                }
#endif
            }
            //Fix any root folders that shouldn't be root folders
            List<InventoryFolderBase> skeleton = GetInventorySkeleton(account.PrincipalID);
            List<UUID> foundFolders = new List<UUID>();
            foreach (InventoryFolderBase f in skeleton)
            {
                if (!foundFolders.Contains(f.ID))
                    foundFolders.Add(f.ID);
                if (f.Name == "My Inventory" && f.ParentID != UUID.Zero)
                {
                    //Merge them all together
                    badFolders.Add(f.ID);
                }
            }
            foreach (InventoryFolderBase f in skeleton)
            {
                if ((!foundFolders.Contains(f.ParentID) && f.ParentID != UUID.Zero) ||
                    f.ID == f.ParentID)
                {
                    //The viewer loses the parentID when something goes wrong
                    //it puts it in the top where My Inventory should be
                    //We need to put it back in the My Inventory folder, as the sub folders are right for some reason
                    f.ParentID = rootFolder.ID;
                    m_Database.StoreFolder(f);
                    MainConsole.Instance.WarnFormat("Fixing folder {0}", f.Name);
                }
                else if (badFolders.Contains(f.ParentID))
                {
                    //Put it back in the My Inventory folder
                    f.ParentID = rootFolder.ID;
                    m_Database.StoreFolder(f);
                    MainConsole.Instance.WarnFormat("Fixing folder {0}", f.Name);
                }
                else if (f.Type == (short) AssetType.CurrentOutfitFolder)
                {
                    List<InventoryItemBase> items = GetFolderItems(account.PrincipalID, f.ID);
                    //Check the links!
                    List<UUID> brokenLinks = new List<UUID>();
                    foreach (InventoryItemBase item in items)
                    {
                        InventoryItemBase linkedItem = null;
                        if ((linkedItem = GetItem(new InventoryItemBase(item.AssetID))) == null)
                        {
                            //Broken link...
                            brokenLinks.Add(item.ID);
                        }
                        else if (linkedItem.ID == AvatarWearable.DEFAULT_EYES_ITEM ||
                                 linkedItem.ID == AvatarWearable.DEFAULT_BODY_ITEM ||
                                 linkedItem.ID == AvatarWearable.DEFAULT_HAIR_ITEM ||
                                 linkedItem.ID == AvatarWearable.DEFAULT_PANTS_ITEM ||
                                 linkedItem.ID == AvatarWearable.DEFAULT_SHIRT_ITEM ||
                                 linkedItem.ID == AvatarWearable.DEFAULT_SKIN_ITEM)
                        {
                            //Default item link, needs removed
                            brokenLinks.Add(item.ID);
                        }
                    }
                    if (brokenLinks.Count != 0)
                        DeleteItems(account.PrincipalID, brokenLinks);
                }
                else if (f.Type == (short) AssetType.Mesh)
                {
                    ForcePurgeFolder(f);
                }
            }
            foreach (UUID id in badFolders)
            {
                m_Database.DeleteFolders("folderID", id.ToString(), false);
            }
            //Make sure that all default folders exist
            CreateUserInventory(account.PrincipalID, false);
            //Refetch the skeleton now
            skeleton = GetInventorySkeleton(account.PrincipalID);
            Dictionary<int, UUID> defaultFolders = new Dictionary<int, UUID>();
            Dictionary<UUID, UUID> changedFolders = new Dictionary<UUID, UUID>();
#if (!ISWIN)
            foreach (InventoryFolderBase folder in skeleton)
            {
                if (folder.Type != -1)
                {
                    if (!defaultFolders.ContainsKey(folder.Type))
                        defaultFolders[folder.Type] = folder.ID;
                    else
                        changedFolders.Add(folder.ID, defaultFolders[folder.Type]);
                }
            }
#else
            foreach (InventoryFolderBase folder in skeleton.Where(folder => folder.Type != -1))
            {
                if (!defaultFolders.ContainsKey(folder.Type))
                    defaultFolders[folder.Type] = folder.ID;
                else
                    changedFolders.Add(folder.ID, defaultFolders[folder.Type]);
            }
#endif
            foreach (InventoryFolderBase folder in skeleton)
            {
                if (folder.Type != -1 && defaultFolders[folder.Type] != folder.ID)
                {
                    //Delete the dup
                    ForcePurgeFolder(folder);
                    MainConsole.Instance.Warn("Purging duplicate default inventory type folder " + folder.Name);
                }
                if (changedFolders.ContainsKey(folder.ParentID))
                {
                    folder.ParentID = changedFolders[folder.ParentID];
                    MainConsole.Instance.Warn("Merging child folder of default inventory type " + folder.Name);
                    m_Database.StoreFolder(folder);
                }
            }
            MainConsole.Instance.Warn("Completed the check");
        }

        protected InventoryFolderBase CreateFolder(UUID principalID, UUID parentID, int type, string name)
        {
            InventoryFolderBase newFolder = new InventoryFolderBase
                                                {
                                                    Name = name,
                                                    Type = (short) type,
                                                    Version = 1,
                                                    ID = UUID.Random(),
                                                    Owner = principalID,
                                                    ParentID = parentID
                                                };


            m_Database.StoreFolder(newFolder);

            return newFolder;
        }

        protected virtual InventoryFolderBase[] GetSystemFolders(UUID principalID)
        {
//            MainConsole.Instance.DebugFormat("[XINVENTORY SERVICE]: Getting system folders for {0}", principalID);

            InventoryFolderBase[] allFolders = m_Database.GetFolders(
                new[] {"agentID"},
                new[] {principalID.ToString()}).ToArray();

            InventoryFolderBase[] sysFolders = Array.FindAll(
                allFolders,
                delegate(InventoryFolderBase f)
                    {
                        if (f.Type > 0)
                            return true;
                        return false;
                    });

//            MainConsole.Instance.DebugFormat(
//                "[XINVENTORY SERVICE]: Found {0} system folders for {1}", sysFolders.Length, principalID);

            return sysFolders;
        }

        private bool ParentIsTrash(UUID folderID)
        {
            List<InventoryFolderBase> folder = m_Database.GetFolders(new[] {"folderID"}, new[] {folderID.ToString()});
            if (folder.Count < 1)
                return false;

            if (folder[0].Type == (int) AssetType.TrashFolder)
                return true;

            UUID parentFolder = folder[0].ParentID;

            while (parentFolder != UUID.Zero)
            {
                List<InventoryFolderBase> parent = m_Database.GetFolders(new[] {"folderID"},
                                                                         new[] {parentFolder.ToString()});
                if (parent.Count < 1)
                    return false;

                if (parent[0].Type == (int) AssetType.TrashFolder)
                    return true;
                if (parent[0].Type == (int) AssetType.RootFolder)
                    return false;

                parentFolder = parent[0].ParentID;
            }
            return false;
        }

        private bool ParentIsLinkFolder(UUID folderID)
        {
            List<InventoryFolderBase> folder = m_Database.GetFolders(new[] {"folderID"}, new[] {folderID.ToString()});
            if (folder.Count < 1)
                return false;

            if (folder[0].Type == (int) AssetType.LinkFolder)
                return true;

            UUID parentFolder = folder[0].ParentID;

            while (parentFolder != UUID.Zero)
            {
                List<InventoryFolderBase> parent = m_Database.GetFolders(new[] {"folderID"},
                                                                         new[] {parentFolder.ToString()});
                if (parent.Count < 1)
                    return false;

                if (parent[0].Type == (int) AssetType.LinkFolder)
                    return true;
                if (parent[0].Type == (int) AssetType.RootFolder)
                    return false;

                parentFolder = parent[0].ParentID;
            }
            return false;
        }
    }
}