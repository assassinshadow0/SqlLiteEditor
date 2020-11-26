using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

using UnityEditor.IMGUI.Controls;
using Mono.Data.Sqlite;

public class DatabaseViewer : EditorWindow
{
    
    SimpleTreeView m_SimpleTreeView;
    [SerializeField] TreeViewState m_TreeViewState;
    void OnEnable ()
    {
        // Check whether there is already a serialized view state (state 
        // that survived assembly reloading)
        if (m_TreeViewState == null)
        {
            m_TreeViewState = new TreeViewState();
        }
        
        
        
        string connection = "URI=file:" + Application.persistentDataPath + "/" + DatabaseName;

        dbcon = new SqliteConnection(connection);
        dbcon.Open();
        
       


    }

    private IDbConnection dbcon;
    [MenuItem ("Window/Database/DatabaseViewer")]
    public static void  ShowWindow () {
        EditorWindow.GetWindow(typeof(DatabaseViewer));
    }

    private int _selected = 0;
    private string DatabaseName = "MainDatabase.db";
    private string NewTableName = "";

    private bool changed = true;

    private int oldSelection;
    private bool tableChanged= false;

    private string[] actions = { "Viewing", "Edit" ,"Add Row", "New Table", "Drop Table (Delete)", "Empty Table (Clear values)", "New Database", "Delete Database"};
    
    private string[] FieldType = { "Integer", "Text"};

    private string currentTable;
    private List<string> NewData = new List<string>();
    private int action = 0;

