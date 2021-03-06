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

namespace Aurora.Framework
{
    /// <summary>
    ///   Connector that links Aurora IDataPlugins to a database backend
    /// </summary>
    public interface IDataConnector : IGenericData
    {
        /// <summary>
        ///   Name of the module
        /// </summary>
        string Identifier { get; }

        /// <summary>
        ///   Checks to see if table 'table' exists
        /// </summary>
        /// <param name = "table"></param>
        /// <returns></returns>
        bool TableExists(string table);

        /// <summary>
        ///   Create a generic table
        /// </summary>
        /// <param name = "table"></param>
        /// <param name = "columns"></param>
        void CreateTable(string table, ColumnDefinition[] columns);

        /// <summary>
        ///   Get the latest version of the database
        /// </summary>
        /// <returns></returns>
        Version GetAuroraVersion(string migratorName);

        /// <summary>
        ///   Set the version of the database
        /// </summary>
        /// <param name = "version"></param>
        void WriteAuroraVersion(Version version, string MigrationName);

        /// <summary>
        ///   Copy tables
        /// </summary>
        /// <param name = "sourceTableName"></param>
        /// <param name = "destinationTableName"></param>
        /// <param name = "columnDefinitions"></param>
        void CopyTableToTable(string sourceTableName, string destinationTableName, ColumnDefinition[] columnDefinitions);

        /// <summary>
        ///   Check whether the data table exists and that the columns are correct
        /// </summary>
        /// <param name = "tableName"></param>
        /// <param name = "columnDefinitions"></param>
        /// <returns></returns>
        bool VerifyTableExists(string tableName, ColumnDefinition[] columnDefinitions);

        /// <summary>
        ///   Check whether the data table exists and that the columns are correct
        ///   Then create the table if it is not created
        /// </summary>
        /// <param name = "tableName"></param>
        /// <param name = "columnDefinitions"></param>
        void EnsureTableExists(string tableName, ColumnDefinition[] columnDefinitions, Dictionary<string, string> renameColumns);

        /// <summary>
        ///   Rename the table from oldTableName to newTableName
        /// </summary>
        /// <param name = "oldTableName"></param>
        /// <param name = "newTableName"></param>
        void RenameTable(string oldTableName, string newTableName);

        /// <summary>
        ///   Drop a table
        /// </summary>
        /// <param name = "tableName"></param>
        void DropTable(string tableName);
    }

    public enum DataManagerTechnology
    {
        SQLite,
        MySql,
        MSSQL2008,
        MSSQL7
    }

    public enum ColumnTypes
    {
        Blob,
        LongBlob,
        Char36,
        Char32,
        Date,
        DateTime,
        Double,
        Integer11,
        Integer30,
        UInteger11,
        UInteger30,
        String,
        String1,
        String2,
        String10,
        String16,
        String30,
        String32,
        String36,
        String45,
        String50,
        String64,
        String128,
        String100,
        String255,
        String512,
        String1024,
        String8196,
        Text,
        MediumText,
        LongText,
        TinyInt1,
        TinyInt4,
        Float,
        Unknown
    }

    public class ColumnDefinition
    {
        public string Name { get; set; }
        public ColumnTypes Type { get; set; }
        public bool IsPrimary { get; set; }

        public override bool Equals(object obj)
        {
            var cdef = obj as ColumnDefinition;
            if (cdef != null)
            {
                return cdef.Name == Name && cdef.Type == Type && cdef.IsPrimary == IsPrimary;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}