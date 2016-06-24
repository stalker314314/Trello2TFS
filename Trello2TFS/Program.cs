using Manatee.Trello;
using Manatee.Trello.ManateeJson;
using Manatee.Trello.RestSharp;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.WorkItemTracking.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace TfsInteraction
{
	class Task
	{
		public string Title { get; private set; }
		public string AssignedTo { get; private set; }
		public int ParentTaskId { get; private set; }
		public string Description { get; private set; }

		public Task(string title, string assignedTo, int parentTaskId, string description)
		{
			this.Title = title;
			this.AssignedTo = assignedTo;
			this.ParentTaskId = parentTaskId;
			this.Description = description;
        }
	}
	class Program
	{
		static void Main(string[] args)
		{
			List<Task> tasks = GetTrelloTasks();
			foreach(Task task in tasks)
			{
				InsertTfsItem(task);
			}
		}
        static List<Task> GetTrelloTasks()
		{
			List<Task> tasks = new List<Task>();

            Dictionary<string, string> memberMapping = new Dictionary<string, string>(){
				{ "Stefan", "stlazi" },
                { "Mladen", "mlpant" },
				{ "Isidora", "isidoraj" },
				{ "Petar Lotrean", "pelotr" },
				{ "Dejan Dundjerski", "dejandu" }
			};

			Dictionary<string, int> parentTaskMapping = new Dictionary<string, int>()
			{
				{ "KPI report", 7522190 },
				{ "LSI detection", 7522208 },
				{ "TFS alerts", 7720609 },
				{ "Exception Anomaly", 7522234 },
				{ "Random:)", 7720632 },
				{ "CWO", -1 }
			};

            TrelloAuthorization.Default.AppKey = "<app key>";
			TrelloAuthorization.Default.UserToken = "<user token>";
			var serializer = new ManateeSerializer();
			TrelloConfiguration.Serializer = serializer;
			TrelloConfiguration.Deserializer = serializer;
			TrelloConfiguration.JsonFactory = new ManateeFactory();
			TrelloConfiguration.RestClientProvider = new RestSharpClientProvider();
			// Add here ID of board (for May, it was "fUtX9JPg")
			var board = new Board("");
			foreach (Card card in board.Cards)
			{
				if (!card.List.Name.Contains("Completed"))
				{
					continue;
				}

				string title = card.Name;
				if (!memberMapping.ContainsKey(card.Members[0].FullName))
				{
					throw new Exception(String.Format("User {0} doesnt exist in memberMapping", card.Members[0].FullName));
				}

				string assignedTo = memberMapping[card.Members[0].FullName];
				string description = card.Description;
				string labelName = card.Labels[0].Name;
				if (!parentTaskMapping.ContainsKey(labelName))
				{
					throw new Exception(String.Format("Label {0} doesnt exist in parentTaskMapping", labelName));
				}
				int parentTaskId = parentTaskMapping[labelName];
				if (parentTaskId != -1) {
					tasks.Add(new Task(title, assignedTo, parentTaskId, description));
				}
			}

			return tasks;
		}

        static void InsertTfsItem(Task task)
		{
			TfsTeamProjectCollection coll = new TfsTeamProjectCollection(new Uri("<your project URI>"), CredentialCache.DefaultCredentials);
			coll.EnsureAuthenticated();

			WorkItemStore wis = new WorkItemStore(coll);
			Project sqlServerProject = wis.Projects["<your project>"];
			WorkItemType taskType = sqlServerProject.WorkItemTypes["Task"];
			WorkItemLinkTypeEnd parentLinkTypeEnd = wis.WorkItemLinkTypes.LinkTypeEnds["Parent"];

			WorkItem wi = new WorkItem(taskType);
			wi.AreaPath = @"<your\area\path>";
			wi.IterationPath = @"<your\iteration\path>";
			FieldCollection fc = wi.Fields;
			fc["Issue Type"].Value = "Development";
			fc["Issue Subtype"].Value = "Code Implementation";
			fc["Ranking"].Value = 2;
			fc["Priority"].Value = 2;
			fc["Team Custom"].Value = "CWO";
			fc["Assigned To"].Value = task.AssignedTo;
			wi.Description = task.Description;
			fc["Management Custom"].Value = "Imported from Trello";
			wi.Links.Add(new RelatedLink(parentLinkTypeEnd, task.ParentTaskId));
			wi.Title = task.Title;
			wi.Save();

			wi.State = "Closed";
			var asd = wi.Validate();
			wi.Save();
		}
	}
}