    private bool actionChanged = false;
    private List<string> fields;
    private bool tableIsEmpty = false;
    void OnGUI () {
        // The actual window code goes here

      

        
        Rect controlRect = new Rect(0, 0, 200, EditorGUIUtility.singleLineHeight);
        
        controlRect.y += controlRect.height;
        
        
        GUILayout.Label ("Database", EditorStyles.boldLabel);

        string oldDb = DatabaseName;
        DatabaseName = EditorGUILayout.TextField ("Database Name", DatabaseName);

        
        if (dbcon == null)
        {
            return;
        }
        else
        {
            if (!oldDb.Equals(DatabaseName))
            {
                string connection = "URI=file:" + Application.persistentDataPath + "/" + DatabaseName; 
                dbcon.Close();
                dbcon = new SqliteConnection(connection);
                dbcon.Open();
            }
        }

        
        List<string> tables = new List<string>();
        
        IDataReader dbTables = getTables(dbcon);
        while (dbTables.Read())
        {
            tables.Add(dbTables[1].ToString());
        }


        tableIsEmpty = (tables.Count == 0);
        
        
        tableChanged = false;
        actionChanged = false;
        oldSelection = this._selected;
        this._selected = EditorGUILayout.Popup("Select Table", _selected, tables.ToArray());

       

        if (oldSelection != this._selected)
        {
            tableChanged = true;
            action = 0;
            NewData.Clear();
        }
        
        
        controlRect.y += controlRect.height;

        if (!tableIsEmpty)
        {
            currentTable = tables[_selected];


            IDbCommand cmndRead = dbcon.CreateCommand();
            IDataReader reader;
            string query = "SELECT * FROM " + tables[_selected];
            cmndRead.CommandText = query;
            reader = cmndRead.ExecuteReader();
            MultiColumnHeaderState.Column[] columns = new MultiColumnHeaderState.Column[reader.FieldCount];
            fields = getFields(dbcon, tables[_selected]);

            for (int i = 0; i < fields.Count; i++)
            {
                columns[i] = new MultiColumnHeaderState.Column();
                columns[i].headerContent = new GUIContent(fields[i]);
            }

            MultiColumnHeaderState headerstate = new MultiColumnHeaderState(columns);
            MultiColumnHeader header = new MultiColumnHeader(headerstate);

            if (m_SimpleTreeView == null || tableChanged)
            {
                m_SimpleTreeView = new SimpleTreeView(m_TreeViewState, header, reader, this);

            }

        }


        int oldAction = action;
        
        this.action = EditorGUILayout.Popup("Actions", this.action , actions);
        
        if (oldAction != this.action)
        {
            actionChanged = true;
            
        }

        

        if (action >= 0)
        {
            if (m_SimpleTreeView != null)
            {
                m_SimpleTreeView.SetAction(action);
            }

            if (action == viewing && actionChanged)
            {
               // tableChanged = true;
             
            }
            else if (action == Editing)
            {
                
            }
            else if (action == AddRow)
            {
                for (int i = 0; i < fields.Count; i++)
                {
                    controlRect.y += controlRect.height;
                    NewData.Add("");
                    NewData[i] = EditorGUILayout.TextField (fields[i], NewData[i]);
                }
                controlRect.y += controlRect.height + 50;
          
                if (GUI.Button(controlRect, "Add Data"))
                {

                    string nonNullFields = "";
                    for (int i = 0; i < NewData.Count; i++)
                    {
                        if (!String.IsNullOrEmpty(NewData[i])  && NewData[i].Trim().Length > 0 )
                        {
                            nonNullFields = nonNullFields + fields[i];

                            if (i < (fields.Count-1))
                            {
                                nonNullFields = nonNullFields+", ";
                            }
                        }
                    }
                    string nonNullFieldValues = "";
                    for (int i = 0; i < NewData.Count; i++)
                    {
                        if (!String.IsNullOrEmpty(NewData[i])  && NewData[i].Trim().Length > 0 )
                        {
                            nonNullFieldValues =  nonNullFieldValues + "\"" + NewData[i] +"\"" ;

                            if (i < (fields.Count-1))
                            {
                                nonNullFieldValues =  nonNullFieldValues+", ";
                            }
                        }
                    }

        
                    
                    string command =  "INSERT INTO "+ tables[_selected] + " ( "+ nonNullFields +" ) VALUES ( "+nonNullFieldValues+" )";
                    
          
                    
                    
                    IDbCommand cmnd = dbcon.CreateCommand();
                    cmnd.CommandText = command;
                    cmnd.ExecuteNonQuery();
                    
                    
                    action = 0;
                    NewData.Clear();
                    tableChanged = true;
                }
                //controlRect.y += controlRect.height;
            }
            else if (action == NewTable)
            {
                
                EditorGUILayout.Space(20);
                controlRect.y += controlRect.height+10;
                
                NewTableName = EditorGUILayout.TextField ("New Table Name:", NewTableName);
                controlRect.y += controlRect.height;

                NewField filed0 = new NewField();
                filed0.FieldName = "id";
                filed0.FieldType = 0; //integer
                filed0.FieldName = EditorGUILayout.TextField("Field 0 (auto incremented)", filed0.FieldName);
                if (actionChanged)
                {
                    
                    newFileds = new List<NewField>();
                    newFileds.Add(filed0);
                }

                if (newFileds == null)
                {
                    action = 0;
                    
                }
                else
                {
                    for (int i = 1; i < newFileds.Count; i++)
                    {
                        newFileds[i].FieldName = EditorGUILayout.TextField("Field " + i, newFileds[i].FieldName);
                        newFileds[i].FieldType =
                            EditorGUILayout.Popup("Field " + i + " Type", newFileds[i].FieldType, FieldType);

                        controlRect.y += controlRect.height + 20;

                    }
                }


                controlRect.y += controlRect.height+60;
                if (GUI.Button(controlRect, "Add Field"))
                {
                    NewField filed = new NewField();
                    newFileds.Add(filed);
                    
                } 
                controlRect.y += controlRect.height;
                if (GUI.Button(controlRect, "Create Table"))
                {
                    CreateTable();

                }
                
            
                
            }
            else if (action == DropTable)
            {
                
                controlRect.y += controlRect.height;
                GUILayout.Label ("Note: This will delete the table and all of its contents, are you sure ?", EditorStyles.boldLabel);
                
                controlRect.y += controlRect.height + 20;
                
                if (GUI.Button(controlRect, "Delete Table"))
                {

                    IDbCommand dbcmd = dbcon.CreateCommand();
                    string q_createTable = 
                        "DROP TABLE IF EXISTS "  + currentTable;
                    
                    
                    dbcmd.CommandText = q_createTable;
                    dbcmd.ExecuteReader();

                    oldSelection = -1;
                    
                    action = 0;
                    tableChanged = true;
                }
                
            }
            else if (action == EmptyTable)
            {
                
                
                controlRect.y += controlRect.height;
                GUILayout.Label ("Note: This will clear all the data in this table, the table structure will remain unchanged. Are you sure you want to do this?", EditorStyles.boldLabel);
                
                controlRect.y += controlRect.height + 20;
                
                if (GUI.Button(controlRect, "Clear Table"))
                {

                    IDbCommand dbcmd = dbcon.CreateCommand();
                    string q_createTable = 
                        "Delete FROM "  + currentTable;
                    
                    dbcmd.CommandText = q_createTable;
                    dbcmd.ExecuteReader();

                    oldSelection = -1;
                    
                    action = 0;
                    tableChanged = true;
                }


            }
            else if (action == NewDatabase)
            {
                
            }
            else if (action == DeleteDatabase)
            {
                controlRect.y += controlRect.height;
                GUILayout.Label ("Note: This will delete the entire database and its contents, are you sure ?", EditorStyles.boldLabel);
                
                controlRect.y += controlRect.height + 20;
                
                if (GUI.Button(controlRect, "Delete Database"))
                {

                    File.Delete("URI=file:" + Application.persistentDataPath + "/" + DatabaseName);

                    oldSelection = -1;
                    
                    action = 0;
                    tableChanged = true;
                }
            }


            //action = 0;
        }
        
        
        
        
        controlRect.y += controlRect.height;

        float startY = controlRect.y + controlRect.height + 12;
        
        Rect r = new Rect(0, startY, position.width, position.height - startY);
    
        
        if (m_SimpleTreeView != null)
            m_SimpleTreeView.OnGUI(r);

      
    }
    
