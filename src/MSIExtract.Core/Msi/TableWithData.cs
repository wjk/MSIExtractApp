using System.Data;
using WixToolset.Dtf.WindowsInstaller;

namespace MSIExtract.Msi
{
    public class TableWithData
    {
        public string Name { get; private set; }

        public bool IsEmpty { get; private set; }

        public DataTable Rows { get; private set; }

        internal static TableWithData Create(Database db, string name)
        {
            TableRow[] rows = TableRow.GetRowsFromTable(db, name, out ColumnInfo[] columns);

            var hasSequence = false;
            var dt = new DataTable();
            foreach (ColumnInfo col in columns)
            {
                // Allow integer columns to be sorted naturally
                if (col.IsInteger)
                {
                    dt.Columns.Add(col.Name, typeof(int));
                }
                else
                {
                    dt.Columns.Add(col.Name);
                }
                hasSequence |= col.Name.ToUpper() == "SEQUENCE";
            }

            foreach(TableRow row in rows)
            {
                DataRow dtRow = dt.NewRow();
                foreach (ColumnInfo col in columns)
                {
                    if (col.IsStream)
                    {
                        // TODO: Figure something out for this later.
                        dtRow[col.Name] = "[Binary data]";
                    }
                    else
                    {
                        dtRow[col.Name] = row.GetValue(col.Name);
                    }
                }
                dt.Rows.Add(dtRow);
            }

            // There are quite some data tables that have a 'Sequence' column,
            // so we sort by that if this column is present
            if (hasSequence)
            {
                DataView view = dt.DefaultView;
                view.Sort = "SEQUENCE ASC";
                dt = view.ToTable();
            }
            return new TableWithData(name)
            {
                IsEmpty = dt.Rows.Count == 0,
                Rows = dt
            };
        }

        private TableWithData(string name)
        {
            this.Name = name;
        }
    }
}
