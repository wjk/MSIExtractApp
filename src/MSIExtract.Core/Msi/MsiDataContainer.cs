using System.Collections.Generic;
using System.Linq;
using WixToolset.Dtf.WindowsInstaller;

namespace MSIExtract.Msi
{
    public class MsiDataContainer
    {
        readonly MsiFile[] files;
        readonly TableWithData[] tables;

        /// <summary>
        /// A list of installable files present in the database.
        /// </summary>
        public MsiFile[] Files => this.files;

        /// <summary>
        /// The raw view of the tables in the database.
        /// </summary>
        public TableWithData[] Tables => this.tables;

        /// <summary>
        /// Creates a <see cref="MsiDataContainer"/> object from the specified database.
        /// This contains a list of the files in the database, and all tables.
        /// </summary>
        public static MsiDataContainer CreateFromPath(LessIO.Path msiDatabaseFilePath)
        {
            return new MsiDataContainer(msiDatabaseFilePath);
        }

        private MsiDataContainer(LessIO.Path msiDatabaseFilePath)
        {
            using var db = new Database(msiDatabaseFilePath.PathString, DatabaseOpenMode.ReadOnly);
            this.files = MsiFile.CreateMsiFilesFromMSI(db);

            this.tables = db.Tables.OrderBy(tab => tab.Name).Select(table => TableWithData.Create(db, table.Name)).ToArray();
        }
    }
}