    void CreateTable()
    {

        string[] FieldTypeReal = {"INTEGER", "TEXT", "INTEGER PRIMARY KEY"};

        newFileds[0].FieldType = 2;
        
        string commandBuild =" ( ";
        for (int i = 0; i < newFileds.Count; i++)
        {
            
            if (!String.IsNullOrEmpty(newFileds[i].FieldName)  && newFileds[i].FieldName.Trim().Length > 0 )
            {
                commandBuild =  commandBuild  + newFileds[i].FieldName + "     " + FieldTypeReal[newFileds[i].FieldType] ;

                if (i < (newFileds.Count - 1 ))
                {
                    commandBuild =  commandBuild+", ";
                }
            }
            
        }

        commandBuild = commandBuild + " )";
        
        IDbCommand dbcmd = dbcon.CreateCommand();
        string q_createTable = 
            "CREATE TABLE IF NOT EXISTS " + NewTableName + commandBuild;
  
        Debug.Log(q_createTable);
        
        dbcmd.CommandText = q_createTable;
        dbcmd.ExecuteReader();


        action = 0;
        newFileds.Clear();
    } 

    private List<NewField> newFileds;
    void UpdateACell(string newValue, int column, string[] cellValues)
    {
        
 
        string whereStatementBuild = "";
        for (int i = 0; i < fields.Count; i++)
        {
            if (i != column)
            {
               
                whereStatementBuild = whereStatementBuild + fields[i] + " = \"" + cellValues[    i ] + "\"";
                if (i < (fields.Count - 1))
                {
                    whereStatementBuild = whereStatementBuild + " AND ";
                }
                
            }
         
        }

         string command =  "UPDATE "+ currentTable + " SET " + fields[column] + " = \"" + newValue + "\" " +
                           "WHERE "+ whereStatementBuild;
        
        Debug.Log(command);
       
        IDbCommand cmnd = dbcon.CreateCommand();
        cmnd.CommandText = command;
        cmnd.ExecuteNonQuery();
    
        //
        
    }
    
    void DeleteACell(int column, string[] cellValues)
    {
        
       // Debug.Log("Column - " + column + "  Row - " + row + " cell size ");
        //Debug.Log(newValue);
        string whereStatementBuild = "";
        for (int i = 0; i < fields.Count; i++)
        {
            whereStatementBuild = whereStatementBuild + fields[i] + " = \"" + cellValues[    i ] + "\"";
                if (i < (fields.Count - 1))
                {
                    whereStatementBuild = whereStatementBuild + " AND ";
                }
        }

         string command =  "DELETE FROM "+ currentTable +
                           " WHERE "+ whereStatementBuild;
        
        Debug.Log(command);
       
        IDbCommand cmnd = dbcon.CreateCommand();
        cmnd.CommandText = command;
        cmnd.ExecuteNonQuery();
    
        //
        
    }
    
    IDataReader getTables(IDbConnection dbcon)
    {
        IDbCommand cmnd_read = dbcon.CreateCommand();

        string query ="SELECT * FROM sqlite_master where type='table'";
        cmnd_read.CommandText = query;
        return cmnd_read.ExecuteReader();
    }
    
    List<string> getFields(IDbConnection dbcon, string table)
    {
        IDbCommand cmnd_read = dbcon.CreateCommand();

        string query ="PRAGMA table_info("+ table +")";
        cmnd_read.CommandText = query;
        IDataReader reader;
        reader = cmnd_read.ExecuteReader();
        
        List<string> fields = new List<string>();
        
        while (reader.Read())
        {
            fields.Add(reader[1].ToString());
            
        }
        
        return fields;
    }

    public class SimpleTreeView : TreeView
    {
        private int action = 0;

