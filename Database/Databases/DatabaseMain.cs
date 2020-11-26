using System.Collections;
using System.Collections.Generic;
using System.Data;
using UnityEngine;

using Mono.Data.Sqlite;

public class DatabaseMain : MonoBehaviour
{
    // Start is called before the first frame update
    
    private string DatabaseName = "MainDatabase.db";
    private string tableName = "TestTable";
    void Start()
    {
    
        string connection = "URI=file:" + Application.persistentDataPath + "/" + DatabaseName;
        IDbConnection dbcon = new SqliteConnection(connection);
        dbcon.Open();

        getAllData(tableName, dbcon);

    }
    
    public IDataReader getAllData(string table_name, IDbConnection db_connection)
    {
        IDbCommand dbcmd = db_connection.CreateCommand();
        dbcmd.CommandText =
            "SELECT * FROM " + table_name;
        IDataReader reader = dbcmd.ExecuteReader();
        return reader;
    }

    public IDataReader getNumOfRows(string table_name, IDbConnection db_connection)
    {
        IDbCommand dbcmd = db_connection.CreateCommand();
        dbcmd.CommandText =
            "SELECT COALESCE(MAX(id)+1, 0) FROM " + table_name;
        IDataReader reader = dbcmd.ExecuteReader();
        return reader;
    }
  
}
