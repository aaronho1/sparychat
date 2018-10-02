using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Services;

/*********************************
**ADD THESE USING STATEMENTS
*********************************/
//this one lets us work with json data
using Newtonsoft.Json;

//these two let us work with our database
using System.Data.SqlClient;
using System.Data;

//this one lets us grab our database connection string from web.config
using System.Configuration;

namespace sparkychat
{
	/// <summary>
	/// Summary description for sparkychatservices
	/// </summary>
	[WebService(Namespace = "http://tempuri.org/")]
	[WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
	[System.ComponentModel.ToolboxItem(false)]
	// To allow this Web Service to be called from script, using ASP.NET AJAX, uncomment the following line. 
	//YES, UNCOMMENT THIS LINE BELOW
	[System.Web.Script.Services.ScriptService]
	public class sparkychatservices : System.Web.Services.WebService
	{

		[WebMethod(EnableSession = true)]
		public bool LogOn(string uid, string pass)
		{
			bool success = false;
			string sqlConnectString = System.Configuration.ConfigurationManager.ConnectionStrings["myDB"].ConnectionString;
			string sqlSelect = "SELECT id FROM useraccount WHERE username=@idValue and password=@passValue";

			SqlConnection sqlConnection = new SqlConnection(sqlConnectString);
			SqlCommand sqlCommand = new SqlCommand(sqlSelect, sqlConnection);

			sqlCommand.Parameters.Add("@idValue", System.Data.SqlDbType.NVarChar);
			sqlCommand.Parameters["@idValue"].Value = HttpUtility.UrlDecode(uid);
			sqlCommand.Parameters.Add("@passValue", System.Data.SqlDbType.NVarChar);
			sqlCommand.Parameters["@passValue"].Value = HttpUtility.UrlDecode(pass);

			SqlDataAdapter sqlDa = new SqlDataAdapter(sqlCommand);
			DataTable sqlDt = new DataTable();
			sqlDa.Fill(sqlDt);
			if (sqlDt.Rows.Count > 0)
			{
				Session["id"] = sqlDt.Rows[0]["id"];
				success = true;
			}
			return success;
		}

		[WebMethod(EnableSession = true)]
		public bool LogOff()
		{
			Session.Abandon();
			return true;
		}

		[WebMethod(EnableSession = true)]
		public bool CreateAccount(string uid, string pass, string firstName, string lastName)
		{
			bool success = false;
			string sqlConnectString = System.Configuration.ConfigurationManager.ConnectionStrings["myDB"].ConnectionString;
			string sqlSelect = "insert into useraccount (username, password, firstname, lastname) " +
				"values(@idValue, @passValue, @fnameValue, @lnameValue)SELECT SCOPE_IDENTITY();";

			SqlConnection sqlConnection = new SqlConnection(sqlConnectString);
			SqlCommand sqlCommand = new SqlCommand(sqlSelect, sqlConnection);

			sqlCommand.Parameters.Add("@idValue", System.Data.SqlDbType.NVarChar);
			sqlCommand.Parameters["@idValue"].Value = HttpUtility.UrlDecode(uid);
			sqlCommand.Parameters.Add("@passValue", System.Data.SqlDbType.NVarChar);
			sqlCommand.Parameters["@passValue"].Value = HttpUtility.UrlDecode(pass);
			sqlCommand.Parameters.Add("@fnameValue", System.Data.SqlDbType.NVarChar);
			sqlCommand.Parameters["@fnameValue"].Value = HttpUtility.UrlDecode(firstName);
			sqlCommand.Parameters.Add("@lnameValue", System.Data.SqlDbType.NVarChar);
			sqlCommand.Parameters["@lnameValue"].Value = HttpUtility.UrlDecode(lastName);

			sqlConnection.Open();
			try
			{
				int accountID = Convert.ToInt32(sqlCommand.ExecuteScalar());
				success = true;
				Session["id"] = accountID;
				Session["lastmessageid"] = -1;
			}
			catch (Exception e) { }
			sqlConnection.Close();

			return success;
		}

		[WebMethod(EnableSession = true)]
		public bool SendMessage(string message)
		{
			bool success = false;
			if (Session["id"] != null)
			{
				string sqlConnectString = System.Configuration.ConfigurationManager.ConnectionStrings["myDB"].ConnectionString;
				string sqlSelect = "insert into message (useraccountid, message) " +
					"values(@idValue, @messageValue);";

				SqlConnection sqlConnection = new SqlConnection(sqlConnectString);
				SqlCommand sqlCommand = new SqlCommand(sqlSelect, sqlConnection);

				sqlCommand.Parameters.Add("@idValue", System.Data.SqlDbType.Int);
				sqlCommand.Parameters["@idValue"].Value = Convert.ToInt32(Session["id"]);
				sqlCommand.Parameters.Add("@messageValue", System.Data.SqlDbType.NVarChar);
				sqlCommand.Parameters["@messageValue"].Value = HttpUtility.UrlDecode(message);

				sqlConnection.Open();
				int affectedRows = sqlCommand.ExecuteNonQuery();
				sqlConnection.Close();
				if (affectedRows > 0) {
					success = true;
				}
			}

			return success;
		}

		[WebMethod(EnableSession = true)]
		public Message[] GetMessages()
		{
			DataTable sqlDt = new DataTable("messages");
			int lastMessageId = -1;
			if (Session["id"] != null)
			{
				if (Session["lastmessageid"] != null)
				{
					lastMessageId = Convert.ToInt32(Session["lastmessageid"]);
				}
				string sqlConnectString = System.Configuration.ConfigurationManager.ConnectionStrings["myDB"].ConnectionString;
				string sqlSelect;
				if (lastMessageId == -1)
				{
					sqlSelect = "select * from (select top 100 m.id, m.useraccountid, u.firstname, m.message from message m, useraccount u where m.useraccountid=u.id order by m.id desc) as a order by a.id;";
				}
				else
				{
					sqlSelect = "select m.id, m.useraccountid, u.firstname, m.message from message m, useraccount u where m.useraccountid=u.id and m.id>@messageId order by m.id desc;";
				}
				SqlConnection sqlConnection = new SqlConnection(sqlConnectString);
				SqlCommand sqlCommand = new SqlCommand(sqlSelect, sqlConnection);
				if (lastMessageId != -1)
				{
					sqlCommand.Parameters.Add("@messageId", System.Data.SqlDbType.Int);
					sqlCommand.Parameters["@messageId"].Value = lastMessageId;
				}

				SqlDataAdapter sqlDa = new SqlDataAdapter(sqlCommand);

				sqlDa.Fill(sqlDt);
				if (sqlDt.Rows.Count > 0)
				{
					Session["lastmessageid"] = sqlDt.Rows[sqlDt.Rows.Count-1]["id"];
				}
			}
			List<Message> messages = new List<Message>();
			for(int i = 0; i< sqlDt.Rows.Count; i++)
			{
				messages.Add(new Message
				{
					id = Convert.ToInt32(sqlDt.Rows[i]["id"]),
					userName = sqlDt.Rows[i]["firstname"].ToString(),
					message = sqlDt.Rows[i]["message"].ToString()
				});
			}
			return messages.ToArray();
		}
	}
}