        private DatabaseViewer _databaseViewerInstance;
        private IDataReader reader;
        public SimpleTreeView(TreeViewState treeViewState, MultiColumnHeader header,IDataReader reader, DatabaseViewer instance) : base(treeViewState,header)
        {

            rowHeight = 95;
            showAlternatingRowBackgrounds = true;
            showBorder = true;
            cellMargin = 6;

            this._databaseViewerInstance = instance;
            //extraSpaceBeforeIconAndLabel = Single.Epsilon;
            
            useScrollView = true;
            multiColumnHeader.canSort = true;
            multiColumnHeader.ResizeToFit();
            this.reader = reader;
            
        
            
            Reload();
            
            
        }
        
     
        protected override void RowGUI(RowGUIArgs args)
        {
            var item = (DatabaseViewerItem)args.item;

            GUIStyle style = EditorStyles.textArea;
            style.alignment = TextAnchor.UpperCenter;
            style.clipping = TextClipping.Clip;
            style.wordWrap = true;
            
        
            for (int i = 0; i < args.GetNumVisibleColumns(); ++i)
            {
                Rect r = args.GetCellRect(i);
                int column = args.GetColumn(i);
                int idx = column;

                r.height = r.height - 25;
                item.properties[idx] = EditorGUI.TextArea(r, item.properties[idx], style);


                //r.y = r.height;
                //r.height = 25;
               // new Rect(0, r.height - 30, r.width, 25)

               if (action == 1)
               {
                   
                   r.height = 25;
                   r.y = (r.y + r.height) + 40;
                   r.width = r.width / 2;

                   if (GUI.Button(r, "Apply"))
                   {
                       _databaseViewerInstance.UpdateACell(item.properties[idx], idx, item.properties);
                   }

                   if (idx == 0)
                   {
                       r.x = r.width;

                       if (GUI.Button(r, "Delete"))
                       {
                           _databaseViewerInstance.DeleteACell(idx, item.properties);
                       }
                   }
               }


            }

        }
        
        protected override TreeViewItem BuildRoot ()
        {
            // BuildRoot is called every time Reload is called to ensure that TreeViewItems 
            // are created from data. Here we create a fixed set of items. In a real world example,
            // a data model should be passed into the TreeView and the items created from the model.

            // This section illustrates that IDs should be unique. The root item is required to 
            // have a depth of -1, and the rest of the items increment from that.
            var root = new TreeViewItem {id = 0, depth = -1, displayName = "Root"};

            root.parent = null;
            
            root.children = new List<TreeViewItem>();
            
            
            while (reader.Read())
            {

                
                
                List<string> cellValues = new List<string>();


                for (int i = 0; i < reader.FieldCount; i++)
                {
                    cellValues.Add( reader[i].ToString());
                }
                
                
                

                DatabaseViewerItem item = DatabaseViewerItem.CreateFromUnityObject(cellValues);
                //item.icon = 
                //item.id = GetNewID();

                
                root.children.Add(item);
                
            }
            
            
           
            
            
            // Utility method that initializes the TreeViewItem.children and .parent for all items.
            //SetupDepthsFromParentsAndChildren(root);
            //SetupParentsAndChildrenFromDepths (root, root.children);
            //SetupDepthsFromParentsAndChildren(root); 
            // Return root of the tree
            return root;
        }
        
        protected int _freeID = 0;
        public int GetNewID()
        {
            int id = _freeID;
            _freeID += 1;

            return id;
        }
        
        public void SetAction(int action)
        {
            this.action = action;
        }
    }
    
    
    public class DatabaseViewerItem : TreeViewItem
    {
        //TODO : not too happy with stocking reference to so many thing, prob waste tons of space
        //but as we can't access property by index, need to build an array from them
       
        public string[] properties;

        public static DatabaseViewerItem CreateFromUnityObject(List<string> stuff)
        {
            DatabaseViewerItem newItem = new DatabaseViewerItem();
            newItem.children = new List<TreeViewItem>(); 
            newItem.depth = 0;
            newItem.properties = new string[stuff.Count];

           for (int i = 0; i < stuff.Count; i++)
           {
               newItem.properties[i] = stuff[i];
           }
           
            return newItem;
        }
    }
    
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
       
    }
    
    
    static int viewing = 0;
    static int Editing = 1;
    static int AddRow = 2;
    static int NewTable = 3;
    static int DropTable = 4;
    static int EmptyTable = 5;
    static int NewDatabase = 6;
    static int DeleteDatabase = 7;
   
    public class NewField {
        public string FieldName; 
        public int FieldType;
    }
    
    
   

}
