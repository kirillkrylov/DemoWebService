using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace DemoWebService
{
    class Program
    {
        static async Task Main()
        {
            Utils utils = Utils.Instance;
            utils.SetCredentials("Supervisor", "Supervisor", "http://k_krylov_nb:9010");
            if (await utils.LoginAsync()) {
                PrintCurrentUser();
            }

            utils.WebSocketMessageReceived += WebSocketMessageReceived;

            //DeleteContact();
            //InsertNewContact();
            UpdateContact();
            //SelectAllContacts();


            Console.ReadLine(); 

            await utils.LogoutAsync();
            Console.WriteLine("Bye...");
        }

        private static async void SelectAllContacts() {
            Utils utils = Utils.Instance;
            DirectoryInfo dir = Directory.GetParent(Environment.CurrentDirectory);
            dir = Directory.GetParent(dir.FullName);
            dir = Directory.GetParent(dir.FullName);
            string jsonDir = Path.Combine(dir.FullName, "jsonSamples");


            string json = File.ReadAllText(Path.Combine(jsonDir, "SelectAllContacts.json"));
            RequestResponse rr = await utils.ExecuteRequest(json, ActionEnum.SELECT);
            if (!string.IsNullOrEmpty(rr.ErrorMessage))
            {
                Console.WriteLine(rr.ErrorMessage);
            }
            else
            {
                Console.WriteLine(rr.Result);
            }
        }
        private static async void UpdateContact() {
            Utils utils = Utils.Instance;
            DirectoryInfo dir = Directory.GetParent(Environment.CurrentDirectory);
            dir = Directory.GetParent(dir.FullName);
            dir = Directory.GetParent(dir.FullName);
            string jsonDir = Path.Combine(dir.FullName, "jsonSamples");


            string json = File.ReadAllText(Path.Combine(jsonDir, "UpdateSupervisor.json"));
            RequestResponse rr = await utils.ExecuteRequest(json, ActionEnum.UPDATE);
            if (!string.IsNullOrEmpty(rr.ErrorMessage))
            {
                Console.WriteLine(rr.ErrorMessage);
            }
            else
            {
                Console.WriteLine(rr.Result);
            }

        }
        private static async void InsertNewContact()
        {
            Utils utils = Utils.Instance;
            DirectoryInfo dir = Directory.GetParent(Environment.CurrentDirectory);
            dir = Directory.GetParent(dir.FullName);
            dir = Directory.GetParent(dir.FullName);
            string jsonDir = Path.Combine(dir.FullName, "jsonSamples");


            string json = File.ReadAllText(Path.Combine(jsonDir, "InsertNewContact.json"));
            RequestResponse rr = await utils.ExecuteRequest(json, ActionEnum.INSERT);
            if (!string.IsNullOrEmpty(rr.ErrorMessage))
            {
                Console.WriteLine(rr.ErrorMessage);
            }
            else
            {
                Console.WriteLine(rr.Result);
            }
        }
        private static async void DeleteContact()
        {
            Utils utils = Utils.Instance;
            DirectoryInfo dir = Directory.GetParent(Environment.CurrentDirectory);
            dir = Directory.GetParent(dir.FullName);
            dir = Directory.GetParent(dir.FullName);
            string jsonDir = Path.Combine(dir.FullName, "jsonSamples");


            string json = File.ReadAllText(Path.Combine(jsonDir, "DeleteContact.json"));
            RequestResponse rr = await utils.ExecuteRequest(json, ActionEnum.DELETE);
            if (!string.IsNullOrEmpty(rr.ErrorMessage))
            {
                Console.WriteLine(rr.ErrorMessage);
            }
            else
            {
                Console.WriteLine(rr.Result);
            }
        }
        private static void WebSocketMessageReceived(object sender, WebSocketMessageReceivedEventArgs e)
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("---------------- New WebSocket Message ----------------");
            Console.WriteLine($"Id:\t\t{e.MessageId}");
            Console.WriteLine($"Header:\t\t{e.MessageHeader}");
            Console.WriteLine($"Body:\t\t{e.MessageBody}");
            Console.WriteLine("-------------------------------------------------------");
            Console.ResetColor();
        }
        private static void PrintCurrentUser() {

            CurrentUser cu = Utils.Instance.CurrentUser;
            Console.WriteLine("*** Current User Info ***");
            Console.WriteLine($"Account: {cu.Account.DisplayValue}");
            Console.WriteLine($"Contact: {cu.Contact.DisplayValue}");
            Console.WriteLine($"Culture: {cu.Culture.DisplayValue}");
            Console.WriteLine();
        }
    }
}
