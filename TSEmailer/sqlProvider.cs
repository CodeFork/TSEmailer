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
    public class TSdb
    {
        public static SqlTableEditor SQLEditor;
        public static SqlTableCreator SQLWriter;
        public static SqlTable TSEtable;

        public TSdb()
        {
            SQLEditor = new SqlTableEditor(TShock.DB, TShock.DB.GetSqlType() == SqlType.Sqlite ? (IQueryBuilder)new SqliteQueryCreator() : new MysqlQueryCreator());
            SQLWriter = new SqlTableCreator(TShock.DB, TShock.DB.GetSqlType() == SqlType.Sqlite ? (IQueryBuilder)new SqliteQueryCreator() : new MysqlQueryCreator());
            TSEtable = new SqlTable("TSEmailer",
                new SqlColumn("TSEindex", MySqlDbType.Int32) { AutoIncrement = true, Primary = true, Unique = true, NotNull = true },
                new SqlColumn("PlayerID", MySqlDbType.Int32) { Unique = true, NotNull = true },
                new SqlColumn("PlayerName", MySqlDbType.Text) { Length = 250, NotNull = true },
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

        public DataTable GetDataTable(List<SqlValue> wheres)
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

        public DataRow GetPlayerStatus(int curPlayerID)
        {
            DataTable dt = GetDataTable(new List<SqlValue>{
                new SqlValue("PlayerID",curPlayerID),
                new SqlValue("RecordType",1)});
            if (dt.Rows.Count > 0)
            {
                return dt.Rows[0];
            }
            else
            {
                return new DataRow();
            }
        }

        public string SetOnJoinAddress(int curPlayerID, string PlayerName)
        {
            //check curPlayerID has email address set
            //check PlayerName has email address registered
            //set OnJoin address as RecordType = 2
            return "Not Yet Implemented...";
        }

        public string SetPlayerAddress(TSPlayer player, string address)
        {
            //Check to if the player ID exists with record type 1
            List<SqlValue> values = new List<SqlValue>(){
                new SqlValue("PlayerID", player.UserID),
                new SqlValue("RecordType",1)
                 };
            List<Object> results = SQLEditor.ReadColumn(TSEtable.Name, "TSEindex", values);
            if (Int32.Parse(results[0].ToString()) < 1)
            {
                values.Clear();
                values.Add(new SqlValue("PlayerID", player.UserID));
                values.Add(new SqlValue("PlayerName", player.Name));
                values.Add(new SqlValue("RecordType",1));
                values.Add(new SqlValue("Address", address));
                SQLEditor.InsertValues(TSEtable.Name, values);
                return "Your email address is now registered with the server.";
            }
            else
            {
                SQLEditor.UpdateValues(TSEtable.Name, new List<SqlValue> { new SqlValue("Address", address), new SqlValue("PlayerName", player.Name) }, new List<SqlValue> { new SqlValue("TSEindex", Int32.Parse(results[0].ToString())) });
                return "Your email address has been updated.";
            }
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