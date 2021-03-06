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
using Aurora.Framework;
using C5;

namespace Aurora.DataManager.Migration.Migrators
{
    public class DirectoryMigrator_6 : Migrator
    {
        public DirectoryMigrator_6()
        {
            Version = new Version(0, 0, 6);
            MigrationName = "Directory";

            schema = new List<Rec<string, ColumnDefinition[]>>();

            AddSchema("searchparcel", ColDefs(ColDef("RegionID", ColumnTypes.String50),
                                              ColDef("ParcelID", ColumnTypes.String50, true),
                                              ColDef("LocalID", ColumnTypes.String50),
                                              ColDef("LandingX", ColumnTypes.String50),
                                              ColDef("LandingY", ColumnTypes.String50),
                                              ColDef("LandingZ", ColumnTypes.String50),
                                              ColDef("Name", ColumnTypes.String50),
                                              ColDef("Description", ColumnTypes.String255),
                                              ColDef("Flags", ColumnTypes.String50),
                                              ColDef("Dwell", ColumnTypes.String50),
                                              ColDef("InfoUUID", ColumnTypes.String50),
                                              ColDef("ForSale", ColumnTypes.String50),
                                              ColDef("SalePrice", ColumnTypes.String50),
                                              ColDef("Auction", ColumnTypes.String50),
                                              ColDef("Area", ColumnTypes.String50),
                                              ColDef("EstateID", ColumnTypes.String50),
                                              ColDef("Maturity", ColumnTypes.String50),
                                              ColDef("OwnerID", ColumnTypes.String50),
                                              ColDef("GroupID", ColumnTypes.String50),
                                              ColDef("ShowInSearch", ColumnTypes.String50),
                                              ColDef("SnapshotID", ColumnTypes.String50),
                                              ColDef("Bitmap", ColumnTypes.LongText),
                                              ColDef("Category", ColumnTypes.String50)));

            AddSchema("asevents", ColDefs(
                ColDef("EID", ColumnTypes.Integer11, true),
                ColDef("creator", ColumnTypes.Char36),
                ColDef("region", ColumnTypes.Char36),
                ColDef("parcel", ColumnTypes.Char36),
                ColDef("date", ColumnTypes.DateTime),
                ColDef("cover", ColumnTypes.Integer11),
                ColDef("maturity", ColumnTypes.TinyInt4),
                ColDef("flags", ColumnTypes.Integer11),
                ColDef("duration", ColumnTypes.Integer11),
                ColDef("localPosX", ColumnTypes.Float),
                ColDef("localPosY", ColumnTypes.Float),
                ColDef("localPosZ", ColumnTypes.Float),
                ColDef("name", ColumnTypes.String50),
                ColDef("description", ColumnTypes.String255),
                ColDef("category", ColumnTypes.String50)
            ));
        }

        protected override void DoCreateDefaults(IDataConnector genericData)
        {
            EnsureAllTablesInSchemaExist(genericData);
        }

        protected override bool DoValidate(IDataConnector genericData)
        {
            return TestThatAllTablesValidate(genericData);
        }

        protected override void DoMigrate(IDataConnector genericData)
        {
            DoCreateDefaults(genericData);
        }

        protected override void DoPrepareRestorePoint(IDataConnector genericData)
        {
            CopyAllTablesToTempVersions(genericData);
        }
    }
}