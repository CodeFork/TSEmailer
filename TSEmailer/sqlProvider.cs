using System;
using TShockAPI;
using TShockAPI.DB;
using MySql.Data.MySqlClient;
using Community.CsharpSqlite;
using Community.CsharpSqlite.SQLiteClient;
using System.Data;
using System.Collections.Generic;

namespace sqlProvider
{
    public class tseSQL
    {
        public static SqlTableEditor SQLEditor;
        public static SqlTableCreator SQLWriter;
        public static SqlTable TSEtable;

        public tseSQL()
        {
            SQLEditor = new SqlTableEditor(TShock.DB, TShock.DB.GetSqlType() == SqlType.Sqlite ? (IQueryBuilder)new SqliteQueryCreator() : new MysqlQueryCreator());
            SQLWriter = new SqlTableCreator(TShock.DB, TShock.DB.GetSqlType() == SqlType.Sqlite ? (IQueryBuilder)new SqliteQueryCreator() : new MysqlQueryCreator());
            TSEtable = new SqlTable("TSEmailer",
                new SqlColumn("TSEindex", MySqlDbType.Int32) { AutoIncrement = true, Primary = true, Unique = true, NotNull = true },
                new SqlColumn("PlayerID", MySqlDbType.Int32) { Unique = true, NotNull = true },
                new SqlColumn("RecordType", MySqlDbType.Int32) { NotNull = true }, //RecordTypes: ['1' = Player Settings] ['2' = OnJoin notifing address])
                new SqlColumn("address", MySqlDbType.Text) { Length = 250 },
                new SqlColumn("players", MySqlDbType.Bit) { DefaultValue = "1" },
                new SqlColumn("admins", MySqlDbType.Bit) { DefaultValue = "1" },
                new SqlColumn("eblast", MySqlDbType.Bit) { DefaultValue = "1" },
                new SqlColumn("reply", MySqlDbType.Bit) { DefaultValue = "1" },
                new SqlColumn("notify", MySqlDbType.Bit) { DefaultValue = "1" },
                new SqlColumn("onjoin", MySqlDbType.Text) { Length = 250 }
                );
            SetupDBTable();
        }

        public static DataTable GetDataTable(List<SqlValue> wheres)
        {
            DataTable table = new DataTable();
            for (int i=0; i<TSEtable.Columns.Count; i++)
            {
                table.Columns.Add(TSEtable.Columns[i].Name.ToString(), TSEtable.Columns[i].Type.GetType());

                List<object> colValues = SQLEditor.ReadColumn(TSEtable.Name.ToString(), TSEtable.Columns[i].Name.ToString(), wheres);

                for (int x=0; x<colValues.Count; x++)
                {
                    if (table.Rows[x].IsNull(i))
                    {
                        table.Rows.Add(new DataRow());
                    }
                    table.Rows[x].SetField(i, colValues[x]);
                }
            }
            
            return table;
        }

        public static void SetPlayerAddress(int curPlayerID, string address)
        {
            //Check to if the player ID exists with record type 1
            List<SqlValue> values = new List<SqlValue>(){
                new SqlValue("PlayerID", curPlayerID),
                new SqlValue("RecordType",1)
                 };
            List<Object> results = SQLEditor.ReadColumn(TSEtable.Name, "TSEindex", values);
            if (Int32.Parse(results[0].ToString()) > 0)
            {

            }
            //string text = SQLEditor.ReadColumn("Tutorial", "String", new List<SqlValue>())[0].ToString();
            //if ( SQLEditor.ReadColumn("TSEindex", "PlayerID", new HandlerList<
        }

        public static void SetupDBTable()
        {
            try
            {
                SQLWriter.EnsureExists(TSEtable);
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("TSEmailer - Error with DB table setup");
                Console.ForegroundColor = ConsoleColor.Gray;
                Log.Error("TSEmailer - DB Exception");
                Log.Error(ex.ToString());
            }
        }
    }
}